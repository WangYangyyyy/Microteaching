using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;
using Furion.VirtualFileServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace BehaviorTest.Application.RBAC.Services;

public class VideoService : IVideoService, IDynamicApiController, ITransient
{
    private readonly IRepository<Video> _videoRepository;
    private readonly IRepository<VideoCut> _videoCutRepository;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ILogger<VideoService> _logger;
    private readonly HttpClient _http;

    public VideoService(
        IRepository<Video> videoRepository,
        IRepository<VideoCut> videoCutRepository,
        IWebHostEnvironment hostEnvironment,
        ILogger<VideoService> logger,
        HttpClient http)
    {
        _videoRepository = videoRepository;
        _videoCutRepository = videoCutRepository;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _http = http;
    }

    private readonly ConcurrentDictionary<Guid, (Process Process, string FilePath, string FileName, string ContentType, ulong MicroLessonId)>
        _recordings = new();

    // 开始录制视频流
    public async Task<OperationResult> StartRecordingAsync(string streamUrl, ulong? microLessonId = null)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
            return OperationResult.FailureResult("流地址不能为空");

        var recordingsFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos");
        if (!Directory.Exists(recordingsFolder)) Directory.CreateDirectory(recordingsFolder);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.mp4";
        var filePath = Path.Combine(recordingsFolder, fileName);

        var args = $"-y -i \"{streamUrl}\" -c copy -f mp4 \"{filePath}\"";
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true, // ffmpeg 把所有日志都输出到 StandardError
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        Process process = null;
        // TaskCompletionSource (TCS) 用于在后台任务和主线程之间传递信号
        // bool = true 表示成功, bool = false 表示失败
        var tcs = new TaskCompletionSource<bool>();
        string lastErrorLine = string.Empty;

        try
        {
            process = Process.Start(psi);
            if (process == null) return OperationResult.FailureResult("启动 ffmpeg 失败");

            // --- 启动智能监视任务 ---
            // 这个后台任务会一直读取 ffmpeg 日志
            _ = Task.Run(async () =>
            {
                try
                {
                    var errorReader = process.StandardError;

                    while (!process.StandardError.EndOfStream)
                    {
                        string line = await errorReader.ReadLineAsync();
                        if (line == null) break;

                        _logger.LogTrace("ffmpeg: {0}", line);
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lastErrorLine = line; // 记录最后一行错误
                            // 添加：记录所有错误行以便调试
                            if (line.Contains("Error") || line.Contains("failed"))
                            {
                                _logger.LogError("FFmpeg error: {0}", line);
                            }
                        }

                        // --- 1. 检查成功信号 ---
                        // 只要看到 "Input #0" 或 "Stream #0" 就代表连接成功
                        if (line.Contains("Input #0,") || (line.Contains("Stream #0:") &&
                                                           (line.Contains("Video:") || line.Contains("Audio:"))))
                        {
                            // 立即发送“成功”信号
                            tcs.TrySetResult(true);
                        }

                        // --- 2. 检查明确的失败信号 ---
                        if (line.Contains("Connection refused") ||
                            line.Contains("Operation timed out") ||
                            line.Contains("No such file or directory") ||
                            line.Contains("Invalid data found when processing input"))
                        {
                            // 这是一个明确的连接失败
                            // 注意：我们只在 'tcs' 尚未成功时才设置失败
                            tcs.TrySetResult(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "读取 ffmpeg 日志时出错");
                    tcs.TrySetResult(false); // 发生异常也算失败
                }
                finally
                {
                    // --- 3. 进程自己退出了 ---
                    // 如果进程退出了，而我们从未发送过“成功”信号，那么就判定为“失败”
                    tcs.TrySetResult(false);

                    // 关键：我们 *不* 在这里 Dispose() 进程
                    // StopRecordingAsync 是唯一负责 Dispose() 的地方
                }
            });

            // --- 等待验证结果 ---
            // 我们给它 10 秒钟时间，看是 tcs.Task (日志分析) 先完成，还是 timeoutTask (10秒超时) 先完成
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // --- 场景A: 10秒超时 ---
                // 这就是您描述的“等待未唤醒的流”的场景
                // 10秒过去了，日志里既没报成功也没报失败，说明 ffmpeg 还在傻等
                try
                {
                    _logger.LogWarning($"ffmpeg (Id: {process.Id}) 启动超时 (10s)，强制终止。");
                    process.Kill(true);
                }
                catch
                {
                    /* 忽略 kill 失败 */
                }

                // 注意：我们不 Dispose()，让 StopRecordingAsync (如果被调用) 或系统来处理
                // (更新：不，我们必须 Dispose()，因为 StopRecordingAsync 永远不会被调用)
                process.Dispose();

                return OperationResult.FailureResult($"启动失败：连接超时 (10s)。请检查流是否已唤醒。");
            }

            // --- 场景B: 日志分析完成 (tcs.Task 完成) ---
            var success = await tcs.Task; // 获取分析结果 (true/false)

            if (success)
            {
                // --- 成功！---
                // 日志中已检测到 "Input #0"，确认录制开始
                var id = Guid.NewGuid();
                _recordings.TryAdd(id, (process, filePath, fileName, "video/mp4", microLessonId ?? 1));

                return OperationResult.SuccessResult("开始录制", new { RecordingId = id, FileName = fileName });
            }
            else
            {
                // --- 失败！---
                // 进程在10秒内就自己退出了，或者报了 "Connection refused"
                process.Dispose(); // 失败了，我们自己清理
                return OperationResult.FailureResult($"启动失败：ffmpeg 无法连接或提前退出。Last error: {lastErrorLine}");
            }
        }
        catch (Exception ex)
        {
            if (process != null)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    /* ignore */
                }

