using BehaviorTest.Application.RBAC.Services.DemoService.DTO;

namespace BehaviorTest.Application.RBAC.Services.DemoService;

public interface IDemoService
{
    /// <summary>
    /// 对指定视频执行完整处理链路（同步顺序执行）
    /// </summary>
    Task<DemoPipelineResultDto> RunFullPipelineAsync(ulong videoId);

    /// <summary>
    /// 查询指定视频的处理进度，用于进度条显示
    /// </summary>
    Task<PipelineProgressDto> GetPipelineProgressAsync(ulong videoId);
}