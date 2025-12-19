using BehaviorTest.Application.RBAC.Entity;
using BehaviorTest.Application.RBAC.Services.DTO;
using BehaviorTest.Application.RBAC.Tools;
using BootstrapBlazor.Components;

namespace BehaviorTest.Application.RBAC.Services;

public class UserService : IUserService, IDynamicApiController, ITransient
{
    private readonly IRepository<UserEntity> _userEntityRep;

    public UserService(IRepository<UserEntity> userEntityRep)
    {
        _userEntityRep = userEntityRep;
    }

    /// <summary>
    /// 登录（免授权）
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [AllowAnonymous]
    public async Task<dynamic> Login([FromBody] LoginDTO input)
    {
        System.Console.WriteLine($"=== UserService.Login 被调用 ===");
        System.Console.WriteLine($"Input is null: {input == null}");
        
        // Validate input
        if (input == null)
        {
            System.Console.WriteLine("错误: input 为 null");
            return new { code = 400, message = "请求数据不能为空" };
        }
        
        System.Console.WriteLine($"Account: {input.Account}, Password: {(string.IsNullOrEmpty(input.Password) ? "empty" : "provided")}");
            
        if (string.IsNullOrWhiteSpace(input.Account))
        {
            System.Console.WriteLine("错误: Account 为空");
            return new { code = 400, message = "用户名不能为空" };
        }
            
        if (string.IsNullOrWhiteSpace(input.Password))
        {
            System.Console.WriteLine("错误: Password 为空");
            return new { code = 400, message = "密码不能为空" };
        }
        
        // First check if user exists
        var user = await _userEntityRep.Where(u => u.Name == input.Account)
            .FirstOrDefaultAsync();
        
        if (user == null) 
            return new { code = 404, message = "账号不存在" };

        // Then verify password
        var password = DataEncryption.Sha1Encrypt(input.Password);
        if (user.Password != password)
            return new { code = 401, message = "密码错误" };

        string token = JWTEncryption.Encrypt(new Dictionary<string, object>()
        {
            { "UserId", user.Id.ToString() },
            { "Account", user.Name },
            { "Role", user.Role },
        }, 20);

        return new { code = 200, message = "登录成功", token };
    }


    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<dynamic> GetUserInfo([Required] string userId)
    {
        var user = await _userEntityRep.Where(u => u.Id.ToString() == userId).FirstOrDefaultAsync();
        if (user == null) return new { code = 401, message = "用户不存在" };
        return new { code = 200, message = "获取成功", data = new { user.Id, user.Name, user.Role } };
    }

    /// <summary>
    /// 获取用户菜单
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<dynamic> GetUserMenus([Required] string userId)
    {
        var user = await _userEntityRep.Where(u => u.Id.ToString() == userId).FirstOrDefaultAsync();
        if (user == null) return new { code = 401, message = "用户不存在" };
        //模拟不同角色返回不同菜单
        List<MenuItem> menus = new List<MenuItem>();
        if (user.Role == "Admin1")
        {
            menus = new List<MenuItem>()
            {
                new MenuItem() { Text = "首页", Icon = "fa fa-home", Url = "/", Items = null },
                new MenuItem() { Text = "教学院长页面", Icon = "fa fa-user-shield", Url = "/counter", Items = null },
                new MenuItem() { Text = "教务处页面", Icon = "fa fa-user-tie", Url = "/fetchdata", Items = null },
                new MenuItem() { Text = "教师页面", Icon = "fa fa-chalkboard-teacher", Url = "/uploadvideo", Items = null },
            };
        }
        else if (user.Role == "Admin")
        {
            menus = new List<MenuItem>()
            {
                new MenuItem() { Text = "首页", Icon = "fa fa-home", Url = "/", Items = null },
                new MenuItem() { Text = "教学院长页面", Icon = "fa fa-user-shield", Url = "/counter", Items = null },
                new MenuItem() { Text = "教务处页面", Icon = "fa fa-user-tie", Url = "/fetchdata", Items = null },
                new MenuItem() { Text = "教师页面", Icon = "fa fa-chalkboard-teacher", Url = "/uploadvideo", Items = null },
            };
        }
        else if (user.Role == "User")
        {
            menus = new List<MenuItem>()
            {
                new MenuItem() { Text = "首页", Icon = "fa fa-home", Url = "/", Items = null },
                new MenuItem() { Text = "教学院长页面", Icon = "fa fa-user-shield", Url = "/counter", Items = null },
                new MenuItem() { Text = "教务处页面", Icon = "fa fa-user-tie", Url = "/fetchdata", Items = null },
                new MenuItem() { Text = "教师页面", Icon = "fa fa-chalkboard-teacher", Url = "/uploadvideo", Items = null },
            };
        }

        return new { code = 200, message = "获取成功", data = menus };
    }

    /// <summary>
    ///  登出
    /// </summary>
    /// <returns></returns>
    public async Task<dynamic> Logout()
    {
        return new { code = 200, message = "登出成功" };
    }

    /// <summary>
    /// 是否登录
    /// </summary>
    /// <returns></returns>
    public async Task<bool> IsLogin()
    {
        var user = App.User;
        return user?.Identity != null && user.Identity.IsAuthenticated;
    }
}