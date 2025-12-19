using System.ComponentModel.DataAnnotations.Schema;
namespace BehaviorTest.Application.RBAC.Entity;

[Table("Auth")] 
public class AuthEntity : IEntity 
{   
    /// <summary>
    /// 主键 Id
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// 角色归属
    /// </summary>
    [Required]     
    public string Role { get; set; } = string.Empty; 
    
    /// <summary>
    /// 权限范围
    /// </summary>
    [Required]     
    public string Auth { get; set; } = string.Empty;
}