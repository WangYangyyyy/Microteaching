using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("summaries")]
public class Summary : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("video_id")]
    public ulong VideoId { get; set; }

    [Column("run_uuid"), Required, MaxLength(36)]
    public string RunUuid { get; set; } = null!;

    [Column("source"), Required, MaxLength(16)]
    public string Source { get; set; } = null!;

    [Column("related_transcript_id")]
    public ulong? RelatedTranscriptId { get; set; }

    [Column("summary_model"), Required, MaxLength(128)]
    public string SummaryModel { get; set; } = null!;

    [Column("summary_store_path"), Required, MaxLength(512)]
    public string SummaryStorePath { get; set; } = null!;

    [Column("characters_count")]
    public uint? CharactersCount { get; set; }

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [ForeignKey(nameof(VideoId))]
    public Video Video { get; set; } = null!;

    [ForeignKey(nameof(RelatedTranscriptId))]
    public Transcript? RelatedTranscript { get; set; }

}