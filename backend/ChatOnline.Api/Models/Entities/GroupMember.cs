namespace ChatOnline.Api.Models.Entities;

/// <summary>
/// 群成员角色：数字越小权限越大
/// </summary>
public enum MemberRole
{
    Owner = 0,    // 群主：全部权限
    Admin = 1,    // 管理员：踢人、禁言
    Member = 2    // 普通成员：只能发消息
}

/// <summary>
/// 群成员表，记录「某个用户以什么身份加入了哪个群」
/// 联合唯一：一个用户不能重复加入同一个群 (GroupId + UserId)
/// </summary>
public class GroupMember
{
    public int Id { get; set; }

    // ---- 外键列 ----
    public int GroupId { get; set; }
    public int UserId { get; set; }

    // ---- 匿名马甲 ----
    public string AnonNickname { get; set; } = string.Empty;
    public string? AnonAvatar { get; set; }

    // ---- 权限与状态 ----
    public MemberRole Role { get; set; } = MemberRole.Member;
    public bool IsMuted { get; set; } = false;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // ===== 导航属性 =====

    // 通过 GroupId → ChatGroup.Id 找到对应的群
    public ChatGroup Group { get; set; } = null!;

    // 通过 UserId → User.Id 找到对应的用户
    public User User { get; set; } = null!;
}
