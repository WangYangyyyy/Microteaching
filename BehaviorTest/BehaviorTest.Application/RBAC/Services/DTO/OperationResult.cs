namespace BehaviorTest.Application.RBAC.Services.DTO;

/// <summary>
/// 操作结果
/// </summary>
public class OperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 数据
    /// </summary>
    public object? Data { get; set; }

    public static OperationResult SuccessResult(string message = "操作成功", object? data = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static OperationResult FailureResult(string message = "操作失败")
    {
        return new OperationResult
        {
            Success = false,
            Message = message
        };
    }
}

