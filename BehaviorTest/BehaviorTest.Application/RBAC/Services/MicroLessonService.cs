using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public class MicroLessonService : IMicroLessonService, IDynamicApiController, ITransient
{
    private readonly IRepository<MicroLesson> _microLessonRepository;

    public MicroLessonService(IRepository<MicroLesson> microLessonRepository)
    {
        _microLessonRepository = microLessonRepository;
    }

    public async Task<List<MicroLesson>> GetMicroLessonsAsync()
    {
        // 这里默认获取所有，实际场景可能需要根据 TeacherId (当前用户) 过滤
        return await _microLessonRepository.Entities
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<OperationResult> CreateMicroLessonAsync(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            return OperationResult.FailureResult("课程标题不能为空");

        try
        {
            var lesson = new MicroLesson
            {
                TeacherId = 1, // 暂时硬编码为1，后续对接真实用户系统
                Title = title,
                Description = description,
                CreatedAt = DateTime.Now
            };

            await _microLessonRepository.InsertNowAsync(lesson);

            // 返回 ID 方便前端选中
            return OperationResult.SuccessResult("创建成功", lesson.Id); 
        }
        catch (Exception ex)
        {
            return OperationResult.FailureResult($"创建课程失败：{ex.Message}");
        }
    }
}