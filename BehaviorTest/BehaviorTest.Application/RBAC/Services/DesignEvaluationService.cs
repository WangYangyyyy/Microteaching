#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;


namespace BehaviorTest.Application.RBAC.Services
{
    public class DesignEvaluationService : ITransient, IDynamicApiController, IDesignEvaluationService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IRepository<LessonPlan> _lessonPlanRepository;
        private readonly IRepository<Evaluation> _evaluationRepository;
        private readonly HttpClient _httpClient;

        private readonly string _model = "qwen3-235b";
        private readonly string _apiUrl = "http://211.82.200.182:32788";
        private readonly string _apiKey = "YCwMHQK4yauM0eOj96BdD0102148450b9383D303B21b23A7";

        // 用于移除 <think>...</think>
        private static readonly Regex ThinkPattern =
            new Regex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public DesignEvaluationService(
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            IRepository<LessonPlan> lessonPlanRepository,
            IRepository<Evaluation> evaluationRepository)
        {
            _env = env;
            _lessonPlanRepository = lessonPlanRepository;
            _evaluationRepository = evaluationRepository;

            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }


        /// <summary>
        /// 上传导学案文件到 wwwroot/teachingplan，并在 lesson_plans 表插入一条记录
        /// </summary>
        public async Task<string> UploadTeachingPlanAsync(IBrowserFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var teachingPlanDir = Path.Combine(webRoot, "teachingplan");
            Directory.CreateDirectory(teachingPlanDir);

            var ext = Path.GetExtension(file.Name); // .docx
            var safeName = Path.GetFileNameWithoutExtension(file.Name);
            safeName = string.Join("_", safeName.Split(Path.GetInvalidFileNameChars()));

            var newFileName = $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";
            var fullPath = Path.Combine(teachingPlanDir, newFileName);

            const long maxFileSize = 30 * 1024 * 1024; // 30MB
            await using (var read = file.OpenReadStream(maxFileSize))
            await using (var write = new FileStream(fullPath, FileMode.Create))
            {
                await read.CopyToAsync(write);
            }

            // 相对路径，例如：teachingplan/xxx_20251114....docx
            var relativePath = Path.Combine("teachingplan", newFileName).Replace("\\", "/");

            // ⭐ 写入 lesson_plans 表
            // TODO：MicroLessonId 这里先写 0，你后续可以改成从 UI 传入真实的 microLessonId
            var lessonPlan = new LessonPlan
            {
                MicroLessonId = 1, // ⚠️ 先占位，等你有 MicroLesson 后改掉
                SourcePath = relativePath,
                OriginalName = file.Name,
                CreatedAt = DateTime.Now
            };

            await _lessonPlanRepository.InsertNowAsync(lessonPlan);

            return relativePath;
        }


        /// <summary>
        /// 读取 Word 文档内容，发送到大模型评分，并把结果写入 evaluations 表
        /// </summary>
        public async Task<EvaluationResultDto> EvaluateDesignAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");

