#nullable enable

using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services.DemoService
{
    /// <summary>
    /// 专门为 Demo 页面准备的“建议型”课堂评价服务：
    /// - 不再输出分数，而是输出改进建议报告
    /// </summary>
    public interface IDemoEvaluationService
    {
        /// <summary>
        /// 重置对话历史，开始一次新的建议生成流程
        /// </summary>
        void ResetConversation();

        /// <summary>
        /// 对一段完整的课堂转录文本生成建议（四个维度的综合建议）
        /// </summary>
        /// <param name="transcriptContent">课堂转录/摘要合并后的文本</param>
        /// <returns>Markdown 格式的建议报告</returns>
        Task<string?> SuggestAsync(string transcriptContent);

        /// <summary>
        /// 根据 VideoId 汇总 summaries -> 调用大模型生成建议 -> 保存到 processing/&lt;video&gt;/evaluations 目录
        /// </summary>
        Task<OperationResult> EvaluateAndSaveSuggestionsForVideoAsync(ulong videoId);

        /// <summary>
        /// 获取指定视频最新的一份“建议报告”访问路径（返回 wwwroot 下的相对 URL）
        /// </summary>
        Task<string?> GetSuggestionReportForVideoAsync(ulong videoId);
    }
}