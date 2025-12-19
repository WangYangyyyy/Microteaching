// Services/AudioService.cs

using System.Diagnostics;
using System.Text.Json;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services;
using BehaviorTest.Application.RBAC.Services.DTO;

public class AudioService : IAudioService, IDynamicApiController, ITransient
{
    // 通过构造函数注入依赖
    private readonly IRepository<VideoCut> _videoCutRepository;
    private readonly IRepository<Transcript> _transcriptRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public AudioService(IRepository<VideoCut> videoCutRepository, IRepository<Transcript> transcriptRepository,
        IHttpClientFactory httpClientFactory)
    {
        _videoCutRepository = videoCutRepository;
        _transcriptRepository = transcriptRepository;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 【新增】核心业务方法：根据 VideoId 转换所有关联的视频片段
    /// </summary>
    /// <param name="videoId">视频的ID</param>
    /// <returns>一个包含详细转换结果的对象</returns>
    public async Task<BatchConversionResult> ConvertAllSegmentsForVideoAsync(ulong videoId)
    {
        var result = new BatchConversionResult();

        var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
        result.TotalFiles = cuts.Count;

        if (result.TotalFiles == 0)
        {
            result.FailedMessages.Add("没有找到需要转换的视频片段。");
            return result;
        }

        foreach (var cut in cuts)
        {
            // 计算视频的 processing 根目录（segments 的上级目录）
            var segmentDir = Path.GetDirectoryName(cut.SegmentPath);
            if (string.IsNullOrEmpty(segmentDir))
            {
                result.FailedMessages.Add($"无法解析片段路径: {cut.SegmentPath}");
                continue;
            }

            var videoDir = Path.GetDirectoryName(segmentDir) ?? segmentDir; // 安全回退
            var audioDir = Path.Combine(videoDir, "audio");
            Directory.CreateDirectory(audioDir);
            var audioPath = Path.Combine(audioDir, Path.GetFileNameWithoutExtension(cut.SegmentPath) + ".mp3");

            // 调用内部的单个文件转换方法
            var singleResult = await ConvertSingleVideoToAudioAsync(cut.SegmentPath, audioPath);

            if (singleResult.Success)
            {
                result.SuccessCount++;
            }
            else
            {
                var fileName = Path.GetFileName(cut.SegmentPath);
                result.FailedMessages.Add($"文件 '{fileName}' 转换失败: {singleResult.Message}");
            }
        }

        result.FailureCount = result.TotalFiles - result.SuccessCount;

        return result;
    }

    /// <summary>
    /// 【重构】将原方法改为 private，作为内部实现细节
    /// </summary>
    private async Task<OperationResult> ConvertSingleVideoToAudioAsync(string videoPath, string audioOutputPath)
    {
        try
        {
            if (!File.Exists(videoPath))
            {
                // 现在的错误信息更具体
                return OperationResult.FailureResult($"视频文件不存在: {videoPath}");
            }

            // ffmpeg 参数中添加 -y 可以自动覆盖已存在的文件，避免因文件已存在而转换失败
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoPath}\" -y -vn -acodec libmp3lame \"{audioOutputPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true // 在后台运行，不弹出黑色的cmd窗口
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                return OperationResult.FailureResult("无法启动 FFmpeg 进程");
            }

            // 必须异步读取错误流，否则如果错误信息过多，可能导致缓冲区满而死锁
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            // ffmpeg 即使成功也可能向 StandardError 输出信息，所以主要判断 ExitCode
            if (process.ExitCode == 0)
            {
                return OperationResult.SuccessResult("音频转换完成");
            }
            else
            {
                return OperationResult.FailureResult($"FFmpeg 进程执行失败 (ExitCode: {process.ExitCode}): {error}");
            }
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"转换过程中发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 转录音频文件
    /// </summary>
    public async Task<OperationResult> TranscribeAudioFileAsync(string audioPath, string outputDir)
    {
        try
        {
            if (!File.Exists(audioPath))
            {
                return OperationResult.FailureResult("音频文件不存在");
            }

            Directory.CreateDirectory(outputDir);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10); // 增加超时时间到10分钟

            using var content = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(audioPath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            content.Add(fileContent, "file", Path.GetFileName(audioPath));
            content.Add(new StringContent(outputDir), "directory");

            var url =
                $"http://118.230.212.160:8000/transcribe/upload?file_path={Uri.EscapeDataString(audioPath)}&directory={Uri.EscapeDataString(outputDir)}";
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult.FailureResult($"转录请求失败: {response.StatusCode}, {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(responseContent);

            if (json.RootElement.TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(audioPath) + ".txt");
                await File.WriteAllTextAsync(outputPath, text);

                // 保存转录信息到数据库
                var segmentBase = Path.GetFileNameWithoutExtension(audioPath);
                var cut = await _videoCutRepository.Where(vc => vc.SegmentPath.Contains(segmentBase))
                    .FirstOrDefaultAsync();
                if (cut != null)
                {
                    var transcript = new Transcript
                    {
                        VideoId = cut.VideoId,
                        RunUuid = Guid.NewGuid().ToString(),
                        SegmentIndex = (uint)cut.SegmentIndex,
                        TranscriberModel = "whisper",
                        LanguageCode = "zh",
                        TextStorePath = outputPath,
                        CharactersCount = (uint)text.Length,
                        GeneratedAt = DateTime.UtcNow
                    };
                    await _transcriptRepository.InsertNowAsync(transcript);
                }

                return OperationResult.SuccessResult("转录完成");
            }
            else
            {
                return OperationResult.FailureResult("返回体中没有 text 字段");
            }
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"转录出错: {ex.Message}");
        }
    }
    
    
    /// <summary>
    /// 将原页面中的 StartTranscribing 逻辑移动到服务层：对指定 videoId 的所有片段进行转录。
    /// 返回 BatchConversionResult 以便前端显示总数/成功/失败信息。
    /// </summary>
    public async Task<BatchConversionResult> TranscribeAllSegmentsForVideoAsync(ulong videoId)
    {
        var result = new BatchConversionResult();

        var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
        result.TotalFiles = cuts.Count;

        if (result.TotalFiles == 0)
        {
            result.FailedMessages.Add("没有找到需要转录的视频片段。");
            return result;
        }

        foreach (var cut in cuts)
        {
            var segmentDir = Path.GetDirectoryName(cut.SegmentPath);
            if (string.IsNullOrEmpty(segmentDir))
            {
                result.FailedMessages.Add($"无法解析片段路径: {cut.SegmentPath}");
                continue;
            }

            var videoDir = Path.GetDirectoryName(segmentDir) ?? segmentDir;
            var audioDir = Path.Combine(videoDir, "audio");
            var audioPath = Path.Combine(audioDir, Path.GetFileNameWithoutExtension(cut.SegmentPath) + ".mp3");

            if (!File.Exists(audioPath))
            {
                result.FailedMessages.Add($"音频文件不存在: {audioPath}");
                continue;
            }

            var transcriptDir = Path.Combine(videoDir, "transcripts");
            Directory.CreateDirectory(transcriptDir);

            var op = await TranscribeAudioFileAsync(audioPath, transcriptDir);
            if (op.Success)
            {
                result.SuccessCount++;
            }
            else
            {
                var fileName = Path.GetFileName(audioPath);
                result.FailedMessages.Add($"文件 '{fileName}' 转录失败: {op.Message}");
            }
        }

        result.FailureCount = result.TotalFiles - result.SuccessCount;
        return result;
    }
}