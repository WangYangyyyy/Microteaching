using BehaviorTest.Application.RBAC.Services;
using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BehaviorTest.Web.Core;

public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConsoleFormatter();
        services.AddJwt();
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .AddInjectWithUnifyResult();
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpClient(); // Add HttpClient service
        services.AddVirtualFileServer();// 启用虚拟文件服务器
        services.AddBootstrapBlazor(); // 注册BootstrapBlazor及AjaxService
        
        // 注册服务
        services.AddTransient<IAudioService, AudioService>();
        services.AddTransient<ISummaryService, SummaryService>();
        services.AddTransient<IClassroomEvaluationService, ClassroomEvaluationService>();
        services.AddTransient<IQuestionAnswerService, QuestionAnswerService>();
        services.AddTransient<IQaEvaluationService, QaEvaluationService>();
        // 配置文件上传大小限制（500MB）
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 524288000; // 500MB
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseInject();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }
}
