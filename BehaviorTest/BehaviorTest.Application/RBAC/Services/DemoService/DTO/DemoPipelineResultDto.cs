using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services.DemoService.DTO;

/// <summary>
/// 一键处理的整体结果，前端想看详细信息可以用这个
/// </summary>
public class DemoPipelineResultDto
{
    public ulong VideoId { get; set; }

    /// <summary>切割结果</summary>
    public OperationResult CutResult { get; set; } = OperationResult.FailureResult("未执行");

    /// <summary>视频片段转音频结果（批量）</summary>
    public BatchConversionResult AudioConversionResult { get; set; } = new();

    /// <summary>音频转录结果（批量）</summary>
    public BatchConversionResult TranscriptionResult { get; set; } = new();

    /// <summary>摘要生成结果（批量）</summary>
    public BatchConversionResult SummaryResult { get; set; } = new();

    /// <summary>课堂评价结果（生成报告）</summary>
    public OperationResult EvaluationResult { get; set; } = OperationResult.FailureResult("未执行");
}


