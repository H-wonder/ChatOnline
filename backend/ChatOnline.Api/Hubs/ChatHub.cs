using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ChatOnline.Api.Data;
using ChatOnline.Api.Models.Dtos;
using ChatOnline.Api.Models.Entities;
using ChatOnline.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatOnline.Api.Hubs;

/// <summary>
/// SignalR Hub：所有实时通信的入口
/// 每个客户端方法（Invoke）对应一个服务端处理逻辑
/// 每个服务端事件（SendAsync）对应一条推送到客户端的消息
/// </summary>
[Authorize]   // 连接 SignalR 也需要 JWT Token
public class ChatHub : Hub
{
    private readonly ConnectionMapping _connections;
    private readonly MessageService _messageService;
    private readonly GroupService _groupService;
    private readonly AppDbContext _db;

    public ChatHub(ConnectionMapping connections, MessageService messageService,
        GroupService groupService, AppDbContext db)
    {
        _connections = connections;
        _messageService = messageService;
        _groupService = groupService;
        _db = db;
    }

    /// <summary>
    /// 从 SignalR 连接的 Context 中提取当前用户 Id
    /// </summary>
    private int GetUserId()
    {
        return int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    // ==================== 连接生命周期 ====================

    /// <summary>
    /// 客户端连接建立时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        int userId = GetUserId();
        _connections.Add(userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接时触发（关闭页面、网络断线等）
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = GetUserId();
        _connections.Remove(userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ==================== 群聊消息 ====================

    /// <summary>
    /// 用户加入群聊的 SignalR Group（消息房间）
    /// 前端进入聊天页面时调用
    /// </summary>
    public async Task JoinGroup(int groupId)
    {
        string groupName = $"group_{groupId}";    // SignalR Group 名称

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // 通知群内其他人
        int userId = GetUserId();
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        await Clients.Group(groupName).SendAsync("UserJoinedGroup",
            userId, member?.AnonNickname ?? "新成员");
    }

    /// <summary>
    /// 用户离开群聊的 SignalR Group
    /// </summary>
    public async Task LeaveGroup(int groupId)
    {
        string groupName = $"group_{groupId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        int userId = GetUserId();
        await Clients.Group(groupName).SendAsync("UserLeftGroup", userId);
    }

    /// <summary>
    /// 发送群聊消息
    /// </summary>
    public async Task SendGroupMessage(int groupId, string content,
        int messageType = 0, string? fileUrl = null)
    {
        int userId = GetUserId();

        // 保存消息到数据库（内部会校验成员资格和禁言状态）
        var msgDto = await _messageService.SaveGroupMessageAsync(
            userId, groupId, content, (MessageType)messageType, fileUrl);

        if (msgDto == null)
        {
            await Clients.Caller.SendAsync("Error", "发送失败：你不在该群或已被禁言");
            return;
        }

        // 广播给群内所有在线成员（包括发送者自己，前端据此确认发送成功）
        string groupName = $"group_{groupId}";
        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", msgDto);
    }

    // ==================== 私聊 ====================

    /// <summary>
    /// 用户 A 发起私聊请求给用户 B
    /// </summary>
    public async Task RequestPrivateChat(int targetUserId)
    {
        int fromUserId = GetUserId();

        if (fromUserId == targetUserId)
        {
            await Clients.Caller.SendAsync("Error", "不能和自己私聊");
            return;
        }

        // 检查是否已有私聊记录（同一对人只允许一条）
        var existingChat = await _db.PrivateChats
            .FirstOrDefaultAsync(c =>
                (c.User1Id == fromUserId && c.User2Id == targetUserId) ||
                (c.User1Id == targetUserId && c.User2Id == fromUserId));

        if (existingChat != null)
        {
            if (existingChat.IsAccepted == true)
            {
                await Clients.Caller.SendAsync("Error", "你们已在私聊中");
                return;
            }
            if (existingChat.IsAccepted == null)
            {
                await Clients.Caller.SendAsync("Error", "你已发过请求，请等待对方回复");
                return;
            }
            // 之前被拒绝过，重置请求
            existingChat.IsAccepted = null;
            existingChat.User1Id = fromUserId;
            existingChat.User2Id = targetUserId;
            await _db.SaveChangesAsync();
        }
        else
        {
            // 创建新的私聊记录
            var chat = new PrivateChat
            {
                User1Id = fromUserId,
                User2Id = targetUserId,
                IsAccepted = null,          // 待处理
                CreatedAt = DateTime.UtcNow
            };
            _db.PrivateChats.Add(chat);
            await _db.SaveChangesAsync();
        }

        // 获取发起方的匿名马甲名
        var fromUser = await _db.Users.FindAsync(fromUserId);

        // 查出刚创建/更新的 chat 记录拿到 chatId
        var chatRecord = await _db.PrivateChats
            .FirstOrDefaultAsync(c =>
                (c.User1Id == fromUserId && c.User2Id == targetUserId) ||
                (c.User1Id == targetUserId && c.User2Id == fromUserId));

        // 通过 ConnectionMapping 找到目标用户所有连接，推送请求
        var targetConnections = _connections.GetConnections(targetUserId).ToList();
        Console.WriteLine($"[私聊请求] from={fromUserId} to={targetUserId} chatId={chatRecord?.Id} 目标连接数={targetConnections.Count}");
        foreach (var connId in targetConnections)
        {
            await Clients.Client(connId).SendAsync("ReceivePrivateChatRequest",
                new
                {
                    chatId = chatRecord?.Id ?? 0,
                    fromUserId,
                    fromUsername = fromUser?.Username ?? "未知用户",
                    message = "向你发起了私聊请求"
                });
        }

        await Clients.Caller.SendAsync("PrivateChatRequestSent",
            new { message = "私聊请求已发送" });
    }

    /// <summary>
    /// 用户 B 响应用户 A 的私聊请求
    /// </summary>
    public async Task RespondPrivateChat(int chatId, bool accepted)
    {
        int userId = GetUserId();

        var chat = await _db.PrivateChats.FindAsync(chatId);
        if (chat == null || chat.User2Id != userId)
        {
            await Clients.Caller.SendAsync("Error", "无权操作");
            return;
        }

        chat.IsAccepted = accepted;
        await _db.SaveChangesAsync();

        // 通知发起方结果
        string eventName = accepted ? "PrivateChatAccepted" : "PrivateChatRejected";
        foreach (var connId in _connections.GetConnections(chat.User1Id))
        {
            await Clients.Client(connId).SendAsync(eventName, new { chatId });
        }

        await Clients.Caller.SendAsync(accepted ? "PrivateChatStarted" : "PrivateChatClosed",
            new { chatId });
    }

    /// <summary>
    /// 发送私聊消息
    /// </summary>
    public async Task SendPrivateMessage(int chatId, string content,
        int messageType = 0, string? fileUrl = null)
    {
        int userId = GetUserId();

        var msgDto = await _messageService.SavePrivateMessageAsync(
            userId, chatId, content, (MessageType)messageType, fileUrl);

        if (msgDto == null)
        {
            await Clients.Caller.SendAsync("Error", "发送失败");
            return;
        }

        // 推送给私聊双方
        var chat = await _db.PrivateChats.FindAsync(chatId);
        if (chat != null)
        {
            foreach (var connId in _connections.GetConnections(chat.User1Id))
                await Clients.Client(connId).SendAsync("ReceivePrivateMessage", msgDto);
            foreach (var connId in _connections.GetConnections(chat.User2Id))
                await Clients.Client(connId).SendAsync("ReceivePrivateMessage", msgDto);
        }
    }

    /// <summary>
    /// 向对方暴露自己的真实身份
    /// </summary>
    public async Task RevealIdentity(int chatId)
    {
        int userId = GetUserId();

        var chat = await _db.PrivateChats.FindAsync(chatId);
        if (chat == null) return;

        var user = await _db.Users.FindAsync(userId);

        if (chat.User1Id == userId)
        {
            chat.User1Revealed = true;
            foreach (var connId in _connections.GetConnections(chat.User2Id))
                await Clients.Client(connId).SendAsync("IdentityRevealed",
                    new { chatId, userId, realName = user?.Username, realAvatar = user?.Avatar });
        }
        else if (chat.User2Id == userId)
        {
            chat.User2Revealed = true;
            foreach (var connId in _connections.GetConnections(chat.User1Id))
                await Clients.Client(connId).SendAsync("IdentityRevealed",
                    new { chatId, userId, realName = user?.Username, realAvatar = user?.Avatar });
        }

        await _db.SaveChangesAsync();
        await Clients.Caller.SendAsync("IdentityRevealConfirmed", new { chatId });
    }

    // ==================== 群管理事件通知 ====================

    /// <summary>
    /// 通知群内：有成员被踢出
    /// </summary>
    public async Task NotifyMemberKicked(int groupId, int targetUserId)
    {
        string groupName = $"group_{groupId}";
        await Clients.Group(groupName).SendAsync("MemberKicked", targetUserId);

        // 如果被踢者在群内，踢出 SignalR Group
        foreach (var connId in _connections.GetConnections(targetUserId))
        {
            await Groups.RemoveFromGroupAsync(connId, groupName);
        }
    }

    /// <summary>
    /// 通知群内：有成员被禁言/解禁
    /// </summary>
    public async Task NotifyMemberMuted(int groupId, int targetUserId, bool isMuted)
    {
        string groupName = $"group_{groupId}";
        await Clients.Group(groupName).SendAsync("MemberMuted", targetUserId, isMuted);
    }

    /// <summary>
    /// 通知群内：群聊被解散
    /// </summary>
    public async Task NotifyGroupDissolved(int groupId)
    {
        string groupName = $"group_{groupId}";
        await Clients.Group(groupName).SendAsync("GroupDissolved", groupId);
    }
}
