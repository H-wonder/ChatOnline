using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatOnline.Api.Models.Requests;
using ChatOnline.Api.Services;

namespace ChatOnline.Api.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupController : ControllerBase
{
    private readonly GroupService _groupService;

    public GroupController(GroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>
    /// 从 JWT Token 中提取当前用户的 Id
    /// </summary>
    private int GetCurrentUserId()
    {
        // JWT 的 Claim 里存了 userId，这里取出来
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim!.Value);
    }

    /// <summary>
    /// GET /api/groups?search=xxx
    /// [AllowAnonymous] 表示不用登录也能访问（群聊大厅是公开的）
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetGroups([FromQuery] string? search)
    {
        var groups = await _groupService.GetPublicGroupsAsync(search);
        return Ok(groups);
    }

    /// <summary>
    /// GET /api/groups/mine  获取我加入的群（侧边栏用）
    /// </summary>
    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyGroups()
    {
        int userId = GetCurrentUserId();
        var groups = await _groupService.GetMyGroupsAsync(userId);
        return Ok(groups);
    }

    /// <summary>
    /// POST /api/groups  创建群聊（需登录）
    /// </summary>
    [Authorize]                     // 必须登录才能创建群
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        int userId = GetCurrentUserId();
        var result = await _groupService.CreateGroupAsync(userId, request);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/groups/{id}  群聊详情
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroupDetail(int id)
    {
        var detail = await _groupService.GetGroupDetailAsync(id);
        if (detail == null)
            return NotFound(new { message = "群不存在" });

        return Ok(detail);
    }

    /// <summary>
    /// POST /api/groups/{id}/join  加入群聊
    /// </summary>
    [Authorize]
    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinGroup(int id, [FromBody] JoinGroupRequest request)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _groupService.JoinGroupAsync(userId, id, request);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// POST /api/groups/{id}/leave  退出群聊
    /// </summary>
    [Authorize]
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _groupService.LeaveGroupAsync(userId, id);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// GET /api/groups/{id}/members  群成员列表
    /// </summary>
    [Authorize]
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var detail = await _groupService.GetGroupDetailAsync(id);
        if (detail == null) return NotFound();

        return Ok(detail.Members);
    }

    /// <summary>
    /// PUT /api/groups/{id}/members/{userId}/mute  禁言/解禁
    /// </summary>
    [Authorize]
    [HttpPut("{id}/members/{userId}/mute")]
    public async Task<IActionResult> ToggleMute(int id, int userId, [FromBody] bool mute)
    {
        int operatorId = GetCurrentUserId();
        var (success, message) = await _groupService.ToggleMuteAsync(operatorId, id, userId, mute);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// PUT /api/groups/{id}/members/{userId}/role  设置管理员
    /// </summary>
    [Authorize]
    [HttpPut("{id}/members/{userId}/role")]
    public async Task<IActionResult> SetAdmin(int id, int userId, [FromBody] bool isAdmin)
    {
        int operatorId = GetCurrentUserId();
        var (success, message) = await _groupService.SetAdminAsync(operatorId, id, userId, isAdmin);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// DELETE /api/groups/{id}/members/{userId}  踢出成员
    /// </summary>
    [Authorize]
    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> KickMember(int id, int userId)
    {
        int operatorId = GetCurrentUserId();
        var (success, message) = await _groupService.KickMemberAsync(operatorId, id, userId);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// PUT /api/groups/{id}  修改群信息
    /// </summary>
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest request)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _groupService.UpdateGroupAsync(
            userId, id, request.Name, request.Description, request.Password, request.Question, request.QuestionAnswer);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }

    /// <summary>
    /// DELETE /api/groups/{id}  解散群聊
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DissolveGroup(int id)
    {
        int userId = GetCurrentUserId();
        var (success, message) = await _groupService.DissolveGroupAsync(userId, id);

        if (!success) return BadRequest(new { message });
        return Ok(new { message });
    }
}
