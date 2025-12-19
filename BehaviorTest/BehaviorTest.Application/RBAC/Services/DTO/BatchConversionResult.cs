namespace BehaviorTest.Application.RBAC.Services.DTO;

public class BatchConversionResult
{
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedMessages { get; set; } = new List<string>();
}
