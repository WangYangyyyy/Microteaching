using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BootstrapBlazor.Components;
using Furion.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BehaviorTest.Application.RBAC.Services.DTO;

namespace BehaviorTest.Web.Entry.Pages.Account;

public partial class Login
{
    private string Title { get; set; } = "智能教室管理系统";

    private bool IsLoading { get; set; }

    [SupplyParameterFromQuery]
    [Parameter]
    public string ReturnUrl { get; set; }

    private LoginDTO LoginDTO { get; set; } = new LoginDTO();

    [Inject]
    [NotNull]
    private AjaxService AjaxService { get; set; }

    [Inject]
    [NotNull]
    public MessageService MessageService { get; set; }

    [Inject]
    [NotNull]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    [NotNull]
    private NavigationManager NavigationManager { get; set; }

    private Task OnSignUp()
    {
        //注册用户
        throw new NotImplementedException();
    }

    private Task OnForgotPassword()
    {
        //忘记密码
        throw new NotImplementedException();
    }

    private async Task DoLogin()
    {
        if (string.IsNullOrEmpty(LoginDTO.Account))
        {
            await MessageService.Show(new MessageOption()
            {
                Color = Color.Danger,
                Content = "用户名不能为空"
            });
            return;
        }

        if (LoginDTO.Account.Length < 2)
        {
            await MessageService.Show(new MessageOption()
            {
                Color = Color.Danger,
                Content = "用户名至少需要2个字符"
            });
            return;
        }

        if (string.IsNullOrEmpty(LoginDTO.Password))
        {
            await MessageService.Show(new MessageOption()
            {
                Color = Color.Danger,
                Content = "密码不能为空"
            });
            return;
        }

        if (LoginDTO.Password.Length < 5)
        {
            await MessageService.Show(new MessageOption()
            {
                Color = Color.Danger,
                Content = "密码至少需要5个字符"
            });
            return;
        }

        IsLoading = true;
        StateHasChanged();

        try
        {
            // Log the data being sent
            System.Console.WriteLine($"Attempting login with Account: {LoginDTO.Account}, Password length: {LoginDTO.Password?.Length}");
            
            var ajaxOption = new AjaxOption
            {
                Url = "/api/user/login",
                Data = LoginDTO
            };

            var str = await AjaxService.InvokeAsync(ajaxOption);
            if (str == null)
            {
                await MessageService.Show(new MessageOption()
                {
                    Color = Color.Danger,
                    Content = "登录失败"
                });
            }
            else
            {
                JsonElement jsonEl = str.RootElement;
                string jsonString = jsonEl.ToString();
                System.Console.WriteLine($"API Response JSON: {jsonString}");
                
                // Try to parse the JSON
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;
                
                // Check if response has a 'code' property directly
                int code = 0;
                string token = string.Empty;
                string message = "登录失败";
                
                if (root.TryGetProperty("code", out JsonElement codeElement))
                {
                    code = codeElement.GetInt32();
                    
                    if (root.TryGetProperty("token", out JsonElement tokenElement))
                    {
                        token = tokenElement.GetString() ?? string.Empty;
                    }
                    
                    if (root.TryGetProperty("message", out JsonElement messageElement))
                    {
                        message = messageElement.GetString() ?? "登录失败";
                    }
                }
                // Check if response is wrapped (e.g., has a 'data' property)
                else if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    if (dataElement.TryGetProperty("code", out codeElement))
                    {
                        code = codeElement.GetInt32();
                    }
                    
                    if (dataElement.TryGetProperty("token", out JsonElement tokenElement))
                    {
                        token = tokenElement.GetString() ?? string.Empty;
                    }
                    
                    if (dataElement.TryGetProperty("message", out JsonElement messageElement))
                    {
                        message = messageElement.GetString() ?? "登录失败";
                    }
                }
                else
                {
                    // If neither structure is found, log the structure
                    System.Console.WriteLine($"Unexpected JSON structure. Root properties: {string.Join(", ", root.EnumerateObject().Select(p => p.Name))}");
                }

                if (code == 200 && !string.IsNullOrEmpty(token))
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.setItem", "token", token);
                    ReturnUrl ??= "/";
                    Log.Information($"登录成功，跳转到：{ReturnUrl}");
                    
                    await MessageService.Show(new MessageOption()
                    {
                        Color = Color.Success,
                        Content = "登录成功"
                    });
                    
                    // Navigate without forceLoad - App.razor will detect the token via LocationChanged event
                    NavigationManager.NavigateTo(ReturnUrl);
                }
                else
                {
                    await MessageService.Show(new MessageOption()
                    {
                        Color = Color.Danger,
                        Content = message
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error during login: {ex.GetType().Name}");
            System.Console.WriteLine($"Error message: {ex.Message}");
            System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            string errorMessage = "登录失败";
            
            // Check for specific error types
            if (ex.Message.Contains("400") || ex.Message.Contains("Bad Request"))
            {
                errorMessage = "请求格式错误，请确认输入的用户名和密码格式正确";
            }
            else if (ex.Message.Contains("404"))
            {
                errorMessage = "登录服务不可用";
            }
            else if (ex.Message.Contains("500"))
            {
                errorMessage = "服务器错误，请稍后重试";
            }
            
            await MessageService.Show(new MessageOption()
            {
                Color = Color.Danger,
                Content = errorMessage
            });
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }
}
