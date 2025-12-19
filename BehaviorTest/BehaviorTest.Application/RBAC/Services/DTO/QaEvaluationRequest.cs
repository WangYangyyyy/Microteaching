namespace BehaviorTest.Application.RBAC.Services.DTO;

public class QaEvaluationRequest
{
    /// <summary>
    /// 课堂 sessionId，留空则取最近记录
    /// </summary>
    public int? SessionId { get; set; }

    /// <summary>
    /// 指定的问答记录 Id 列表，优先于 SessionId
    /// </summary>
    public List<ulong>? QaIds { get; set; }

    /// <summary>
    /// 拉取条数
    /// </summary>
    public int Take { get; set; } = 10;

    /// <summary>
    /// 可选自定义系统提示
    /// </summary>
    public string? SystemPrompt { get; set; }
}
