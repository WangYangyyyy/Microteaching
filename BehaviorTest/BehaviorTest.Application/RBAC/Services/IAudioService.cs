using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IAudioService
{

    /// <summary>
    /// 根据 VideoId 转换所有关联的视频片段
    /// </summary>
    /// <param name="videoId">视频的ID</param>
    /// <returns>批量转换结果</returns>
    Task<BatchConversionResult> ConvertAllSegmentsForVideoAsync(ulong videoId);

    /// <summary>
    /// 转录音频文件
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputDir">输出目录</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> TranscribeAudioFileAsync(string audioPath, string outputDir);


    Task<BatchConversionResult> TranscribeAllSegmentsForVideoAsync(ulong videoId);
}