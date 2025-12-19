#nullable enable

// Services/DemoEvaluationService.cs

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DemoService;
using BehaviorTest.Application.RBAC.Services.DTO;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BehaviorTest.Application.RBAC.Services
{
    /// <summary>
    /// Demo 专用的课堂“建议”服务：
    /// - 基于邯郸学院理论课评价标准（6 个观察点、21 个条目）
    /// - 不打分，只生成改进建议报告
    /// - 报告最终以 PDF 形式保存
    /// </summary>
    public class DemoEvaluationService : ITransient, IDynamicApiController, IDemoEvaluationService
    {
        private readonly IRepository<VideoCut> _videoCutRepository;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;

        private readonly string _model = "qwen3-235b";
        private readonly string _apiUrl = "http://211.82.200.182:32788";
        private readonly string _apiKey = "YCwMHQK4yauM0eOj96BdD0102148450b9383D303B21b23A7";

        private readonly List<ChatMessage> _conversationHistory = new();

        private static readonly Regex ThinkPattern =
            new Regex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public DemoEvaluationService(
            IHttpClientFactory httpClientFactory,
            IRepository<VideoCut> videoCutRepository,
            IWebHostEnvironment env)
        {
            _videoCutRepository = videoCutRepository;
            _env = env;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        /// <summary>重置对话历史，并注入 Demo 的系统提示词</summary>
        public void ResetConversation()
        {
            _conversationHistory.Clear();
            _conversationHistory.Add(new ChatMessage("system", DemoPrompts.SystemPrompt));
        }

        /// <summary>
        /// 对一段完整的课堂文本生成“建议报告”（六个观察点 + 综合报告）
        /// </summary>
        public async Task<string?> SuggestAsync(string transcriptContent)
        {
            Console.WriteLine("=== Demo 建议模式：开始生成建议（新标准） ===");

            ResetConversation();

            // 1. 送入转录/摘要
            var transcriptMessage = DemoPrompts.GetTranscriptSubmissionMessage(transcriptContent);
            await CallChatApiAsync(transcriptMessage);

            // 2. 六个观察点分别提建议
            var goals = await CallChatApiAsync(DemoPrompts.TeachingGoalsSuggestionPrompt)
                        ?? "【教学目标】建议生成失败。";
            var content = await CallChatApiAsync(DemoPrompts.TeachingContentSuggestionPrompt)
                          ?? "【教学内容】建议生成失败。";
            var methods = await CallChatApiAsync(DemoPrompts.TeachingMethodsSuggestionPrompt)
                          ?? "【教学方法】建议生成失败。";
            var effect = await CallChatApiAsync(DemoPrompts.TeachingEffectSuggestionPrompt)
                         ?? "【教学效果】建议生成失败。";
            var attitude = await CallChatApiAsync(DemoPrompts.TeachingAttitudeSuggestionPrompt)
                           ?? "【教学态度】建议生成失败。";
            var ethics = await CallChatApiAsync(DemoPrompts.ProfessionalEthicsSuggestionPrompt)
                         ?? "【职业道德】建议生成失败。";

            // 3. 综合报告
            var finalReport = await CallChatApiAsync(DemoPrompts.FinalSuggestionReportPrompt)
                              ?? "综合建议生成失败。";

            // 4. 整体拼成一个 Markdown 文本
            var sb = new StringBuilder();
            sb.AppendLine("# 课堂教学改进建议（理论课·Demo）");
            sb.AppendLine();
            sb.AppendLine("以下内容基于《邯郸学院课堂教学评价标准（理论课）》六个观察点生成，仅供教学改进参考。");
            sb.AppendLine();
            sb.AppendLine("## 观察点一：教学目标");
            sb.AppendLine(goals);
            sb.AppendLine();
            sb.AppendLine("## 观察点二：教学内容");
            sb.AppendLine(content);
            sb.AppendLine();
            sb.AppendLine("## 观察点三：教学方法");
            sb.AppendLine(methods);
            sb.AppendLine();
            sb.AppendLine("## 观察点四：教学效果");
            sb.AppendLine(effect);
            sb.AppendLine();
            sb.AppendLine("## 观察点五：教学态度");
            sb.AppendLine(attitude);
            sb.AppendLine();
            sb.AppendLine("## 观察点六：职业道德");
            sb.AppendLine(ethics);
            sb.AppendLine();
            sb.AppendLine("## 综合改进建议报告");
            sb.AppendLine(finalReport);

            Console.WriteLine("=== Demo 建议模式：生成完成 ===");

            return sb.ToString();
        }

        /// <summary>
        /// 按 videoId 汇总 summaries -> 生成建议 -> 保存为 PDF 到 processing/&lt;video&gt;/evaluations
        /// </summary>
        public async Task<OperationResult> EvaluateAndSaveSuggestionsForVideoAsync(ulong videoId)
        {
            try
            {
                Console.WriteLine($"开始处理视频 {videoId} 的建议生成。");

                var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
                if (!cuts.Any())
                {
                    Console.WriteLine("未找到任何片段，无法生成建议。");
                    return OperationResult.FailureResult("没有找到任何片段，无法生成建议。");
                }

                var firstSegmentDir = Path.GetDirectoryName(cuts.First().SegmentPath)!;
                var videoDir = Path.GetDirectoryName(firstSegmentDir)!;

                var summariesDir = Path.Combine(videoDir, "summaries");
                var transcriptContents = new List<string>();

                if (Directory.Exists(summariesDir))
                {
                    var transcriptFiles = Directory
                        .EnumerateFiles(summariesDir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f =>
                            f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

                    foreach (var file in transcriptFiles)
                    {
                        var content = await File.ReadAllTextAsync(file);
                        transcriptContents.Add(content);
                    }
                }

                if (!transcriptContents.Any())
                {
                    Console.WriteLine("未找到摘要内容，无法生成建议。");
                    return OperationResult.FailureResult("没有找到摘要内容，无法生成建议。");
                }

                var combinedTranscript = string.Join("\n\n", transcriptContents);
                Console.WriteLine($"汇总了 {transcriptContents.Count} 个摘要文件，总长度：{combinedTranscript.Length} 字符。");

                var suggestionReportMarkdown = await SuggestAsync(combinedTranscript)
                                              ?? "生成完成，但没有任何建议内容。";
                Console.WriteLine("建议报告生成完成。");

                var evaluationDir = Path.Combine(videoDir, "evaluations");
                Directory.CreateDirectory(evaluationDir);

                var fileName = $"demo_suggestions_{videoId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var reportPath = Path.Combine(evaluationDir, fileName);

                
                // 使用 QuestPDF 生成 PDF
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.UseEnvironmentFonts = true;
                QuestPDF.Settings.FontDiscoveryPaths.Add("/usr/share/fonts");
                var doc = new DemoSuggestionReportDocument
                {
                    Title = $"课堂教学改进建议报告（理论课）- 视频 {videoId}",
                    BodyMarkdown = suggestionReportMarkdown
                };
                var pdfBytes = doc.GeneratePdf();
                await File.WriteAllBytesAsync(reportPath, pdfBytes);
                Console.WriteLine($"PDF 报告已保存到：{reportPath}");

                return OperationResult.SuccessResult($"建议生成完成，PDF 报告已保存到：{reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成建议时出错：{ex.Message}");
                return OperationResult.FailureResult($"生成建议出错：{ex.Message}");
            }
        }


        /// <summary>
        /// 获取指定视频最新的 Demo 建议 PDF 报告 URL（复制到 wwwroot/evaluations 下）
        /// </summary>
        public async Task<string?> GetSuggestionReportForVideoAsync(ulong videoId)
        {
            try
            {
                string[] exts = { ".pdf", ".docx", ".html", ".htm", ".txt", ".md" };
                var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
                var webEvalDir = Path.Combine(webRoot, "evaluations");
                Directory.CreateDirectory(webEvalDir);

                // 优先从 processing 的 evaluations 中找 demo_suggestions 开头的最新 PDF
                var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
                if (cuts.Any())
                {
                    var firstSegmentDir = Path.GetDirectoryName(cuts.First().SegmentPath)!;
                    var videoDir = Path.GetDirectoryName(firstSegmentDir)!;
                    var processingEvalDir = Path.Combine(videoDir, "evaluations");

                    if (Directory.Exists(processingEvalDir))
                    {
                        var candidateFileInfo = Directory
                            .EnumerateFiles(processingEvalDir, $"demo_suggestions_{videoId}_*.*", SearchOption.TopDirectoryOnly)
                            .Select(f => new FileInfo(f))
                            .Where(fi => exts.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                            .OrderByDescending(fi => fi.LastWriteTimeUtc)
                            .FirstOrDefault();

                        if (candidateFileInfo != null)
                        {
                            var fileName = candidateFileInfo.Name;
                            var destPath = Path.Combine(webEvalDir, fileName);

                            var needCopy = !File.Exists(destPath) ||
                                           candidateFileInfo.LastWriteTimeUtc >
                                           File.GetLastWriteTimeUtc(destPath);

                            if (needCopy)
                            {
                                File.Copy(candidateFileInfo.FullName, destPath, true);
                            }

                            return "/evaluations/" + fileName.Replace("\\", "/");
                        }
                    }
                }

                // 回退：直接在 wwwroot/evaluations 下按 videoId 搜索
                var fallbackFile = Directory
                    .EnumerateFiles(webEvalDir, $"*{videoId}*.*", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileInfo(f))
                    .Where(fi => exts.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                    .OrderByDescending(fi => fi.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (fallbackFile != null)
                {
                    return "/evaluations/" + fallbackFile.Name.Replace("\\", "/");
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>调用 Chat Completions API，并维护对话历史</summary>
        private async Task<string?> CallChatApiAsync(string userMessage)
        {
            _conversationHistory.Add(new ChatMessage("user", userMessage));

            var request = new ChatRequest(_model, _conversationHistory)
            {
                MaxTokens = 10_000,
                Temperature = 0
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_apiUrl}/v1/chat/completions", content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.Error.WriteLine(
                        $"Demo API 请求失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1);
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<ChatResponse>(responseJson);

                var assistantMessageContent = responseData?.Choices.FirstOrDefault()?.Message?.Content ?? "";

                _conversationHistory.Add(new ChatMessage("assistant", assistantMessageContent));

                var strippedContent = StripThinkBlocks(assistantMessageContent);
                Console.WriteLine($"API响应: {strippedContent}");

                return strippedContent;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"调用 Demo API 时发生错误: {ex.Message}");
                _conversationHistory.RemoveAt(_conversationHistory.Count - 1);
                return null;
            }
        }


        private static string StripThinkBlocks(string text)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : ThinkPattern.Replace(text, "").Trim();
        }
    }
}
