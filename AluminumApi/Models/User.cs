using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 用户角色类型枚举
/// </summary>
public enum RoleType
{
    /// <summary>普通用户</summary>
    User = 0,
    /// <summary>管理员</summary>
    Admin = 1
}

/// <summary>
/// 用户状态枚举
/// </summary>
public enum UserStatus
{
    /// <summary>禁用</summary>
    Disabled = 0,
    /// <summary>启用</summary>
    Enabled = 1
}

/// <summary>
/// 用户表（严格按照需求文档）
/// </summary>
[SugarTable("Users")]
public class User
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>用户ID</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>用户名称</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>手机号码</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? Tel { get; set; }

    /// <summary>密码</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Password { get; set; } = string.Empty;

    /// <summary>用户角色: 0=普通用户, 1=管理员</summary>
    public RoleType Role { get; set; } = RoleType.User;

    /// <summary>账户状态: 0=禁用, 1=启用</summary>
    public UserStatus Status { get; set; } = UserStatus.Enabled;
}
