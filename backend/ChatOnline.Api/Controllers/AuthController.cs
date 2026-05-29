using Microsoft.AspNetCore.Mvc;
using ChatOnline.Api.Models.Requests;
using ChatOnline.Api.Services;

namespace ChatOnline.Api.Controllers;

/// <summary>
/// 认证控制器：处理注册和登录请求
/// </summary>
[ApiController]                              // 自动校验 Request，绑定参数
[Route("api/auth")]                          // 路由前缀，所有方法路径都以 /api/auth 开头
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    /// <summary>
    /// 构造函数注入 AuthService
    /// </summary>
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /api/auth/register
    /// [FromBody] 告诉框架：从 HTTP 请求的 JSON body 中解析参数
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 如果 [Required] 校验失败，框架自动返回 400，不会进到这里
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);       // HTTP 400

        return Ok(result);                   // HTTP 200
    }

    /// <summary>
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Success)
            return Unauthorized(result);     // HTTP 401

        return Ok(result);                   // HTTP 200
    }
}
