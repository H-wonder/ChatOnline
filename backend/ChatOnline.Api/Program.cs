using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChatOnline.Api.Data;
using ChatOnline.Api.Hubs;
using ChatOnline.Api.Services;

var builder = WebApplication.CreateBuilder(args);  // 创建应用构建器，后面所有服务都往这里注册

// ============================================================
// 一、注册 EF Core + MySQL
//效果： 之后任何 Service 需要数据库，框架自动给一个 AppDbContext，它知道去哪连 MySQL。
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    // 从 appsettings.json 读取连接字符串
    var connStr = builder.Configuration.GetConnectionString("Default");
    // 告诉 EF Core 使用 MySQL（Pomelo 驱动）
    options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
});

// ============================================================
// 二、注册 JWT 认证
// ============================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 从 appsettings.json 的 Jwt 段读取配置
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

        // Token 验证参数
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // 验证签发者
            ValidateAudience = true,         // 验证接收方
            ValidateLifetime = true,         // 验证过期时间
            ValidateIssuerSigningKey = true, // 验证签名密钥
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        // SignalR 通过 query string 传 token，也需要处理
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 从 URL 参数 access_token 中提取 JWT
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                // 只有 SignalR 的 /hubs 路径才从 query string 取 token
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ============================================================
// 三、注册 SignalR（实时通信）
//效果： 告诉框架"我要用 WebSocket 实时通信"。ChatHub 需要这个才能工作。
// ============================================================
builder.Services.AddSignalR();

// ============================================================
// 四、注册自定义服务
// ============================================================
// AddScoped：每个 HTTP 请求创建一个新实例，请求结束自动销毁
// Singleton：整个应用生命周期只有一个实例（全局共享）
builder.Services.AddSingleton<ConnectionMapping>();

// Scoped：每个 HTTP 请求一个实例
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<UserService>();

// ============================================================
// 五、注册控制器（Web API）
// ============================================================
builder.Services.AddControllers();

// ============================================================
// 五、注册 CORS（允许前端跨域访问）
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueApp", policy =>
    {
        policy.AllowAnyOrigin()          // 允许任意来源（方案二：后端托管前端，同源访问）
              .AllowAnyHeader()
              .AllowAnyMethod();
        // 注意：AllowAnyOrigin 与 AllowCredentials 不能同时使用
        // 方案二中前端和后端同源，SignalR 不需要 AllowCredentials
    });
});

// ============================================================
// 构建应用
// ============================================================
var app = builder.Build();

// ============================================================
// 六、配置中间件管道（请求处理流水线）
// ============================================================

app.UseCors("VueApp");              // 启用跨域
app.UseStaticFiles();               // 提供静态文件（uploads/ 目录 + wwwroot/ 前端）
app.UseAuthentication();            // 启用 JWT 认证
app.UseAuthorization();             // 启用授权

app.MapControllers();               // 映射控制器路由（/api/xxx → Controller 方法）

app.MapHub<ChatHub>("/hubs/chat");   // SignalR WebSocket 端点

// SPA fallback：所有非 API/Hub 请求都返回 index.html（Vue Router history 模式需要）
app.MapFallbackToFile("index.html");

app.Run();
