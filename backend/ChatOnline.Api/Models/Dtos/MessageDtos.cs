namespace ChatOnline.Api.Models.Dtos;

/// <summary>
/// 群聊消息（通过 SignalR 推送给前端）
/// </summary>
public class GroupMessageDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }   // 群解散后为 null
    public int SenderId { get; set; }
    public string SenderNickname { get; set; } = string.Empty;  // 匿名马甲名（不是真实用户名！）
    public string? SenderAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";  // "Text" / "Image" / "File"
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 私聊消息
/// </summary>
public class PrivateMessageDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;  // 真实用户名（私聊里可能暴露）
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
