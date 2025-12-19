namespace BehaviorTest.Application.RBAC.Services.DTO;

public class VideoCutDto
{
    public string VideoFilePath { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string OutputPath { get; set; }
}