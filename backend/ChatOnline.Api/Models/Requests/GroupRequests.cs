using System.ComponentModel.DataAnnotations;

namespace ChatOnline.Api.Models.Requests;

/// <summary>
/// 创建群聊请求
/// </summary>
public class CreateGroupRequest
{
    [Required(ErrorMessage = "群名称不能为空")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }  // 群简介，可空

    public bool IsPublic { get; set; } = true; // 是否公开

    [MaxLength(100)]
    public string? Password { get; set; }       // 入群密码（可空，表示无密码）

    [MaxLength(200)]
    public string? Question { get; set; }       // 入群问题

    [MaxLength(200)]
    public string? QuestionAnswer { get; set; } // 入群答案
}

/// <summary>
/// 修改群信息请求
/// </summary>
public class UpdateGroupRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Question { get; set; }

    [MaxLength(200)]
    public string? QuestionAnswer { get; set; }
}

/// <summary>
/// 加入群聊请求
/// </summary>
public class JoinGroupRequest
{
    [Required]
    [MaxLength(50)]
    public string AnonNickname { get; set; } = string.Empty;  // 匿名马甲名，必填

    [MaxLength(500)]
    public string? AnonAvatar { get; set; }                    // 匿名头像

    public string? Password { get; set; }        // 入群密码（如果有的话）
    public string? Answer { get; set; }          // 入群问题答案（如果有的话）
}
