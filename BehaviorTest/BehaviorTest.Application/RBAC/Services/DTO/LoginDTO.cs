using System.Text.Json.Serialization;

namespace BehaviorTest.Application.RBAC.Services.DTO;

public class LoginDTO
{
    /// <summary>
    /// 用户名
    /// </summary>
    /// <example>admin</example>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    /// <example>admin</example>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}