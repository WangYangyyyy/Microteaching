using System;
using System.Linq;
using System.Threading.Tasks;
using BehaviorTest.Application.RBAC.Entity;
using Furion;
using Furion.Authorization;
using Furion.DatabaseAccessor;
using Furion.LinqBuilder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BehaviorTest.Web.Core.Handlers;

public class JwtHandler : AppAuthorizeHandler
{
    private readonly IServiceProvider _serviceProvider;
    public JwtHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Jwt 中间件
    /// </summary>
    /// <param name="context"></param>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public override Task<bool> PipelineAsync(AuthorizationHandlerContext context, DefaultHttpContext httpContext)
    {
        var jwtRole = App.User.FindFirst("Role")?.Value;
        if (jwtRole == null) return Task.FromResult(false);
        var routeData = httpContext.GetRouteData();
        var requestPoint = routeData?.Values["controller"]?.ToString();
        if (string.IsNullOrEmpty(requestPoint)) return Task.FromResult(false);
        using var scope = _serviceProvider.CreateScope();
        var authRepo = scope.ServiceProvider.GetRequiredService<IRepository<AuthEntity>>().Where(a => a.Role == jwtRole).FirstOrDefault();
        if (authRepo == null) return Task.FromResult(false);
        var roleAuth = authRepo.Auth.Split(',').ToList();
        if (!roleAuth.IsNullOrEmpty() && roleAuth.Contains(requestPoint)) { return Task.FromResult(true); }
        return Task.FromResult(false);
    }
}