using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 物料类型枚举
/// </summary>
public enum MaterialType
{
    /// <summary>母材</summary>
    MotherBoard = 0,
    /// <summary>余料</summary>
    Remnant = 1,
    /// <summary>次品</summary>
    Defective = 2
}

/// <summary>
/// 库存表（严格按照需求文档）
/// </summary>
[SugarTable("Inventories")]
public class Inventory
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>条码号</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Barcode { get; set; }

    /// <summary>材料ID</summary>
    [SugarColumn(IsNullable = true)]
    public int? MaterialId { get; set; }

    /// <summary>品种</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>物料类型: 0=母材, 1=余料, 2=次品</summary>
    public MaterialType MaterialType { get; set; } = MaterialType.MotherBoard;

    /// <summary>厚度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Thickness { get; set; }

    /// <summary>长度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Length { get; set; }

    /// <summary>宽度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Width { get; set; }

    /// <summary>件数</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? Quantity { get; set; }

    /// <summary>重量(kg)</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? Weight { get; set; }

    /// <summary>状态</summary>
    public int Status { get; set; } = 0;

    /// <summary>损耗重量</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? LossWeight { get; set; }

    /// <summary>母材ID</summary>
    [SugarColumn(IsNullable = true)]
    public int? RootMaterialId { get; set; }
}
