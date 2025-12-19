using System.ComponentModel.DataAnnotations.Schema;

namespace BehaviorTest.Application.RBAC.Entity;

[Table("video_cuts")]
public class VideoCut : IEntity
{
    [Key, Column("id")]
    public ulong Id { get; set; }

    [Column("video_id")]
    public ulong VideoId { get; set; }

    [Column("segment_index")]
    public int SegmentIndex { get; set; }

    [Column("segment_path"), Required, MaxLength(512)]
    public string SegmentPath { get; set; } = null!;

    [Column("start_time")]
    public double StartTime { get; set; }

    [Column("end_time")]
    public double EndTime { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(VideoId))]
    public Video Video { get; set; } = null!;
}
