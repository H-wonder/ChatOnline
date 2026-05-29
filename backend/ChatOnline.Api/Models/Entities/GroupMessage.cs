namespace ChatOnline.Api.Models.Entities;

/// <summary>
/// 消息类型
/// </summary>
public enum MessageType
{
    Text = 0,    // 文字消息（含 Emoji）
    Image = 1,   // 图片消息
    File = 2     // 文件消息
}

/// <summary>
/// 群聊消息表，记录群里的每一条消息
/// </summary>
public class GroupMessage
{
    public int Id { get; set; }

    // ---- 外键列 ----
    public int? GroupId { get; set; }    // 群解散后置为 null，消息保留
    public int SenderId { get; set; }

    // ---- 消息内容 ----
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? FileUrl { get; set; }

    // ---- 状态 ----
    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性 =====

    // 这条消息属于哪个群
    public ChatGroup Group { get; set; } = null!;

    // 这条消息是谁发的
    public User Sender { get; set; } = null!;
}
