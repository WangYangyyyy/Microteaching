using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Application.RBAC.Services;

public interface IUserService
{
    Task<dynamic> Login(LoginDTO input);
    
    Task<dynamic> GetUserInfo([Required] string userId);
    
    Task<dynamic> GetUserMenus([Required] string userId);
    
    Task<dynamic> Logout();
    
    //是否登录
    Task<bool> IsLogin();
}