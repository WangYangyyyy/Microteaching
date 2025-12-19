using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("evaluations")]
public class Evaluation : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }


    [Column("video_id")]
    public ulong? VideoId { get; set; }

    [Column("run_uuid"), Required, MaxLength(36)]
    public string RunUuid { get; set; } = null!;

    [Column("evaluation_model"), Required, MaxLength(128)]
    public string EvaluationModel { get; set; } = null!;

    [Column("input_source"), MaxLength(16)]
    public string InputSource { get; set; } = "summary";

    [Column("total_score")]
    public decimal? TotalScore { get; set; }

    [Column("philosophy_score")]
    public decimal? PhilosophyScore { get; set; }

    [Column("content_score")]
    public decimal? ContentScore { get; set; }

    [Column("process_score")]
    public decimal? ProcessScore { get; set; }

    [Column("effect_score")]
    public decimal? EffectScore { get; set; }

    [Column("philosophy_rationale_path"), MaxLength(512)]
    public string? PhilosophyRationalePath { get; set; }

    [Column("content_rationale_path"), MaxLength(512)]
    public string? ContentRationalePath { get; set; }

    [Column("process_rationale_path"), MaxLength(512)]
    public string? ProcessRationalePath { get; set; }

    [Column("effect_rationale_path"), MaxLength(512)]
    public string? EffectRationalePath { get; set; }

    [Column("report_store_path"), Required, MaxLength(512)]
    public string ReportStorePath { get; set; } = null!;

    [Column("raw_response_store_path"), MaxLength(512)]
    public string? RawResponseStorePath { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }
    
    [Column("micro_lesson_id")]
    public ulong? MicroLessonId { get; set; }

    [Column("phase_type"), MaxLength(32)]
    public string PhaseType { get; set; } = "InClass"; // 默认是课堂评价

    [ForeignKey(nameof(MicroLessonId))]
    public MicroLesson? MicroLesson { get; set; }


    [ForeignKey(nameof(VideoId))]
    public Video? Video { get; set; } = null!;


}