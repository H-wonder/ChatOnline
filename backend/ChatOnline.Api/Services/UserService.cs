using Microsoft.EntityFrameworkCore;
using ChatOnline.Api.Data;
using ChatOnline.Api.Models.Dtos;

namespace ChatOnline.Api.Services;

/// <summary>
/// 用户服务：个人资料查询、修改、马甲管理
/// </summary>
public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取用户资料
    /// </summary>
    public async Task<UserBrief?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return null;

        return new UserBrief
        {
            Id = user.Id,
            Username = user.Username,
            Avatar = user.Avatar,
            Bio = user.Bio
        };
    }

    /// <summary>
    /// 修改个人资料
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(int userId,
        string? avatar, string? bio)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return (false, "用户不存在");

        if (avatar != null)
            user.Avatar = avatar;

        if (bio != null)
            user.Bio = bio;

        await _db.SaveChangesAsync();
        return (true, "修改成功");
    }

    /// <summary>
    /// 修改自己在某个群的匿名马甲
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAnonNicknameAsync(
        int userId, int groupId, string newNickname)
    {
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null)
            return (false, "你不在该群中");

        member.AnonNickname = newNickname;
        await _db.SaveChangesAsync();

        return (true, "马甲已更新");
    }
}
