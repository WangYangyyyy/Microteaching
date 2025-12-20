using BehaviorTest.Application.RBAC.Services.DTO;
using Microsoft.AspNetCore.Components.Forms;

namespace BehaviorTest.Application.RBAC.Services;

public interface IDesignEvaluationService
{
    /// <summary>
    /// 上传导学案文件到wwwroot/teachingplan目录
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <returns>文件保存路径</returns>
    Task<string> UploadTeachingPlanAsync(IBrowserFile browserFile, ulong microLessonId);

    /// <summary>
    /// 读取Word文档内容，发送到OpenAI接口进行打分
    /// </summary>
    /// <param name="filePath">Word文件路径</param>
    /// <returns>打分结果</returns>
    Task<EvaluationResultDto> EvaluateDesignAsync(string filePath);
    
    Task<List<LessonPlanListItemDto>> GetLessonPlansAsync();
    
    /// <summary>
    /// 下载评估报告
    /// </summary>
    /// <param name="lessonPlanId"></param>
    /// <returns></returns>
    Task<string?> GetEvaluationReportPathAsync(ulong lessonPlanId);

}