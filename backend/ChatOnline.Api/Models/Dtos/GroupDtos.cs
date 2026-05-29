namespace ChatOnline.Api.Models.Dtos;

/// <summary>
/// 群聊大厅列表中的每一项
/// </summary>
public class GroupBrief
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MemberCount { get; set; }        // 在线成员数
    public bool HasPassword { get; set; }       // 是否有密码（前端据此显示锁图标）
    public bool HasQuestion { get; set; }       // 是否有入群问题
    public OwnerInfo? Owner { get; set; }       // 群主简要信息
    public DateTime CreatedAt { get; set; }
}

public class OwnerInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// 群聊详情（进入群聊后展示）
/// </summary>
public class GroupDetail
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public List<MemberDto> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 群成员信息（群内展示用匿名马甲）
/// </summary>
public class MemberDto
{
    public int UserId { get; set; }
    public string AnonNickname { get; set; } = string.Empty;  // 匿名马甲名
    public string? AnonAvatar { get; set; }
    public string Role { get; set; } = string.Empty;   // "群主" / "管理员" / "成员"
    public bool IsMuted { get; set; }
    public bool IsOnline { get; set; }                  // 是否在线
}
