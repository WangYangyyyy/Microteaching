using Microsoft.AspNetCore.Http;

namespace BehaviorTest.Application.RBAC.Services.DTO;

public class VideoUploadDto
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string? Notes { get; set; }
}
