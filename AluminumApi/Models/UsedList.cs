using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 套料记录表（严格按照需求文档）
/// </summary>
[SugarTable("UsedLists")]
public class UsedList
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>库存ID</summary>
    public int InventoryId { get; set; }

    /// <summary>订单ID</summary>
    public int OrderId { get; set; }

    /// <summary>品种</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>厚度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Thickness { get; set; }

    /// <summary>长度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Length { get; set; }

    /// <summary>宽度(mm)</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal Width { get; set; }

    /// <summary>重量</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal? Weight { get; set; }

    /// <summary>配料时间</summary>
    public DateTime UseTime { get; set; } = DateTime.Now;
}
