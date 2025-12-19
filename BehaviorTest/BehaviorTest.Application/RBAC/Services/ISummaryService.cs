using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface ISummaryService
{
    /// <summary>
    /// 根据 VideoId 生成所有关联转录的摘要
    /// </summary>
    /// <param name="videoId">视频的ID</param>
    /// <returns>批量生成结果</returns>
    Task<BatchConversionResult> GenerateSummariesForVideoAsync(ulong videoId);
}