using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public class SummaryService : ISummaryService
{
    private readonly IRepository<Transcript> _transcriptRepository;
    private readonly IRepository<Summary> _summaryRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiUrl = "http://211.82.200.182:32788";
    private readonly string _model = "qwen3-235b";
    private readonly string _apiKey = "YCwMHQK4yauM0eOj96BdD0102148450b9383D303B21b23A7";

    public SummaryService(IRepository<Transcript> transcriptRepository, IRepository<Summary> summaryRepository, IHttpClientFactory httpClientFactory)
    {
        _transcriptRepository = transcriptRepository;
        _summaryRepository = summaryRepository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<BatchConversionResult> GenerateSummariesForVideoAsync(ulong videoId)
    {
        var result = new BatchConversionResult();

        var transcripts = await _transcriptRepository.Where(t => t.VideoId == videoId).ToListAsync();
        result.TotalFiles = transcripts.Count;

        if (result.TotalFiles == 0)
        {
            result.FailedMessages.Add("没有找到转录记录。");
            return result;
        }

        foreach (var transcript in transcripts)
        {
            // transcript.TextStorePath is expected to be .../processing/<videoName>/transcripts/<file>.txt
            var transcriptDir = Path.GetDirectoryName(transcript.TextStorePath)!; // .../processing/<videoName>/transcripts
            var processingDir = Path.GetDirectoryName(transcriptDir)!; // .../processing/<videoName>
            var summaryDir = Path.Combine(processingDir, "summaries");
            Directory.CreateDirectory(summaryDir);
            var summaryPath = Path.Combine(summaryDir, Path.GetFileNameWithoutExtension(transcript.TextStorePath) + "_summary.md");

            var singleResult = await GenerateSummaryAsync(transcript.TextStorePath, summaryPath, transcript.RunUuid, transcript.VideoId);

            if (singleResult.Success)
            {
                result.SuccessCount++;
            }
            else
            {
                var fileName = Path.GetFileName(transcript.TextStorePath);
                result.FailedMessages.Add($"文件 '{fileName}' 摘要生成失败: {singleResult.Message}");
            }
        }

        result.FailureCount = result.TotalFiles - result.SuccessCount;

        return result;
    }

    private async Task<OperationResult> GenerateSummaryAsync(string transcriptPath, string summaryPath, string runUuid, ulong videoId)
    {
        try
        {
            if (!File.Exists(transcriptPath))
            {
                return OperationResult.FailureResult("转录文件不存在");
            }

            var transcriptText = await File.ReadAllTextAsync(transcriptPath);
            var prompt = $"""
    你是一位善于提炼中文课堂/讲座内容的助教。请对以下文本内容做摘要。
    任务要求:
    1. 先抽取本段的核心要点(知识点/观点/结论)。
    2. 用简洁中文列出关键术语或概念并做一句话解释。
    3. 提炼出本段包含的逻辑结构或推理链(如果有)。
    4. 标注任何重要的数字、公式、定义。
    5. 归纳可能的考试/复习重点 (若无可略)。
    6. 输出格式使用 Markdown, 结构如下:

    ### 分块摘要
    - 核心要点:
      - ...
    - 关键概念:
      - 概念: 解释
    - 逻辑结构:
      - ...
    - 重要数据或定义:
      - ...
    - 复习/考试提示:
      - ...

    源文本(保持原文不再复述, 直接分析):
{transcriptText}
""";

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return OperationResult.FailureResult($"摘要请求失败: {response.StatusCode}, {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var summary = choices[0].GetProperty("message").GetProperty("content").GetString();
                if (string.IsNullOrEmpty(summary))
                {
                    return OperationResult.FailureResult("API 返回的摘要内容为空");
                }
                await File.WriteAllTextAsync(summaryPath, summary);

                // 保存到数据库
                var summaryEntity = new Summary
                {
                    VideoId = videoId,
                    RunUuid = runUuid,
                    Source = "transcript",
                    SummaryModel = _model,
                    SummaryStorePath = summaryPath,
                    CharactersCount = (uint)summary.Length,
                    GeneratedAt = DateTime.UtcNow
                };

                await _summaryRepository.InsertNowAsync(summaryEntity);

                return OperationResult.SuccessResult("摘要生成完成");
            }
            else
            {
                return OperationResult.FailureResult("API 返回中没有摘要内容");
            }
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"摘要生成出错: {ex.Message}");
        }
    }
}
