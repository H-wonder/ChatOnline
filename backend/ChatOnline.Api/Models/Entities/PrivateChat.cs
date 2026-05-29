namespace ChatOnline.Api.Models.Entities;

/// <summary>
/// 私聊会话表，记录两个用户之间的一次私聊关系
/// User1 是发起方，User2 是接收方
/// </summary>
public class PrivateChat
{
    public int Id { get; set; }

    // ---- 外键列 ----
    public int User1Id { get; set; }    // 发起方
    public int User2Id { get; set; }    // 接收方

    // ---- 状态 ----
    // null = 待处理，false = 拒绝，true = 接受
    public bool? IsAccepted { get; set; }

    // ---- 身份暴露标记 ----
    public bool User1Revealed { get; set; } = false;
    public bool User2Revealed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性 =====

    public User User1 { get; set; } = null!;          // 发起方用户
    public User User2 { get; set; } = null!;          // 接收方用户

    // 一个私聊会话有多条消息
    public ICollection<PrivateMessage> Messages { get; set; } = new List<PrivateMessage>();
}
