using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IQaEvaluationService
{
    /// <summary>
    /// 将问答列表发送到 LLM 进行评估
    /// </summary>
    Task<OperationResult> EvaluateAsync(QaEvaluationRequest request);
}
