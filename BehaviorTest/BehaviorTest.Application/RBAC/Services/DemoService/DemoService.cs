#nullable enable

using System.Text;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DemoService.DTO;
using BehaviorTest.Application.RBAC.Services.DTO;
using Microsoft.AspNetCore.Hosting;

namespace BehaviorTest.Application.RBAC.Services.DemoService;

/// <summary>
/// Demo 页面一键处理服务：
/// 切割 -> 转音频 -> 转录 -> 摘要 -> 课堂评价
/// 并把每一步写入 pipeline_runs 表，方便前端显示进度
/// </summary>
public class DemoService : IDemoService, IDynamicApiController, ITransient
{
    private readonly IVideoService _videoService;
    private readonly IAudioService _audioService;
    private readonly ISummaryService _summaryService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IDemoEvaluationService _demoEvaluationService;
    private readonly IRepository<PipelineRun> _pipelineRunRepository;
    private readonly IRepository<VideoCut> _videoCutRepository;

    // 固定的 5 个阶段名称，统一写在这里，前后端都可以约定使用
    private const string StageCutVideo = "cut_video";
    private const string StageConvertAudio = "convert_audio";
    private const string StageTranscribe = "transcribe";
    private const string StageSummary = "summary";
    private const string StageEvaluate = "evaluate";

    private const string DirSegments = "segments";
    private const string DirAudios = "audio";
    private const string DirTranscripts = "transcripts";
    private const string DirSummaries = "summaries";
    private const string DirEvaluations = "evaluations";
    

