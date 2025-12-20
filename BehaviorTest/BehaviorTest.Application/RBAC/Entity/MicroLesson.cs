using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("micro_lessons")]// 课次表
public class MicroLesson:IPrivateEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("teacher_id")]
    public int TeacherId { get; set; }

    [Column("title"), MaxLength(255)]
    public string Title { get; set; } = null!;

    [Column("description"), MaxLength(512)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Video> Videos { get; set; } = new List<Video>();
    public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
}