            // 支持相对路径和绝对路径
            var fullPath = Path.IsPathRooted(filePath)
                ? filePath
                : Path.Combine(webRoot, filePath.TrimStart('/', '\\'));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("找不到指定的导学案文件", fullPath);

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext != ".docx")
                throw new NotSupportedException($"当前仅支持 .docx 文件，实际扩展名为 {ext}");

            // 1. 提取 Word 文本
            var docText = ExtractTextFromDocx(fullPath);
            
            // 打印一下长度和前几百个字符
            Console.WriteLine($"[DEBUG] Doc text length = {docText.Length}");
            Console.WriteLine("[DEBUG] Doc text preview:");
            Console.WriteLine(docText.Substring(0, Math.Min(300, docText.Length)));

            // 2. 调用 LLM 评分，拿到 EvaluationResultDto
            var evalResult = await CallLlmForLessonPlanAsync(docText);

            // 3. 找到对应的 LessonPlan 记录（按 SourcePath 匹配，统一相对路径格式）
            var normalizedPath = filePath.TrimStart('/', '\\').Replace("\\", "/");

            var lessonPlan = await _lessonPlanRepository.Entities
                .Where(lp => lp.SourcePath == normalizedPath)
                .FirstOrDefaultAsync();

            // 4. 生成一个简单的报告文件，存放评语和分数
            //    ⚠️ 改为保存到 wwwroot/teachingplan/evaluations 目录
            var evalDir = Path.Combine(webRoot, "teachingplan", "evaluations");
            Directory.CreateDirectory(evalDir);

            var reportFileName = $"lessonplan_eval_{DateTime.UtcNow:yyyyMMddHHmmssfff}.md";
            var reportFullPath = Path.Combine(evalDir, reportFileName);

            var reportContent = new StringBuilder();
            reportContent.AppendLine("# 导学案教学设计评价报告");
            reportContent.AppendLine();
            reportContent.AppendLine($"- 总分: {evalResult.TotalScore}");
            reportContent.AppendLine($"- 教学理念与目标: {evalResult.PhilosophyScore}");
            reportContent.AppendLine($"- 教学内容: {evalResult.ContentScore}");
            reportContent.AppendLine($"- 教学过程: {evalResult.ProcessScore}");
            reportContent.AppendLine($"- 教学效果预期: {evalResult.EffectScore}");
            reportContent.AppendLine();
            reportContent.AppendLine("## 综合评语");
            reportContent.AppendLine();
            reportContent.AppendLine(evalResult.Comment ?? string.Empty);

            await File.WriteAllTextAsync(reportFullPath, reportContent.ToString(), Encoding.UTF8);

            // 相对路径：teachingplan/evaluations/xxx.md
            var reportRelativePath = Path.Combine("teachingplan", "evaluations", reportFileName)
                .Replace("\\", "/");

            // 5. 写入 evaluations 表
            var evaluation = new Evaluation
            {
                // 这里目前没有绑定具体视频，可以先用 0 占位，之后如果有 Video 关联再改
                VideoId = null,

                MicroLessonId = lessonPlan?.MicroLessonId,
                PhaseType = "LessonPlan",

                RunUuid = Guid.NewGuid().ToString(),
                EvaluationModel = _model,
                InputSource = "lesson_plan",

                TotalScore = evalResult.TotalScore,
                PhilosophyScore = evalResult.PhilosophyScore,
                ContentScore = evalResult.ContentScore,
                ProcessScore = evalResult.ProcessScore,
                EffectScore = evalResult.EffectScore,

                ReportStorePath = reportRelativePath,
                RawResponseStorePath = null,

                StartedAt = DateTime.Now,
                FinishedAt = DateTime.Now
            };

            await _evaluationRepository.InsertNowAsync(evaluation);

            // 6. 返回给前端用于展示
            return evalResult;
        }


        /// <summary>
        /// 从 .docx 中提取纯文本
        /// </summary>
        private static string ExtractTextFromDocx(string filePath)
        {
            var sb = new StringBuilder();
            using var wordDoc = WordprocessingDocument.Open(filePath, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }

            return sb.ToString();
        }

        private async Task<EvaluationResultDto> CallLlmForLessonPlanAsync(string teachingPlanText)
        {
            var systemPrompt = @"
你是一名教学教研专家，请根据导学案从以下四个维度进行量化评价：
1. 教学理念与目标（philosophy_score，0-25 分）
2. 教学内容（content_score，0-25 分）
3. 教学过程设计（process_score，0-25 分）
4. 教学效果预期与评价设计（effect_score，0-25 分）

要求：
- 每个维度给出 0-25 分，总分 total_score 为 0-100。
- 分数可以是整数或一位小数。
- 给出一个简短的综合评语 comment。
- 只返回一个 JSON 对象。
JSON 示例：
{
  ""total_score"": 86,
  ""philosophy_score"": 22,
  ""content_score"": 23,
  ""process_score"": 21,
  ""effect_score"": 20,
  ""comment"": ""综合评语...""
}
";

            var userPrompt = $"以下是导学案内容，请根据上述标准进行评价：\n\n{teachingPlanText}";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"调用评分接口失败: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            // 1️⃣ 先解析外层结构，取出 choices[0].message.content
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;
            var contentJson = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            
            Console.WriteLine("[DEBUG] LLM raw content:");
            Console.WriteLine(contentJson);

            if (string.IsNullOrWhiteSpace(contentJson))
                throw new InvalidOperationException("评分接口返回内容为空");

            // 2️⃣ 尝试只保留 { ... } 这一段 JSON
            var firstBrace = contentJson.IndexOf('{');
            var lastBrace = contentJson.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                contentJson = contentJson.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            // 3️⃣ 反序列化为 EvaluationResultDto（属性名大小写不敏感）
            var result = JsonSerializer.Deserialize<EvaluationResultDto>(
                contentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
                throw new InvalidOperationException("无法解析评分结果 JSON：" + contentJson);

            // 可选：如果你希望兼容旧的 Score 字段，可以顺手赋值一下
            if (result.TotalScore != 0 && result.Score == 0)
            {
                result.Score = (int)Math.Round(result.TotalScore);
            }

            return result;
        }

        public async Task<List<LessonPlanListItemDto>> GetLessonPlansAsync()
        {
            var list = await _lessonPlanRepository
                .Entities
                .OrderByDescending(lp => lp.CreatedAt)
                .Select(lp => new LessonPlanListItemDto
                {
                    Id = lp.Id,
                    OriginalName = lp.OriginalName,
                    SourcePath = lp.SourcePath,
                    CreatedAt = lp.CreatedAt
                })
                .ToListAsync();

            return list;
        }
    }
}