    public DemoService(
        IVideoService videoService,
        IAudioService audioService,
        ISummaryService summaryService,
        IDemoEvaluationService demoEvaluationService,
        IRepository<PipelineRun> pipelineRunRepository,
        IRepository<VideoCut> videoCutRepository,
        IWebHostEnvironment webHostEnvironment)
    {
        _videoService = videoService;
        _audioService = audioService;
        _summaryService = summaryService;
        _demoEvaluationService = demoEvaluationService;
        _pipelineRunRepository = pipelineRunRepository;
        _videoCutRepository = videoCutRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    /// <summary>
    /// 一键执行完整链路（按顺序同步执行）
    /// </summary>
    public async Task<DemoPipelineResultDto> RunFullPipelineAsync(ulong videoId)
    {
        var result = new DemoPipelineResultDto
        {
            VideoId = videoId
        };

        // 给本次管线一个统一的 RunUuid，方便后面按一次“任务”查看
        var runUuid = Guid.NewGuid().ToString("N");

        // ========== 1. 切割视频 ==========
        if (await HasStageSucceededBeforeAsync(videoId, DirSegments))
        {
            // 已有成功记录，直接跳过
            await MarkStageSkippedAsync(
                videoId,
                runUuid,
                StageCutVideo,
                "跳过：该视频的切割阶段已在之前的运行中成功完成，本次未重复执行。");

            result.CutResult = OperationResult.SuccessResult(
                "已跳过：该视频切割阶段之前已完成，本次未重复执行。");
        }
        else
        {
            var cutStage = await StartStageAsync(videoId, runUuid, StageCutVideo);
            try
            {
                var cutResult = await _videoService.CutVideoAsync(videoId);
                result.CutResult = cutResult;

                if (!cutResult.Success)
                {
                    await MarkStageFailedAsync(cutStage, cutResult.Message);
                    return result;
                }

                await MarkStageSuccessAsync(cutStage);
            }
            catch (Exception ex)
            {
                await MarkStageFailedAsync(cutStage, ex.Message);
                result.CutResult = OperationResult.FailureResult($"切割异常：{ex.Message}");
                return result;
            }
        }

        // ========== 2. 片段转音频 ==========
        if (await HasStageSucceededBeforeAsync(videoId, DirAudios))
        {
            await MarkStageSkippedAsync(
                videoId,
                runUuid,
                StageConvertAudio,
                "跳过：该视频的“片段转音频”阶段已在之前的运行中成功完成，本次未重复执行。");

            // 这里没有详细统计，简单标记为“已跳过”
            result.AudioConversionResult = new BatchConversionResult
            {
                TotalFiles = 0,
                SuccessCount = 0,
                FailureCount = 0
            };
        }
        else
        {
            var audioStage = await StartStageAsync(videoId, runUuid, StageConvertAudio);
            try
            {
                var audioResult = await _audioService.ConvertAllSegmentsForVideoAsync(videoId);
                result.AudioConversionResult = audioResult;

                if (audioResult.FailureCount > 0 && audioResult.SuccessCount == 0)
                {
                    await MarkStageFailedAsync(audioStage,
                        $"所有片段转音频失败：{string.Join("；", audioResult.FailedMessages)}");
                    return result;
                }

                await MarkStageSuccessAsync(audioStage);
            }
            catch (Exception ex)
            {
                await MarkStageFailedAsync(audioStage, ex.Message);
                result.AudioConversionResult = result.AudioConversionResult ?? new BatchConversionResult();
                result.AudioConversionResult.FailedMessages.Add($"转音频异常：{ex.Message}");
                return result;
            }
        }

        // ========== 3. 音频转录 ==========
        if (await HasStageSucceededBeforeAsync(videoId, DirTranscripts))
        {
            await MarkStageSkippedAsync(
                videoId,
                runUuid,
                StageTranscribe,
                "跳过：该视频的“音频转录”阶段已在之前的运行中成功完成，本次未重复执行。");

            result.TranscriptionResult = new BatchConversionResult
            {
                TotalFiles = 0,
                SuccessCount = 0,
                FailureCount = 0
            };
        }
        else
        {
            var transcribeStage = await StartStageAsync(videoId, runUuid, StageTranscribe);
            try
            {
                var transcribeResult = await _audioService.TranscribeAllSegmentsForVideoAsync(videoId);
                result.TranscriptionResult = transcribeResult;

                if (transcribeResult.FailureCount > 0 && transcribeResult.SuccessCount == 0)
                {
                    await MarkStageFailedAsync(transcribeStage,
                        $"所有片段转录失败：{string.Join("；", transcribeResult.FailedMessages)}");
                    return result;
                }

                await MarkStageSuccessAsync(transcribeStage);
            }
            catch (Exception ex)
            {
                await MarkStageFailedAsync(transcribeStage, ex.Message);
                result.TranscriptionResult = result.TranscriptionResult ?? new BatchConversionResult();
                result.TranscriptionResult.FailedMessages.Add($"转录异常：{ex.Message}");
                return result;
            }
        }


        // ========== 4. 生成摘要 ==========
        if (await HasStageSucceededBeforeAsync(videoId, DirSummaries))
        {
            await MarkStageSkippedAsync(
                videoId,
                runUuid,
                StageSummary,
                "跳过：该视频的“摘要生成”阶段已在之前的运行中成功完成，本次未重复执行。");

            result.SummaryResult = new BatchConversionResult
            {
                TotalFiles = 0,
                SuccessCount = 0,
                FailureCount = 0
            };
        }
        else
        {
            var summaryStage = await StartStageAsync(videoId, runUuid, StageSummary);
            try
            {
                var summaryResult = await _summaryService.GenerateSummariesForVideoAsync(videoId);
                result.SummaryResult = summaryResult;

                if (summaryResult.FailureCount > 0 && summaryResult.SuccessCount == 0)
                {
                    await MarkStageFailedAsync(summaryStage,
                        $"摘要生成全部失败：{string.Join("；", summaryResult.FailedMessages)}");
                    return result;
                }

                await MarkStageSuccessAsync(summaryStage);
            }
            catch (Exception ex)
            {
                await MarkStageFailedAsync(summaryStage, ex.Message);
                result.SummaryResult = result.SummaryResult ?? new BatchConversionResult();
                result.SummaryResult.FailedMessages.Add($"摘要异常：{ex.Message}");
                return result;
            }
        }


        // ========== 5. 课堂改进建议（生成 PDF 报告） ==========
        if (await HasStageSucceededBeforeAsync(videoId, DirEvaluations))
        {
            await MarkStageSkippedAsync(
                videoId,
                runUuid,
                StageEvaluate,
                "跳过：该视频的“改进建议报告生成”阶段已在之前的运行中成功完成，本次未重复执行。");

            result.EvaluationResult = OperationResult.SuccessResult(
                "已跳过：该视频的改进建议报告之前已生成，本次未重复执行。");
        }
        else
        {
            var evalStage = await StartStageAsync(videoId, runUuid, StageEvaluate);
            try
            {
                var evalResult = await _demoEvaluationService.EvaluateAndSaveSuggestionsForVideoAsync(videoId);
                result.EvaluationResult = evalResult;

                if (!evalResult.Success)
                {
                    await MarkStageFailedAsync(evalStage, evalResult.Message);
                    return result;
                }

                await MarkStageSuccessAsync(evalStage);
            }
            catch (Exception ex)
            {
                await MarkStageFailedAsync(evalStage, ex.Message);
                result.EvaluationResult = OperationResult.FailureResult($"评估异常：{ex.Message}");
                return result;
            }
        }

        return result;
    }


    /// <summary>
    /// 前端轮询进度条时调用这个接口
    /// </summary>
    public async Task<PipelineProgressDto> GetPipelineProgressAsync(ulong videoId)
    {
        var allRuns = await _pipelineRunRepository
            .Where(p => p.VideoId == videoId)
            .OrderBy(p => p.StartedAt)
            .ToListAsync();

        var stageOrder = new[]
        {
            StageCutVideo,
            StageConvertAudio,
            StageTranscribe,
            StageSummary,
            StageEvaluate
        };

        var stages = new List<PipelineStageStatusDto>();

        foreach (var stageName in stageOrder)
        {
            var run = allRuns
                .Where(r => r.Stage == stageName)
                .OrderByDescending(r => r.StartedAt)
                .FirstOrDefault();

            stages.Add(new PipelineStageStatusDto
            {
                Stage = stageName,
                Status = run?.Status ?? "pending",
                StartedAt = run?.StartedAt,
                FinishedAt = run?.FinishedAt,
                ErrorMessage = run?.ErrorMessage
            });
        }

        var totalStages = stageOrder.Length;
        var completed = stages.Count(s => s.Status == "success");
        var failedStage = stages.FirstOrDefault(s => s.Status == "failed");
        var runningStage = stages.FirstOrDefault(s => s.Status == "running");

        string overallStatus;
        string currentStage;

        if (failedStage != null)
        {
            overallStatus = "failed";
            currentStage = failedStage.Stage;
        }
        else if (completed == totalStages)
        {
            overallStatus = "success";
            currentStage = "completed";
        }
        else if (runningStage != null)
        {
            overallStatus = "running";
            currentStage = runningStage.Stage;
        }
        else if (completed == 0)
        {
            overallStatus = "pending";
            currentStage = stageOrder[0];
        }
        else
        {
            overallStatus = "running";
            currentStage = stageOrder[completed];
        }

        var progress = totalStages == 0 ? 0 : (double)completed / totalStages;

        return new PipelineProgressDto
        {
            VideoId = videoId,
            TotalStages = totalStages,
            CompletedStages = completed,
            CurrentStage = currentStage,
            Status = overallStatus,
            Progress = progress,
            Stages = stages
        };
    }

    #region PipelineRun 辅助方法

    /// <summary>
    /// 判断某个阶段是否已经在历史运行中成功过（基于文件检测）
    /// </summary>
    private async Task<bool> HasStageSucceededBeforeAsync(ulong videoId, string stage)
    {
        var videoname = await _videoService.GetVideoFileNameAsync(videoId);
        var videonameWithoutExtension = Path.GetFileNameWithoutExtension(videoname);
        var videoDir = Path.Combine("wwwroot", "processing", videonameWithoutExtension);
        var segmentDir = Path.Combine(videoDir, "segments");
        

        bool filesOk = stage switch
        {
            // 切割阶段（看 segments 是否有文件）
            DirSegments => Directory.Exists(segmentDir)&&
                Directory.EnumerateFiles(segmentDir).Any(),

            // 音频转码阶段（看 audio 下有没有音频文件）
            DirAudios => HasFilesWithExtensions(
                Path.Combine(videoDir, DirAudios),
                ".mp3", ".wav", ".m4a", ".flac"),

            // 转录阶段（看 transcripts 下是否有文本）
            DirTranscripts => HasFilesWithExtensions(
                Path.Combine(videoDir, DirTranscripts),
                ".txt", ".md"),

            // 摘要阶段
            DirSummaries => HasFilesWithExtensions(
                Path.Combine(videoDir, DirSummaries),
                ".txt", ".md"),

            // 评估（PDF 报告）阶段
            DirEvaluations => HasFilesWithExtensions(
                Path.Combine(videoDir, DirEvaluations),
                ".pdf", ".docx", ".html", ".htm", ".txt", ".md"),

            _ => false
        };

        return filesOk;
    }



    /// <summary>
    /// 检查目录中是否存在指定后缀文件
    /// </summary>
    private static bool HasFilesWithExtensions(string dir, params string[] extensions)
    {
        if (!Directory.Exists(dir))
            return false;

        foreach (var file in Directory.EnumerateFiles(dir))
        {
            var ext = Path.GetExtension(file);
            if (extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 本次运行直接标记某个阶段为“已跳过但视为成功”
    /// 不调用真实的业务逻辑，只写 pipeline_runs 记录
    /// </summary>
    private async Task<PipelineRun> MarkStageSkippedAsync(
        ulong videoId,
        string runUuid,
        string stage,
        string? reason = null)
    {
        var now = DateTime.UtcNow;

        var entity = new PipelineRun
        {
            VideoId = videoId,
            RunUuid = runUuid,
            Stage = stage,
            Status = "success",
            StartedAt = now,
            FinishedAt = now,
            ErrorMessage = string.IsNullOrWhiteSpace(reason)
                ? "本阶段已在之前的运行中成功完成，本次未重复执行。"
                : reason
        };

        await _pipelineRunRepository.InsertNowAsync(entity);
        return entity;
    }

    private async Task<PipelineRun> StartStageAsync(ulong videoId, string runUuid, string stage)
    {
        var entity = new PipelineRun
        {
            VideoId = videoId,
            RunUuid = runUuid,
            Stage = stage,
            Status = "running",
            StartedAt = DateTime.UtcNow
        };

        await _pipelineRunRepository.InsertNowAsync(entity);
        return entity;
    }

    private async Task MarkStageSuccessAsync(PipelineRun run)
    {
        run.Status = "success";
        run.FinishedAt = DateTime.UtcNow;
        run.ErrorMessage = null;
        await _pipelineRunRepository.UpdateNowAsync(run);
    }

    private async Task MarkStageFailedAsync(PipelineRun run, string? error)
    {
        run.Status = "failed";
        run.FinishedAt = DateTime.UtcNow;
        run.ErrorMessage = SanitizeErrorMessage(error, 500);
        await _pipelineRunRepository.UpdateNowAsync(run);
    }

    // DemoService.cs 里加一个工具方法
    private static string? SanitizeErrorMessage(string? input, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 1. 去掉 MySQL 不支持的 4 字节字符（emoji、某些罕见符号）
        //    char.IsSurrogate 能帮我们过滤掉 UTF-16 代理项（surrogate pair）
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsSurrogate(ch))
            {
                // 跳过 emoji / 4 字节字符
                continue;
            }

            sb.Append(ch);
        }

        var result = sb.ToString();

        // 2. 做一下长度截断，避免超过字段长度
        if (result.Length > maxLength)
        {
            result = result.Substring(0, maxLength);
        }

        return result;
    }

    #endregion
}