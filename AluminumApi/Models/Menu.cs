using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 菜单路由表（严格按照需求文档）
/// </summary>
[SugarTable("Menus")]
public class Menu
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>路由地址</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Url { get; set; } = string.Empty;

    /// <summary>菜单名</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string UrlName { get; set; } = string.Empty;
}

/// <summary>
/// 用户权限表（严格按照需求文档）
/// </summary>
[SugarTable("UserRoles")]
public class UserRole
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>用户ID</summary>
    public int UserId { get; set; }

    /// <summary>菜单ID</summary>
    public int MenuId { get; set; }
}