                process.Dispose();
            }

            return OperationResult.FailureResult($"开始录制失败：{ex.Message}");
        }
    }

    // 停止录制并保存视频
    public async Task<OperationResult> StopRecordingAsync(Guid recordingId)
    {
        // 1. 尝试从字典中移除录制任务
        if (!_recordings.TryRemove(recordingId, out var rec))
        {
            return OperationResult.FailureResult("未找到对应的录制任务或已停止");
        }

        // 在 try 块外部声明 process，以便 finally 块可以访问
        Process process = rec.Process;

        try
        {
            // 2. 检查进程是否仍在运行
            if (!process.HasExited)
            {
                // 3. 优雅停止：如果启动时重定向了 stdin，向 ffmpeg 发送 'q' 让其完成封装
                if (process.StartInfo.RedirectStandardInput)
                {
                    try
                    {
                        await process.StandardInput.WriteAsync("q");
                        await process.StandardInput.FlushAsync();
                        process.StandardInput.Close(); // 关闭输入流以确保 'q' 被发送
                    }
                    catch (Exception ex)
                    {
                        // 如果写入 'q' 失败 (例如进程刚刚退出)，记录日志并准备降级处理
                        _logger.LogDebug(ex, "向 ffmpeg 写入 'q' 失败，准备降级处理");
                    }
                }

                // 4. 等待一段时间让 ffmpeg 优雅退出，超时则强制结束
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10秒超时
                try
                {
                    // 使用 CancellationToken 等待进程退出
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 超时了，进程仍未退出，执行强制终止
                    _logger.LogWarning($"ffmpeg (Id: {process.Id}) 优雅退出超时，执行强制终止。");
                    try
                    {
                        // 尝试带子进程一起结束（Windows 支持 true，Unix 可能忽略）
                        process.Kill(entireProcessTree: true);
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogError(killEx, $"强制终止 ffmpeg (Id: {process.Id}) 失败。");
                        try
                        {
                            // 最后的尝试
                            process.Kill();
                        }
                        catch
                        {
                            /* 忽略最后的 kill 失败 */
                        }
                    }

                    // 确保在 kill 之后我们等待进程完全退出
                    await process.WaitForExitAsync(CancellationToken.None);
                }
            }

            // 5. 进程现已停止，确认文件已生成
            if (!File.Exists(rec.FilePath))
            {
                _logger.LogWarning($"ffmpeg (Id: {process.Id}) 已停止，但录制文件未生成：{rec.FilePath}");
                return OperationResult.FailureResult("录制文件未生成");
            }

            var fileInfo = new FileInfo(rec.FilePath);

            // 检查文件大小，以防万一
            if (fileInfo.Length == 0)
            {
                _logger.LogWarning($"ffmpeg (Id: {process.Id}) 已停止，但文件大小为 0：{rec.FilePath}");
                // 您可以选择在这里删除文件
                // File.Delete(rec.FilePath);
                return OperationResult.FailureResult("录制文件大小为 0");
            }

            // 6. 创建数据库记录
            var video = new Video
            {
                UserId = 1, // 替换为真实的用户ID
                MicroLessonId =  rec.MicroLessonId,
                SourcePath = rec.FilePath,
                OriginalName = rec.FileName,
                Status = "uploaded",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _videoRepository.InsertNowAsync(video);

            // 7. 返回成功结果
            return OperationResult.SuccessResult("录制已保存", new VideoInfoDto
            {
                Id = video.Id,
                FileName = video.OriginalName,
                FilePath = $"/videos/{rec.FileName}", // 确保这是正确的Web访问路径
                FileSize = fileInfo.Length,
                ContentType = rec.ContentType,
                UploadTime = video.CreatedAt,
                Status = video.Status,
                Notes = video.Notes
            });
        }
        catch (Exception ex)
        {
            // 捕获所有异常 (包括 'No process is associated with this object')
            _logger.LogError(ex, $"停止录制 (RecordingId: {recordingId}) 时发生严重错误");
            return OperationResult.FailureResult($"停止录制失败：{ex.Message}");
        }
        finally
        {
            // 
            // --- 关键修复 ---
            // 无论成功还是失败，都要释放进程对象，防止资源泄露
            // 并防止竞态条件
            // 
            process?.Dispose();
        }
    }

    public async Task<OperationResult> UploadVideoAsync1(Stream stream, string? originalFileName = null,
        string? contentType = null)
    {
        try
        {
            var userId = 1; // TODO: 从当前用户上下文获取实际用户ID
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // 如果没有提供文件名，基于 contentType 或默认扩展生成一个
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                string ext = ".mp4";
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    ext = contentType.ToLowerInvariant() switch
                    {
                        "video/mp4" => ".mp4",
                        "video/x-msvideo" => ".avi",
                        "video/quicktime" => ".mov",
                        "video/x-ms-wmv" => ".wmv",
                        "video/x-flv" => ".flv",
                        "video/webm" => ".webm",
                        _ => ".mp4"
                    };
                }

                originalFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")}{ext}";
            }

            var baseName = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var uniqueFileName = originalFileName;
            int counter = 1;
            while (File.Exists(Path.Combine(uploadsFolder, uniqueFileName)))
            {
                uniqueFileName = $"{baseName}({counter}){extension}";
                counter++;
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 将传入的 stream 安全地写入文件（支持非 seekable stream）
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920,
                       useAsync: true))
            {
                await stream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }

            var fileInfo = new FileInfo(filePath);

            var video = new Video
            {
                UserId = userId,
                SourcePath = filePath,
                OriginalName = uniqueFileName,
                Status = "uploaded",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _videoRepository.InsertNowAsync(video);

            return OperationResult.SuccessResult("视频上传成功", new VideoInfoDto
            {
                Id = video.Id,
                FileName = video.OriginalName,
                FilePath = $"/videos/{uniqueFileName}",
                FileSize = fileInfo.Length,
                ContentType = contentType ?? GetContentType(uniqueFileName),
                UploadTime = video.CreatedAt,
                Status = video.Status,
                Notes = video.Notes
            });
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"上传失败：{ex.Message}");
        }
    }


    public async Task<OperationResult> UploadVideoAsync(string fileName, Stream stream, string contentType, ulong? microLessonId = null)
    {
        try
        {
            // 假设用户ID从当前用户获取，这里硬编码为1
            var userId = 1;
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "videos");

            // 生成唯一的文件名，如果存在则加后缀
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var uniqueFileName = fileName;
            int counter = 1;
            while (File.Exists(Path.Combine(uploadsFolder, uniqueFileName)))
            {
                uniqueFileName = $"{baseName}({counter}){extension}";
                counter++;
            }

            // 保存文件到wwwroot/videos目录

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            // 获取文件信息
            var fileInfo = new FileInfo(filePath);

            // 创建Video记录
            var video = new Video
            {
                UserId = userId,
                MicroLessonId =  microLessonId,
                SourcePath = filePath,
                OriginalName = fileName,
                Status = "uploaded",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _videoRepository.InsertNowAsync(video);

            return OperationResult.SuccessResult("视频上传成功", new VideoInfoDto
            {
                Id = video.Id,
                FileName = video.OriginalName,
                FilePath = $"/videos/{uniqueFileName}",
                FileSize = fileInfo.Length,
                ContentType = contentType,
                UploadTime = video.CreatedAt,
                Status = video.Status,
                Notes = video.Notes
            });
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"上传失败：{ex.Message}");
        }
    }

    public async Task<List<VideoInfoDto>> GetAllVideosAsync()
    {
        var videos = await _videoRepository.AsQueryable()
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        return videos.Select(v => new VideoInfoDto
        {
            Id = v.Id,
            FileName = v.OriginalName,
            FilePath = GetVideoWebPath(v.SourcePath),
            FileSize = GetFileSize(v.SourcePath),
            ContentType = GetContentType(v.OriginalName),
            UploadTime = v.CreatedAt,
            Status = v.Status,
            Notes = v.Notes,
            MicroLessonId = v.MicroLessonId ?? 0
        }).ToList();
    }

    public async Task<OperationResult> DeleteVideoAsync(ulong id)
    {
        try
        {
            var video = await _videoRepository.FindOrDefaultAsync(id);
            if (video == null)
            {
                return OperationResult.FailureResult("视频不存在");
            }

            // 删除物理文件
            if (File.Exists(video.SourcePath))
            {
                File.Delete(video.SourcePath);
            }

            // 从数据库中删除
            await _videoRepository.DeleteNowAsync(video);

            return OperationResult.SuccessResult("视频删除成功");
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"删除失败：{ex.Message}");
        }
    }

    public async Task<VideoInfoDto?> GetVideoByIdAsync(ulong id)
    {
        var video = await _videoRepository.FindOrDefaultAsync(id);
        if (video == null)
        {
            return null;
        }

        return new VideoInfoDto
        {
            Id = video.Id,
            FileName = video.OriginalName,
            FilePath = GetVideoWebPath(video.SourcePath),
            FileSize = GetFileSize(video.SourcePath),
            ContentType = GetContentType(video.OriginalName),
            UploadTime = video.CreatedAt,
            Status = video.Status,
            Notes = video.Notes
        };
    }

    public async Task<OperationResult> CutVideoAsync(ulong videoId)
    {
        try
        {
            var video = await _videoRepository.FindOrDefaultAsync(videoId);
            if (video == null)
            {
                return OperationResult.FailureResult("视频不存在");
            }

            var inputPath = video.SourcePath;
            var processingRoot = Path.Combine(_hostEnvironment.WebRootPath, "processing");
            var videoDirName = Path.GetFileNameWithoutExtension(video.OriginalName);
            var videoProcessingDir = Path.Combine(processingRoot, videoDirName);
            Directory.CreateDirectory(videoProcessingDir);

            var segmentsDir = Path.Combine(videoProcessingDir, "segments");
            Directory.CreateDirectory(segmentsDir);

            // 获取视频时长
            var duration = await GetVideoDuration(inputPath);
            int segmentLength = 600; // 10分钟
            int segmentIndex = 0;

            for (double start = 0; start < duration; start += segmentLength)
            {
                double end = Math.Min(start + segmentLength, duration);
                var segmentName = $"{Path.GetFileNameWithoutExtension(video.OriginalName)}_{segmentIndex:D3}.mp4";
                var outputPath = Path.Combine(segmentsDir, segmentName);

                // 使用FFmpeg切割
                var success = await CutVideoSegment(inputPath, outputPath, start, end - start);
                if (success)
                {
                    // 保存到数据库
                    var videoCut = new VideoCut
                    {
                        VideoId = videoId,
                        SegmentIndex = segmentIndex,
                        SegmentPath = outputPath,
                        StartTime = start,
                        EndTime = end,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _videoCutRepository.InsertNowAsync(videoCut);
                }

                segmentIndex++;
            }

            await _videoCutRepository.Context.SaveChangesAsync();

            return OperationResult.SuccessResult("视频切割完成");
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"切割失败：{ex.Message}");
        }
    }


    private async Task<double> GetVideoDuration(string inputPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = $"-v quiet -print_format json -show_format \"{inputPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var process = Process.Start(processInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        try
        {
            using (JsonDocument doc = JsonDocument.Parse(output))
            {
                if (doc.RootElement.TryGetProperty("format", out JsonElement formatElement) &&
                    formatElement.TryGetProperty("duration", out JsonElement durationElement))
                {
                    var durationString = durationElement.GetString();
                    return double.Parse(durationString);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"解析视频时长失败: {ex.Message}, 输出: {output}");
        }

        return 0;
    }

    private async Task<bool> CutVideoSegment(string inputPath, string outputPath, double start, double duration)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{inputPath}\" -ss {start} -t {duration} -c copy \"{outputPath}\"",
            UseShellExecute = false
        };

        var process = Process.Start(processInfo);
        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }
    
    //根据id获取name
    public Task<string> GetVideoFileNameAsync(ulong videoId)
    {
        return _videoRepository.AsQueryable()
            .Where(v => v.Id == videoId)
            .Select(v => v.OriginalName)
            .FirstOrDefaultAsync() ?? Task.FromResult(string.Empty);
    }

    private string GetVideoWebPath(string physicalPath)
    {
        var fileName = Path.GetFileName(physicalPath);
        return $"/videos/{fileName}";
    }

    private long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        return 0;
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",
            _ => "video/mp4"
        };
    }
}