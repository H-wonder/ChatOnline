using Microsoft.EntityFrameworkCore;
using ChatOnline.Api.Data;
using ChatOnline.Api.Models.Dtos;
using ChatOnline.Api.Models.Entities;
using ChatOnline.Api.Models.Requests;

namespace ChatOnline.Api.Services;

/// <summary>
/// 群聊服务：群的创建、加入、管理、列表查询
/// </summary>
public class GroupService
{
    private readonly AppDbContext _db;

    public GroupService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 群聊大厅列表：返回所有公开群，支持搜索
    /// </summary>
    public async Task<List<GroupBrief>> GetPublicGroupsAsync(string? search = null)
    {
        IQueryable<ChatGroup> query = _db.ChatGroups
            .Where(g => g.IsPublic);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g => g.Name.Contains(search));
        }

        return await query
            .Include(g => g.Members)          // 同时加载成员
            .Include(g => g.Owner)            // 同时加载群主
            .Select(g => new GroupBrief
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                MemberCount = g.Members.Count,
                HasPassword = g.Password != null,
                HasQuestion = g.Question != null,
                Owner = g.Owner != null ? new OwnerInfo
                {
                    Id = g.Owner.Id,
                    Username = g.Owner.Username
                } : null,
                CreatedAt = g.CreatedAt
            }).ToListAsync();
    }

    /// <summary>
    /// 获取我加入的所有群（侧边栏用）
    /// </summary>
    public async Task<List<GroupBrief>> GetMyGroupsAsync(int userId)
    {
        return await _db.GroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Group)
                .ThenInclude(g => g.Members)
            .Include(m => m.Group)
                .ThenInclude(g => g.Owner)
            .Select(m => new GroupBrief
            {
                Id = m.Group.Id,
                Name = m.Group.Name,
                Description = m.Group.Description,
                MemberCount = m.Group.Members.Count,
                HasPassword = m.Group.Password != null,
                HasQuestion = m.Group.Question != null,
                Owner = m.Group.Owner != null ? new OwnerInfo
                {
                    Id = m.Group.Owner.Id,
                    Username = m.Group.Owner.Username
                } : null,
                CreatedAt = m.Group.CreatedAt
            })
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 创建群聊
    /// </summary>
    public async Task<GroupBrief> CreateGroupAsync(int ownerId, CreateGroupRequest request)
    {
        var group = new ChatGroup
        {
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            Password = request.Password,
            Question = request.Question,
            QuestionAnswer = request.QuestionAnswer,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatGroups.Add(group);
        await _db.SaveChangesAsync();  // 先保存群，拿到数据库分配的 group.Id

        // 创建者自动加入群聊，成为群主
        var member = new GroupMember
        {
            GroupId = group.Id,        // 这时 group.Id 已经是数据库自增的真实 ID
            UserId = ownerId,
            AnonNickname = "(群主)",
            Role = MemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        _db.GroupMembers.Add(member);

        await _db.SaveChangesAsync();

        return new GroupBrief
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            MemberCount = 1,
            HasPassword = group.Password != null,
            HasQuestion = group.Question != null,
            CreatedAt = group.CreatedAt
        };
    }

    /// <summary>
    /// 加入群聊
    /// </summary>
    public async Task<(bool Success, string Message)> JoinGroupAsync(int userId, int groupId, JoinGroupRequest request)
    {
        var group = await _db.ChatGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return (false, "群不存在");

        // 检查是否已是成员
        if (group.Members.Any(m => m.UserId == userId))
            return (false, "你已在该群中");

        // 验证入群密码
        if (group.Password != null && group.Password != request.Password)
            return (false, "入群密码错误");

        // 验证入群问题答案
        if (group.Question != null && group.QuestionAnswer != null
            && !string.Equals(group.QuestionAnswer, request.Answer, StringComparison.OrdinalIgnoreCase))
            return (false, "问题答案错误");

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            AnonNickname = request.AnonNickname,
            AnonAvatar = request.AnonAvatar,
            Role = MemberRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync();

        return (true, "加入成功");
    }

    /// <summary>
    /// 退出群聊
    /// </summary>
    public async Task<(bool Success, string Message)> LeaveGroupAsync(int userId, int groupId)
    {
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            return (false, "你不在该群中");

        if (member.Role == MemberRole.Owner)
            return (false, "群主不能直接退群，请先转让群主或解散群");

        _db.GroupMembers.Remove(member);
        await _db.SaveChangesAsync();

        return (true, "已退出群聊");
    }

    /// <summary>
    /// 群聊详情
    /// </summary>
    public async Task<GroupDetail?> GetGroupDetailAsync(int groupId)
    {
        var group = await _db.ChatGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return null;

        return new GroupDetail
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsPublic = group.IsPublic,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new MemberDto
            {
                UserId = m.UserId,
                AnonNickname = m.AnonNickname,
                AnonAvatar = m.AnonAvatar,
                Role = m.Role switch
                {
                    MemberRole.Owner => "群主",
                    MemberRole.Admin => "管理员",
                    _ => "成员"
                },
                IsMuted = m.IsMuted,
                IsOnline = false  // 在线状态由 SignalR 维护，此处默认 false
            }).ToList()
        };
    }

    /// <summary>
    /// 踢出成员
    /// </summary>
    public async Task<(bool Success, string Message)> KickMemberAsync(int operatorId, int groupId, int targetUserId)
    {
        var operatorMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == operatorId);

        if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Admin))
            return (false, "没有权限执行此操作");

        var targetMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return (false, "目标用户不在群中");

        if (targetMember.Role == MemberRole.Owner)
            return (false, "不能踢出群主");

        if (operatorMember.Role == MemberRole.Admin && targetMember.Role == MemberRole.Admin)
            return (false, "管理员不能踢出其他管理员");

        _db.GroupMembers.Remove(targetMember);
        await _db.SaveChangesAsync();

        return (true, "已踢出");
    }

    /// <summary>
    /// 禁言 / 解禁
    /// </summary>
    public async Task<(bool Success, string Message)> ToggleMuteAsync(int operatorId, int groupId, int targetUserId, bool mute)
    {
        var operatorMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == operatorId);

        if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Admin))
            return (false, "没有权限");

        var targetMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return (false, "目标用户不在群中");

        if (targetMember.Role == MemberRole.Owner)
            return (false, "不能禁言群主");

        targetMember.IsMuted = mute;
        await _db.SaveChangesAsync();

        return (true, mute ? "已禁言" : "已解除禁言");
    }

    /// <summary>
    /// 设置管理员
    /// </summary>
    public async Task<(bool Success, string Message)> SetAdminAsync(int ownerId, int groupId, int targetUserId, bool isAdmin)
    {
        var targetMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId);

        if (targetMember == null)
            return (false, "目标用户不在群中");

        // 验证操作者是群主
        var ownerMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == ownerId && m.Role == MemberRole.Owner);

        if (ownerMember == null)
            return (false, "只有群主可以设置管理员");

        targetMember.Role = isAdmin ? MemberRole.Admin : MemberRole.Member;
        await _db.SaveChangesAsync();

        return (true, isAdmin ? "已设为管理员" : "已取消管理员");
    }

    /// <summary>
    /// 修改群信息（群主/管理员）
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateGroupAsync(int operatorId, int groupId, string? name, string? description, string? password, string? question, string? questionAnswer)
    {
        var operatorMember = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == operatorId);

        if (operatorMember == null || (operatorMember.Role != MemberRole.Owner && operatorMember.Role != MemberRole.Admin))
            return (false, "没有权限");

        var group = await _db.ChatGroups.FindAsync(groupId);
        if (group == null) return (false, "群不存在");

        if (name != null) group.Name = name;
        if (description != null) group.Description = description;
        if (password != null) group.Password = password;
        if (question != null) group.Question = question;
        if (questionAnswer != null) group.QuestionAnswer = questionAnswer;

        await _db.SaveChangesAsync();
        return (true, "修改成功");
    }

    /// <summary>
    /// 解散群聊
    /// </summary>
    public async Task<(bool Success, string Message)> DissolveGroupAsync(int userId, int groupId)
    {
        var group = await _db.ChatGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return (false, "群不存在");

        if (group.OwnerId != userId)
            return (false, "只有群主可以解散群");

        _db.ChatGroups.Remove(group);          // 级联删除成员记录
        // 消息由于 OnDelete(Restrict) 不会被删除，保留
        await _db.SaveChangesAsync();

        return (true, "群已解散");
    }
}
