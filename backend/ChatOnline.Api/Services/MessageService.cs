using Microsoft.EntityFrameworkCore;
using ChatOnline.Api.Data;
using ChatOnline.Api.Models.Dtos;
using ChatOnline.Api.Models.Entities;

namespace ChatOnline.Api.Services;

/// <summary>
/// 消息服务：消息的存储、查询、删除
/// </summary>
public class MessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 保存一条群聊消息，返回组装好的 DTO（给 SignalR 推送用）
    /// </summary>
    public async Task<GroupMessageDto?> SaveGroupMessageAsync(int senderId, int groupId,
        string content, MessageType messageType = MessageType.Text, string? fileUrl = null)
    {
        // 验证发送者是群成员且未被禁言
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == senderId);

        if (member == null || member.IsMuted)
            return null;

        var message = new GroupMessage
        {
            GroupId = groupId,
            SenderId = senderId,
            Content = content,
            MessageType = messageType,
            FileUrl = fileUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.GroupMessages.Add(message);
        await _db.SaveChangesAsync();

        // 组装 DTO 返回（含马甲名，不含真实用户名）
        return new GroupMessageDto
        {
            Id = message.Id,
            GroupId = message.GroupId,
            SenderId = message.SenderId,
            SenderNickname = member.AnonNickname,    // 用马甲名！
            SenderAvatar = member.AnonAvatar,
            Content = message.Content,
            MessageType = message.MessageType.ToString(),
            FileUrl = message.FileUrl,
            CreatedAt = message.CreatedAt
        };
    }

    /// <summary>
    /// 保存一条私聊消息
    /// </summary>
    public async Task<PrivateMessageDto?> SavePrivateMessageAsync(int senderId, int chatId,
        string content, MessageType messageType = MessageType.Text, string? fileUrl = null)
    {
        // 验证这个私聊存在且已接受
        var chat = await _db.PrivateChats
            .FirstOrDefaultAsync(c => c.Id == chatId && c.IsAccepted == true);

        if (chat == null || (chat.User1Id != senderId && chat.User2Id != senderId))
            return null;

        var message = new PrivateMessage
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            MessageType = messageType,
            FileUrl = fileUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.PrivateMessages.Add(message);
        await _db.SaveChangesAsync();

        // 取发送者的真实用户名（私聊里可能需要暴露）
        var sender = await _db.Users.FindAsync(senderId);

        return new PrivateMessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderName = sender?.Username ?? "未知用户",
            Content = message.Content,
            MessageType = message.MessageType.ToString(),
            FileUrl = message.FileUrl,
            CreatedAt = message.CreatedAt
        };
    }

    /// <summary>
    /// 获取群聊历史消息（分页，最新在前）
    /// </summary>
    public async Task<List<GroupMessageDto>> GetGroupMessagesAsync(int groupId, int page = 1, int pageSize = 50)
    {
        var messages = await _db.GroupMessages
            .Where(m => m.GroupId == groupId && !m.IsDeleted)   // 排除已删除的
            .OrderByDescending(m => m.CreatedAt)                 // 最新在前
            .Skip((page - 1) * pageSize)                         // 跳过前 N 页
            .Take(pageSize)                                      // 取这一页
            .Include(m => m.Sender)                              // 加载发送者（退群了也有记录）
            .Select(m => new GroupMessageDto
            {
                Id = m.Id,
                GroupId = m.GroupId,
                SenderId = m.SenderId,
                SenderNickname = m.Sender != null
                    ? "已离开的用户"                               // 退群用户只显示这个
                    : "已离开的用户",
                Content = m.Content,
                MessageType = m.MessageType.ToString(),
                FileUrl = m.FileUrl,
                CreatedAt = m.CreatedAt
            })
            .Reverse()                                            // 前端显示从旧到新
            .ToListAsync();

        // 补充：对未退群的用户，查出其马甲名
        foreach (var msg in messages)
        {
            if (msg.SenderNickname != "已离开的用户") continue;

            var member = await _db.GroupMembers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == msg.SenderId);

            if (member != null)
                msg.SenderNickname = member.AnonNickname;
        }

        return messages;
    }

    /// <summary>
    /// 获取私聊历史消息
    /// </summary>
    public async Task<List<PrivateMessageDto>> GetPrivateMessagesAsync(int chatId, int page = 1, int pageSize = 50)
    {
        return await _db.PrivateMessages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Sender)
            .Select(m => new PrivateMessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                SenderName = m.Sender.Username,
                Content = m.Content,
                MessageType = m.MessageType.ToString(),
                FileUrl = m.FileUrl,
                CreatedAt = m.CreatedAt
            })
            .Reverse()
            .ToListAsync();
    }

    /// <summary>
    /// 用户删除自己的消息（软删除）
    /// </summary>
    public async Task<bool> DeleteMessageAsync(int messageId, int userId)
    {
        var message = await _db.GroupMessages.FindAsync(messageId);

        if (message == null || message.SenderId != userId)   // 只能删自己的消息
            return false;

        message.IsDeleted = true;
        await _db.SaveChangesAsync();

        return true;
    }
}
