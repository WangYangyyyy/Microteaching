using BehaviorTest.EntityFramework.Core;
using Microsoft.AspNetCore.SignalR;

// 确保引用 Furion 命名空间

var builder = WebApplication.CreateBuilder(args).Inject(); // 👈 1. 核心修改：使用 Inject() 集成 Furion

// ==========================================
// 👇 2. 在这里配置 SignalR 限制 (必须在 Build 之前)
// ==========================================
builder.Services.Configure<HubOptions>(options =>
{
    // 设置为 64MB，足够传输几十秒的音频
    options.MaximumReceiveMessageSize = 64 * 1024 * 1024; 
    options.EnableDetailedErrors = true;
});

// 添加其他服务
builder.Services.AddHttpClient();
builder.Services.AddBootstrapBlazor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DefaultDbContext>();

// ==========================================
// 👆 配置结束
// ==========================================

var app = builder.Build();

// 配置中间件
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseInject(); // Furion 中间件

app.MapControllers();
app.MapBlazorHub(); // 确保映射了 Blazor Hub
app.MapFallbackToPage("/_Host"); // 或者是你的 Blazor 入口

app.Run(); // 👈 3. 最后启动应用