namespace ChatOnline.Api.Models.Dtos;

/// <summary>
/// 登录成功后返回给前端的数据
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }        // 是否成功
    public string? Token { get; set; }       // JWT Token，失败时为 null
    public string? Message { get; set; }     // 提示信息
    public UserBrief? User { get; set; }     // 用户简要信息
}

/// <summary>
/// 返回给前端的用户简要信息（不包含密码哈希、注册时间等敏感/冗余字段）
/// </summary>
public class UserBrief
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
}
