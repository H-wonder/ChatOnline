using System.ComponentModel.DataAnnotations;

namespace ChatOnline.Api.Models.Requests;

/// <summary>
/// 前端传来的注册请求
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// [Required] 是数据注解，ASP.NET Core 会自动校验，
    /// 如果 Username 为空，接口直接返回 400，不需要手动 if 判断
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [MinLength(2, ErrorMessage = "用户名至少 2 个字符")]
    [MaxLength(50, ErrorMessage = "用户名最多 50 个字符")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码至少 6 个字符")]
    [MaxLength(100, ErrorMessage = "密码最多 100 个字符")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 前端传来的登录请求
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}
