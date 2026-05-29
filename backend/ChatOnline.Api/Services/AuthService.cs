using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ChatOnline.Api.Data;
using ChatOnline.Api.Models.Dtos;
using ChatOnline.Api.Models.Entities;
using ChatOnline.Api.Models.Requests;

namespace ChatOnline.Api.Services;

/// <summary>
/// 认证服务：处理注册、登录、JWT 生成
/// </summary>
public class AuthService
{
    // 通过构造函数注入的依赖（ASP.NET Core 自动提供）
    private readonly AppDbContext _db;             // 数据库操作（readonly只读）
    private readonly IConfiguration _config;       // 读取 appsettings.json 配置

    /// <summary>
    /// 构造函数：参数由 ASP.NET Core 依赖注入框架自动传入
    /// 不需要手动 new，在 Program.cs 注册后框架会自动创建
    /// </summary>
    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 注册新用户
    /// </summary>
    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. 检查用户名是否已存在
        bool exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "用户名已存在"
            };
        }

        // 2. 用 BCrypt 对密码做哈希 —— 绝不存明文密码！
        //    BCrypt 自动加盐（salt），每次哈希结果都不同
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. 创建 User 实体并保存到数据库
        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 4. 注册成功，生成 JWT 返回
        return GenerateTokenResponse(user, "注册成功");
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 1. 从数据库查用户
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "用户名或密码错误"
            };
        }

        // 2. 用 BCrypt 验证密码
        bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "用户名或密码错误"
            };
        }

        // 3. 登录成功，生成 JWT
        return GenerateTokenResponse(user, "登录成功");
    }

    /// <summary>
    /// 生成 JWT Token 并组装响应
    /// </summary>
    private LoginResponse GenerateTokenResponse(User user, string message)
    {
        // 从 appsettings.json 读取 JWT 配置
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // JWT 负载（Payload）：声明这个 Token 代表谁
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // 用户 ID
            new Claim(ClaimTypes.Name, user.Username)                  // 用户名
        };

        // 组装 Token
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],//签发人
            audience: jwtSection["Audience"],//受众
            claims: claims,//实际信息
            expires: DateTime.UtcNow.AddDays(double.Parse(jwtSection["ExpireDays"]!)),//过期时间
            signingCredentials: credentials//签名方式
        );

        // 序列化为字符串
        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse
        {
            Success = true,
            Message = message,
            Token = tokenString,
            User = new UserBrief
            {
                Id = user.Id,
                Username = user.Username,
                Avatar = user.Avatar,
                Bio = user.Bio
            }
        };
    }
}
