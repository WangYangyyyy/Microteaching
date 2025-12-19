using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("pipeline_runs")]
public class PipelineRun : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }
    
    [Column("video_id")]
    public ulong VideoId { get; set; }

    [Column("run_uuid"), Required, MaxLength(36)]
    public string RunUuid { get; set; } = null!;

    [Column("stage"), Required, MaxLength(32)]
    public string Stage { get; set; } = null!;

    [Column("status"), Required, MaxLength(32)]
    public string Status { get; set; } = "pending";

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("error_message"), MaxLength(512)]
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(VideoId))]
    public Video Video { get; set; } = null!;

}