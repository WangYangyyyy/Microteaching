using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;


namespace BehaviorTest.Application.RBAC.Entity;

/// <summary>
/// 用户表
/// </summary>
[Table("user")]
public class UserEntity: IEntity
{
    [DisplayName("主键")]
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    [DisplayName("邮件")]
    [Required(ErrorMessage = "邮件地址不能为空")]
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    [DisplayName("日期")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    
    public ICollection<Video> Videos { get; set; } = new List<Video>();
}