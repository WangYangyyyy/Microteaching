namespace BehaviorTest.Application.RBAC.Services.DemoService.DTO;

/// <summary>
/// 单个阶段的状态，用于进度条和步骤条
/// </summary>
public class PipelineStageStatusDto
{
    public string Stage { get; set; } = string.Empty; // 例如: cut_video / convert_audio / transcribe / summary / evaluate
    public string Status { get; set; } = "pending"; // pending / running / success / failed
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? ErrorMessage { get; set; }
}