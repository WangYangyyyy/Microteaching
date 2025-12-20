using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IMicroLessonService
{
    Task<List<MicroLesson>> GetMicroLessonsAsync();
    Task<OperationResult> CreateMicroLessonAsync(string title, string? description);
}