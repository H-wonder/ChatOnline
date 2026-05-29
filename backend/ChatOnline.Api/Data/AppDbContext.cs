using Microsoft.EntityFrameworkCore;
using ChatOnline.Api.Models.Entities;

namespace ChatOnline.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatGroup> ChatGroups => Set<ChatGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();
    public DbSet<PrivateChat> PrivateChats => Set<PrivateChat>();
    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== User =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();          // 用户名唯一

            entity.Property(u => u.Username).HasMaxLength(50);   // VARCHAR(50)
            entity.Property(u => u.PasswordHash).HasMaxLength(255); // VARCHAR(255)
            entity.Property(u => u.Avatar).HasMaxLength(500);
            entity.Property(u => u.Bio).HasMaxLength(200);

            // 用户的所有关联：禁止级联删除
            entity.HasMany(u => u.GroupMembers)
                  .WithOne(gm => gm.User)
                  .HasForeignKey(gm => gm.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.GroupMessages)
                  .WithOne(gm => gm.Sender)
                  .HasForeignKey(gm => gm.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.PrivateMessages)
                  .WithOne(pm => pm.Sender)
                  .HasForeignKey(pm => pm.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== ChatGroup =====
        modelBuilder.Entity<ChatGroup>(entity =>
        {
            entity.Property(g => g.Name).HasMaxLength(100);
            entity.Property(g => g.Description).HasMaxLength(500);
            entity.Property(g => g.Password).HasMaxLength(100);
            entity.Property(g => g.Question).HasMaxLength(200);
            entity.Property(g => g.QuestionAnswer).HasMaxLength(200);

            // 群主 → User，禁止删了 user 却删了群
            entity.HasOne(g => g.Owner)
                  .WithMany()
                  .HasForeignKey(g => g.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 群的成员记录 → 可以级联删除（群解散后成员记录没有保留价值）
            entity.HasMany(g => g.Members)
                  .WithOne(gm => gm.Group)
                  .HasForeignKey(gm => gm.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);       // 删群 → 自动删成员

            // 群的消息 → 群解散后消息保留，GroupId 置空
            entity.HasMany(g => g.Messages)
                  .WithOne(gm => gm.Group)
                  .HasForeignKey(gm => gm.GroupId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== GroupMember =====
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique(); // 联合唯一

            entity.Property(gm => gm.AnonNickname).HasMaxLength(50);
            entity.Property(gm => gm.AnonAvatar).HasMaxLength(500);
        });

        // ===== GroupMessage =====
        modelBuilder.Entity<GroupMessage>(entity =>
        {
            entity.HasIndex(gm => gm.GroupId);       // 性能索引
            entity.HasIndex(gm => gm.CreatedAt);

            entity.Property(gm => gm.FileUrl).HasMaxLength(500);
        });

        // ===== PrivateChat =====
        modelBuilder.Entity<PrivateChat>(entity =>
        {
            entity.HasIndex(pc => new { pc.User1Id, pc.User2Id }).IsUnique();

            entity.HasOne(pc => pc.User1)
                  .WithMany(u => u.PrivateChatsAsUser1)
                  .HasForeignKey(pc => pc.User1Id)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pc => pc.User2)
                  .WithMany(u => u.PrivateChatsAsUser2)
                  .HasForeignKey(pc => pc.User2Id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== PrivateMessage =====
        modelBuilder.Entity<PrivateMessage>(entity =>
        {
            entity.HasIndex(pm => pm.ChatId);
            entity.HasIndex(pm => pm.CreatedAt);

            entity.Property(pm => pm.FileUrl).HasMaxLength(500);

            // 私聊消息 → 私聊会话：级联删除（私聊结束，消息不需要保留）
            entity.HasOne(pm => pm.Chat)
                  .WithMany(pc => pc.Messages)
                  .HasForeignKey(pm => pm.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
