using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("lesson_plans")]// 教案表
public class LessonPlan : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("micro_lesson_id")]
    public ulong MicroLessonId { get; set; }

    [Column("source_path"), MaxLength(512)]
    public string SourcePath { get; set; } = null!;

    [Column("original_name"), MaxLength(255)]
    public string OriginalName { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(MicroLessonId))]
    public MicroLesson MicroLesson { get; set; } = null!;
}
