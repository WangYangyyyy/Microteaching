#nullable enable

// Services/ClassroomEvaluationService.cs

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;
using Microsoft.AspNetCore.Hosting;

namespace BehaviorTest.Application.RBAC.Services
{
    public class ClassroomEvaluationService : ITransient, IDynamicApiController, IClassroomEvaluationService
    {
        private readonly IRepository<Evaluation> _evaluationRepository;
        private readonly IRepository<VideoCut> _videoCutRepository;
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;
        private readonly string _model = "qwen3-235b";
        private readonly string _apiUrl = "http://211.82.200.182:32788";
        private readonly string _apiKey = "YCwMHQK4yauM0eOj96BdD0102148450b9383D303B21b23A7";

        // 对话历史，用于维护上下文
        private List<ChatMessage> _conversationHistory = new();

        // 正则表达式，用于移除模型可能输出的思考块
        private static readonly Regex ThinkPattern =
            new Regex(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public ClassroomEvaluationService(
            IHttpClientFactory httpClientFactory,
            IRepository<Evaluation> evaluationRepository,
            IRepository<VideoCut> videoCutRepository,
            IWebHostEnvironment env
        )
        {
            _evaluationRepository = evaluationRepository;
            _videoCutRepository = videoCutRepository;
            _httpClient = httpClientFactory.CreateClient();
            _env = env;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        /// <summary>
        /// 重置对话历史，开始一次新的评估
        /// </summary>
        public void ResetConversation()
        {
            _conversationHistory.Clear();
            // 每次重置时都加入核心的系统角色设定
            _conversationHistory.Add(new ChatMessage("system", Prompts.SystemPrompt));
        }

        /// <summary>
        /// 执行一个完整的四维度综合评估
        /// </summary>
        /// <param name="transcriptContent">课堂转录内容</param>
        /// <returns>包含所有评估结果的对象</returns>
        public async Task<ComprehensiveEvaluationResult> EvaluateComprehensiveAsync(string transcriptContent)
        {
            Console.WriteLine("=== 开始四维度综合评估 ===");

            // 1. 重置并初始化
            ResetConversation();
            await CallChatApiAsync("请确认您已准备好开始课堂转录评估工作。");

            // 2. 提交转录内容，让模型在上下文中记住它
            var transcriptMessage = Prompts.GetTranscriptSubmissionMessage(transcriptContent);
            await CallChatApiAsync(transcriptMessage);

            var results = new ComprehensiveEvaluationResult();

            // 3. 依序评估四个维度
            results.PhilosophyAndGoals = await EvaluateDimensionAsync("教学理念和目标", Prompts.PhilosophyGoalsCriteria, 25);
            Console.WriteLine(results.PhilosophyAndGoals);
            results.Content = await EvaluateDimensionAsync("教学内容", Prompts.ContentCriteria, 35);
            Console.WriteLine(results.Content);
            results.Process = await EvaluateDimensionAsync("教学过程", Prompts.ProcessCriteria, 25);
            Console.WriteLine(results.Process);
            results.Effect = await EvaluateDimensionAsync("教学效果", Prompts.EffectCriteria, 15);
            Console.WriteLine(results.Effect);

            // 4. 生成最终的综合报告
            Console.WriteLine("=== 生成最终综合报告 ===");
            var finalReport = await CallChatApiAsync(Prompts.FinalReportGenerationPrompt);
            finalReport = results.PhilosophyAndGoals + "\n" +
                          results.Content + "\n" +
                          results.Process + "\n" +
                          results.Effect + "\n\n" +
                          finalReport;
            results.FinalReport = finalReport ?? "报告生成失败。";

            Console.WriteLine("=== 四维度综合评估完成 ===");
            Console.WriteLine(finalReport);
            // 5. 插入评估记录到数据库
            return results;
        }

        /// <summary>
        /// 执行一次快速评估，所有步骤在一次API调用中完成
        /// </summary>
        public async Task<string?> QuickEvaluateAsync(string transcriptContent)
        {
            Console.WriteLine("=== 快速评估模式 ===");
            ResetConversation();

            var message = Prompts.GetQuickEvaluationMessage(transcriptContent);
            var result = await CallChatApiAsync(message);

            Console.WriteLine("评估结果:");
            Console.WriteLine(new string('=', 50));
            Console.WriteLine(result);
            Console.WriteLine(new string('=', 50));

            return result;
        }

        /// <summary>
        /// 评估单一维度的通用方法
        /// </summary>
        private async Task<DimensionEvaluationResult> EvaluateDimensionAsync(string dimensionName, string criteria,
            int maxScore)
        {
            Console.WriteLine($"=== 正在评估维度: {dimensionName} ===");
            var response = await CallChatApiAsync(criteria);
            Console.WriteLine($"--- {dimensionName} 评估完成 ---");
            return new DimensionEvaluationResult(dimensionName, response ?? "评估失败", maxScore);
        }

        /// <summary>
        /// 私有的核心方法，用于调用 Chat Completions API
        /// </summary>
        private async Task<string?> CallChatApiAsync(string userMessage)
        {
            // 将当前用户信息加入对话历史（用于下一次请求）
            _conversationHistory.Add(new ChatMessage("user", userMessage));

            var request = new ChatRequest(_model, _conversationHistory)
            {
                MaxTokens = 10000,
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
                        $"API 请求失败: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    _conversationHistory.RemoveAt(_conversationHistory.Count - 1); // 请求失败，复原历史
                    return null;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<ChatResponse>(responseJson);

                var assistantMessageContent = responseData?.Choices.FirstOrDefault()?.Message?.Content ?? "";

                // 将模型的完整回答（包含可能的<think>块）加入历史，以保持上下文完整
                _conversationHistory.Add(new ChatMessage("assistant", assistantMessageContent));

                // 返回给用户时，移除<think>块
                return StripThinkBlocks(assistantMessageContent);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"调用 API 时发生错误: {ex.Message}");
                _conversationHistory.RemoveAt(_conversationHistory.Count - 1); // 异常发生，复原历史
                return null;
            }
        }


        /// <summary>
        /// 将页面中的 StartEvaluatingTeaching 逻辑移动到服务层：
        /// - 根据 videoId 找到 processing 根目录下的 summaries
        /// - 合并内容并调用 EvaluateComprehensiveAsync
        /// - 将报告保存到 processing/<videoName>/evaluations 下
        /// </summary>
        public async Task<OperationResult> EvaluateAndSaveReportForVideoAsync(ulong videoId)
        {
            try
            {
                var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
                if (!cuts.Any())
                {
                    return OperationResult.FailureResult("没有找到任何片段，无法进行评估。");
                }

                var firstSegmentDir = Path.GetDirectoryName(cuts.First().SegmentPath)!;
                var videoDir = Path.GetDirectoryName(firstSegmentDir)!;

                var summariesDir = Path.Combine(videoDir, "summaries");
                var transcriptContents = new List<string>();

                if (Directory.Exists(summariesDir))
                {
                    var transcriptFiles = Directory.GetFiles(summariesDir, "*.md");
                    foreach (var file in transcriptFiles)
                    {
                        var content = await File.ReadAllTextAsync(file);
                        transcriptContents.Add(content);
                    }
                }

                if (!transcriptContents.Any())
                {
                    return OperationResult.FailureResult("没有找到摘要内容，无法进行评估。");
                }

                var combinedTranscript = string.Join("\n\n", transcriptContents);

                var result = await EvaluateComprehensiveAsync(combinedTranscript);

                var evaluationDir = Path.Combine(videoDir, "evaluations");
                Directory.CreateDirectory(evaluationDir);
                var reportPath = Path.Combine(evaluationDir, $"evaluation_{videoId}_{DateTime.UtcNow:yyyyMMddHHmmss}.md");
                await File.WriteAllTextAsync(reportPath, result.FinalReport ?? "评估完成，但没有生成报告。");

                // 可选：将评估记录保存到数据库（如果需要）
                // await _evaluationRepository.InsertNowAsync(new Evaluation { VideoId = videoId, ReportPath = reportPath, CreatedAt = DateTime.UtcNow });

                return OperationResult.SuccessResult($"评估完成，报告已保存到：{reportPath}");
            }
            catch (Exception ex)
            {
                return OperationResult.FailureResult($"评估出错：{ex.Message}");
            }
        }


        public async Task<string?> GetEvaluationReportForVideoAsync(ulong videoId)
        {
            try
            {
                string[] exts = { ".pdf", ".docx", ".html", ".htm", ".txt", ".md" };
                var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
                var webEvalDir = Path.Combine(webRoot, "evaluations");
                Directory.CreateDirectory(webEvalDir);

                // 优先从 processing 的 evaluations 中找最新文件
                var cuts = await _videoCutRepository.Where(vc => vc.VideoId == videoId).ToListAsync();
                if (cuts.Any())
                {
                    var firstSegmentDir = Path.GetDirectoryName(cuts.First().SegmentPath)!;
                    var videoDir = Path.GetDirectoryName(firstSegmentDir)!;
                    var processingEvalDir = Path.Combine(videoDir, "evaluations");

                    if (Directory.Exists(processingEvalDir))
                    {
                        var candidateFileInfo = Directory
                            .EnumerateFiles(processingEvalDir, $"*{videoId}*.*", SearchOption.TopDirectoryOnly)
                            .Select(f => new FileInfo(f))
                            .Where(fi => exts.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                            .OrderByDescending(fi => fi.LastWriteTimeUtc)
                            .FirstOrDefault();

                        if (candidateFileInfo != null)
                        {
                            var fileName = candidateFileInfo.Name;
                            var destPath = Path.Combine(webEvalDir, fileName);

                            var needCopy = !File.Exists(destPath) ||
                                           candidateFileInfo.LastWriteTimeUtc > File.GetLastWriteTimeUtc(destPath);

                            if (needCopy)
                            {
                                File.Copy(candidateFileInfo.FullName, destPath, true);
                            }

                            return "/evaluations/" + fileName.Replace("\\", "/");
                        }
                    }
                }

                // 回退：直接在 wwwroot 下查找
                var searchRoots = new[]
                {
                    Path.Combine(webRoot, "evaluations"),
                    Path.Combine(webRoot, "evaluation"),
                    Path.Combine(webRoot, "reports"),
                    webRoot
                }.Where(Directory.Exists);

                foreach (var dir in searchRoots)
                {
                    var file = Directory.EnumerateFiles(dir, $"*{videoId}*.*", SearchOption.AllDirectories)
                        .Select(f => new FileInfo(f))
                        .Where(fi => exts.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                        .OrderByDescending(fi => fi.LastWriteTimeUtc)
                        .FirstOrDefault();

                    if (file != null)
                    {
                        var relative = file.FullName.Substring(webRoot.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        return "/" + relative.Replace("\\", "/");
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }


        private string StripThinkBlocks(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : ThinkPattern.Replace(text, "").Trim();
        }
    }
}