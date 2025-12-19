using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("transcripts")]
public class Transcript : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }
    
    [Column("video_id")]
    public ulong VideoId { get; set; }

    [Column("run_uuid"), Required, MaxLength(36)]
    public string RunUuid { get; set; } = null!;

    [Column("segment_index")]
    public uint SegmentIndex { get; set; }

    [Column("transcriber_model"), Required, MaxLength(128)]
    public string TranscriberModel { get; set; } = null!;

    [Column("language_code"), MaxLength(16)]
    public string LanguageCode { get; set; } = "zh";

    [Column("text_store_path"), Required, MaxLength(512)]
    public string TextStorePath { get; set; } = null!;

    [Column("characters_count")]
    public uint? CharactersCount { get; set; }

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [ForeignKey(nameof(VideoId))]
    public Video Video { get; set; } = null!;

    public ICollection<Summary> Summaries { get; set; } = new List<Summary>();

}