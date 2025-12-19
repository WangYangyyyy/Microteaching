using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public class QaEvaluationService : IQaEvaluationService, IDynamicApiController, ITransient
{
    private readonly IRepository<QaRecord> _qaRepository;
    private readonly HttpClient _httpClient;
    private const string ApiUrl = "http://211.82.200.182:32788";
    private const string Model = "qwen3-235b";
    private const string ApiKey = "YCwMHQK4yauM0eOj96BdD0102148450b9383D303B21b23A7";

    public QaEvaluationService(IRepository<QaRecord> qaRepository, IHttpClientFactory httpClientFactory)
    {
        _qaRepository = qaRepository;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<OperationResult> EvaluateAsync(QaEvaluationRequest request)
    {
        var take = Math.Clamp(request.Take, 1, 50);
        var query = _qaRepository.AsQueryable();

        if (request.QaIds is { Count: > 0 })
        {
            query = query.Where(q => request.QaIds.Contains(q.Id));
        }
        else if (request.SessionId.HasValue)
        {
            query = query.Where(q => q.SessionId == request.SessionId);
        }

        var records = await query.OrderByDescending(q => q.CreatedAt)
            .Take(request.QaIds is { Count: > 0 } ? request.QaIds.Count : take)
            .ToListAsync();

        if (!records.Any())
        {
            return OperationResult.FailureResult("未找到可用的问答记录。");
        }

        var blocks = new List<string>();
        foreach (var r in records)
        {
            var answers = new List<StudentAnswerDto>();
            try
            {
                var parsed = JsonSerializer.Deserialize<List<StudentAnswerDto>>(r.AnswersJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) answers = parsed;
            }
            catch
            {
                // ignore parse errors
            }

            var answersText = answers.Any()
                ? string.Join("\n", answers.Select(a => $"- {a.Name ?? "学生"}({a.Role ?? "角色"}): {a.Answer}"))
                : "- 暂无回答";

            blocks.Add($"""
问题：{r.Question}
时间：{r.AskedAt:yyyy-MM-dd HH:mm:ss} (本地)
回答：
{answersText}
""");
        }

        var systemPrompt = string.IsNullOrWhiteSpace(request.SystemPrompt)
            ? "你是教学督导，请对以下课堂问答进行评估：指出回答的正确性、深度、清晰度，选择最佳回答来源，并给出教师改进建议（3-5条，简洁）。"
            : request.SystemPrompt;

        var userContent = "以下是按时间排序的课堂问答，请逐题给出评语与评分(1-5)，最后给出总体建议。\n\n" +
                          string.Join("\n----------------\n", blocks);

        var payload = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var resp = await _httpClient.PostAsync("v1/chat/completions", content);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                return OperationResult.FailureResult($"LLM 调用失败: {resp.StatusCode}, {err}");
            }

            var body = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
                return OperationResult.FailureResult("LLM 返回为空。");

            var result = choices[0].GetProperty("message").GetProperty("content").GetString();
            return OperationResult.SuccessResult("评估完成", result ?? string.Empty);
        }
        catch (HttpRequestException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return OperationResult.FailureResult($"调用 LLM 时网络异常: {detail}");
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"调用 LLM 发生异常: {ex.Message}");
        }
    }
}
