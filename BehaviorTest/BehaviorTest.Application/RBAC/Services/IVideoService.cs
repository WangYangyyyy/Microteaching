using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IVideoService
{

    /// <summary>
    /// 上传视频（用于 Blazor）
    /// </summary>
    Task<OperationResult> UploadVideoAsync(string fileName, Stream stream, string contentType);
    
    /// <summary>
    /// 上传视频（视频流）
    /// </summary>
    Task<OperationResult> UploadVideoAsync1(Stream stream, string? originalFileName = null, string? contentType = null);

    /// <summary>
    /// 获取所有视频
    /// </summary>
    Task<List<VideoInfoDto>> GetAllVideosAsync();

    /// <summary>
    /// 删除视频
    /// </summary>
    Task<OperationResult> DeleteVideoAsync(ulong id);
    

    /// <summary>
    /// 切割视频（按每10分钟）
    /// </summary>
    Task<OperationResult> CutVideoAsync(ulong videoId);

    // 录制视频
    Task<OperationResult> StartRecordingAsync(string streamUrl);
    Task<OperationResult> StopRecordingAsync(Guid recordingId);
    
    Task<string> GetVideoFileNameAsync (ulong videoId);
}
