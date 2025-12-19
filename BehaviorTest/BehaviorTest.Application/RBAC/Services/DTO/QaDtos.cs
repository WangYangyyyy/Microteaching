using System.Text.Json.Serialization;

namespace BehaviorTest.Application.RBAC.Services.DTO;

public class StudentAnswerDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("answer")]
    public string? Answer { get; set; }
    
    [JsonPropertyName("audio_base64")]
    public string? audio_base64 { get; set; }
}

public class QaSaveRequest
{
    public int? SessionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;
    public List<StudentAnswerDto> Answers { get; set; } = new();
}
