namespace ChatOnline.Api.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性：告诉 EF Core 这个类和其他类的关系 =====

    // 一个用户可以加入多个群，所以是集合
    public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    // 一个用户可以发多条群消息
    public ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();

    // 一个用户可以发起多个私聊（User1 表示发起方）
    public ICollection<PrivateChat> PrivateChatsAsUser1 { get; set; } = new List<PrivateChat>();

    // 一个用户也可以被多个私聊请求（User2 表示接收方）
    public ICollection<PrivateChat> PrivateChatsAsUser2 { get; set; } = new List<PrivateChat>();

    // 一个用户可以发多条私聊消息
    public ICollection<PrivateMessage> PrivateMessages { get; set; } = new List<PrivateMessage>();
}
