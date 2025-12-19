using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("videos")]
public class Video : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("source_path"), Required, MaxLength(512)]
    public string SourcePath { get; set; } = null!;

    [Column("original_name"), Required, MaxLength(255)]
    public string OriginalName { get; set; } = null!;

    [Column("duration_seconds")]
    public uint? DurationSeconds { get; set; }

    [Column("status"), MaxLength(32)]
    public string Status { get; set; } = "pending";

    [Column("notes"), MaxLength(512)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("micro_lesson_id")]
    public ulong? MicroLessonId { get; set; }

    [ForeignKey(nameof(MicroLessonId))]
    public MicroLesson? MicroLesson { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity User { get; set; } = null!;

    public ICollection<PipelineRun> PipelineRuns { get; set; } = new List<PipelineRun>();
    public ICollection<Transcript> Transcripts { get; set; } = new List<Transcript>();
    public ICollection<Summary> Summaries { get; set; } = new List<Summary>();
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

}