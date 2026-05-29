namespace ChatOnline.Api.Models.Entities;

/// <summary>
/// 群聊表，一个 ChatGroup 记录就是一个群
/// </summary>
public class ChatGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    public string? Password { get; set; }

    public string? Question { get; set; }

    public string? QuestionAnswer { get; set; }

    public int OwnerId { get; set; }                           // 外键：群主 ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性 =====

    public User Owner { get; set; } = null!;                   // 群主（通过 OwnerId 关联）

    // 一个群有多条成员记录
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    // 一个群有多个消息
    public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
}
