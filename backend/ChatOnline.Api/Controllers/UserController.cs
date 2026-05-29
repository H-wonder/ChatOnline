using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatOnline.Api.Services;

namespace ChatOnline.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    /// <summary>
    /// GET /api/users/me  获取当前用户的个人资料
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        int userId = GetCurrentUserId();
        var profile = await _userService.GetProfileAsync(userId);

        if (profile == null)
            return NotFound(new { message = "用户不存在" });

        return Ok(profile);
    }

    /// <summary>
    /// PUT /api/users/me  修改个人资料
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _userService.UpdateProfileAsync(
            userId, request.Avatar, request.Bio);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    /// <summary>
    /// PUT /api/users/me/groups/{groupId}/anon  修改自己在群里的马甲
    /// </summary>
    [HttpPut("me/groups/{groupId}/anon")]
    public async Task<IActionResult> UpdateAnonNickname(int groupId, [FromBody] UpdateAnonRequest request)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _userService.UpdateAnonNicknameAsync(
            userId, groupId, request.Nickname);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }
}

// 请求参数类（简单到可以写在 Controller 文件里）
public class UpdateProfileRequest
{
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
}

public class UpdateAnonRequest
{
    public string Nickname { get; set; } = string.Empty;
}
