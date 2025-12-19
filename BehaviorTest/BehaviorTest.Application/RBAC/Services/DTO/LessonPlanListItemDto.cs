namespace BehaviorTest.Application.RBAC.Services.DTO;

public class LessonPlanListItemDto
{
    public ulong Id { get; set; }

    public string OriginalName { get; set; } = null!;

    public string SourcePath { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}