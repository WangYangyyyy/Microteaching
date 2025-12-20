using System.Text.Json.Serialization;

namespace BehaviorTest.Application.RBAC.Services.DTO;

public class EvaluationResultDto
{
    /// <summary>
    /// （可选）旧字段，如果不用可以删除
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// （可选）综合报告文本，如果不用也可以删掉
    /// </summary>
    public string Report { get; set; } = string.Empty;

    /// <summary>
    /// 总分（0-100），对应 JSON: total_score
    /// </summary>
    [JsonPropertyName("total_score")]
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 教学理念与目标，对应 JSON: philosophy_score
    /// </summary>
    [JsonPropertyName("philosophy_score")]
    public decimal PhilosophyScore { get; set; }

    /// <summary>
    /// 教学内容，对应 JSON: content_score
    /// </summary>
    [JsonPropertyName("content_score")]
    public decimal ContentScore { get; set; }

    /// <summary>
    /// 教学过程，对应 JSON: process_score
    /// </summary>
    [JsonPropertyName("process_score")]
    public decimal ProcessScore { get; set; }

    /// <summary>
    /// 教学效果预期，对应 JSON: effect_score
    /// </summary>
    [JsonPropertyName("effect_score")]
    public decimal EffectScore { get; set; }

    /// <summary>
    /// 综合评语，对应 JSON: comment
    /// </summary>
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
    
    /// <summary>
    /// 改进建议，对应 JSON: improvements
    /// </summary>
    [JsonPropertyName("improvements")]
    public string Improvements { get; set; } = string.Empty;
}