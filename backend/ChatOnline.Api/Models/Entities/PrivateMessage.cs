namespace ChatOnline.Api.Models.Entities;

/// <summary>
/// 私聊消息表，记录私聊中的每一条消息
/// </summary>
public class PrivateMessage
{
    public int Id { get; set; }

    // ---- 外键列 ----
    public int ChatId { get; set; }
    public int SenderId { get; set; }

    // ---- 消息内容 ----
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性 =====

    // 这条消息属于哪个私聊会话
    public PrivateChat Chat { get; set; } = null!;

    // 这条消息是谁发的
    public User Sender { get; set; } = null!;
}
