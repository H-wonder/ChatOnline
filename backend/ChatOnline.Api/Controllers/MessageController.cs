using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatOnline.Api.Services;

namespace ChatOnline.Api.Controllers;

[ApiController]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;

    public MessageController(MessageService messageService)
    {
        _messageService = messageService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    /// <summary>
    /// GET /api/groups/{groupId}/messages?page=1&pageSize=50
    /// 获取群聊历史消息
    /// </summary>
    [HttpGet("api/groups/{groupId}/messages")]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageService.GetGroupMessagesAsync(groupId, page, pageSize);
        return Ok(messages);
    }

    /// <summary>
    /// DELETE /api/messages/{id}
    /// 用户删除自己的消息
    /// </summary>
    [HttpDelete("api/messages/{id}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        int userId = GetCurrentUserId();
        bool success = await _messageService.DeleteMessageAsync(id, userId);

        if (!success)
            return BadRequest(new { message = "只能删除自己的消息" });

        return Ok(new { message = "已删除" });
    }

    /// <summary>
    /// GET /api/private-chats/{chatId}/messages?page=1&pageSize=50
    /// 获取私聊历史消息
    /// </summary>
    [HttpGet("api/private-chats/{chatId}/messages")]
    public async Task<IActionResult> GetPrivateMessages(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _messageService.GetPrivateMessagesAsync(chatId, page, pageSize);
        return Ok(messages);
    }
}
