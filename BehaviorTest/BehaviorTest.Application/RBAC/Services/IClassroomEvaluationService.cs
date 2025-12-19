#nullable enable

using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IClassroomEvaluationService
{
    /// <summary>
    /// 重置对话历史，开始一次新的评估
    /// </summary>
    void ResetConversation();

    /// <summary>
    /// 执行一个完整的四维度综合评估
    /// </summary>
    /// <param name="transcriptContent">课堂转录内容</param>
    /// <returns>包含所有评估结果的对象</returns>
    Task<ComprehensiveEvaluationResult> EvaluateComprehensiveAsync(string transcriptContent);

    /// <summary>
    /// 执行一次快速评估，所有步骤在一次API调用中完成
    /// </summary>
    Task<string?> QuickEvaluateAsync(string transcriptContent);
    
    /// <summary>
    ///  根据 VideoId 评估并保存报告
    /// </summary>
    /// <param name="videoId"></param>
    /// <returns></returns>
    Task<OperationResult> EvaluateAndSaveReportForVideoAsync(ulong videoId);
    
    Task<string?> GetEvaluationReportForVideoAsync(ulong videoId);

}