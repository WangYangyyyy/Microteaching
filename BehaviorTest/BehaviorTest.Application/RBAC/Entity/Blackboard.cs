using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("blackboards")]
public class Blackboard : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("micro_lesson_id")]
    public ulong MicroLessonId { get; set; }

    [Column("image_path"), MaxLength(512)]
    public string ImagePath { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(MicroLessonId))]
    public MicroLesson MicroLesson { get; set; } = null!;
}
