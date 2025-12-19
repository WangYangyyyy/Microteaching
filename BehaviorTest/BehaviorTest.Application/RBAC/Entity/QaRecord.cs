using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("qa_records")]
public class QaRecord : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("session_id")]
    public int? SessionId { get; set; }

    [Column("question"), Required, MaxLength(1024)]
    public string Question { get; set; } = null!;

    [Column("answers_json"), Required]
    public string AnswersJson { get; set; } = "[]";

    [Column("asked_at")]
    public DateTime AskedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
