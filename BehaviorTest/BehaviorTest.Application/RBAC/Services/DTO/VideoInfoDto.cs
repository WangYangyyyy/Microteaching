namespace BehaviorTest.Application.RBAC.Services.DTO;

/// <summary>
/// 视频信息传输对象
/// </summary>
public class VideoInfoDto
{
    /// <summary>
    /// 视频ID
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件访问路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 微课id
    /// </summary>
    public ulong MicroLessonId { get; set; }
}

