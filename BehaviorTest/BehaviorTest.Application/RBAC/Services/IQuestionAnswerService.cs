using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IQuestionAnswerService
{
    /// <summary>
    /// 保存一次教师提问与学生回答的集合
    /// </summary>
    Task<OperationResult> SaveAsync(QaSaveRequest request);

    /// <summary>
    /// 获取最近的问答记录，便于后续汇总发送给 LLM
    /// </summary>
    Task<List<QaRecord>> GetRecentAsync(int take = 20);
}
