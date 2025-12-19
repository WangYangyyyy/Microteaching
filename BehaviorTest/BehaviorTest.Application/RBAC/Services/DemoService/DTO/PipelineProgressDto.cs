namespace BehaviorTest.Application.RBAC.Services.DemoService.DTO;


/// <summary>
/// 整体进度信息
/// </summary>
public class PipelineProgressDto
{
    public ulong VideoId { get; set; }

    /// <summary>总阶段数（目前固定 5 步）</summary>
    public int TotalStages { get; set; }

    /// <summary>已完成阶段数（success 的数量）</summary>
    public int CompletedStages { get; set; }

    /// <summary>当前阶段（可用于高亮步骤条）</summary>
    public string CurrentStage { get; set; } = string.Empty;

    /// <summary>整体状态：running / success / failed / pending</summary>
    public string Status { get; set; } = "pending";

    /// <summary>0~1 的进度值，前端可以直接用来算百分比</summary>
    public double Progress { get; set; }

    /// <summary>各阶段的详细状态</summary>
    public List<PipelineStageStatusDto> Stages { get; set; } = new();
}