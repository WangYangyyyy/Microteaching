// Models/ChatDtos.cs
using System.Text.Json.Serialization;

// 用于对话历史和请求
public record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

// API 请求主体
public record ChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<ChatMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream = false,
    [property: JsonPropertyName("max_tokens")] int? MaxTokens = null,
    [property: JsonPropertyName("temperature")] double? Temperature = null
);

// API 响应的简化结构
public record ChatResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice> Choices { get; set; } = new();
}

public record ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}

// 用于存储评估结果的结构化对象
public record DimensionEvaluationResult(
    string DimensionName,
    string MarkdownReport,
    int MaxScore
);

public record ComprehensiveEvaluationResult
{
    public DimensionEvaluationResult? PhilosophyAndGoals { get; set; }
    public DimensionEvaluationResult? Content { get; set; }
    public DimensionEvaluationResult? Process { get; set; }
    public DimensionEvaluationResult? Effect { get; set; }
    public string FinalReport { get; set; } = string.Empty;
}