using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 订单表（严格按照需求文档）
/// </summary>
[SugarTable("Orders")]
public class Order
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>订单号</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>客户</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Customer { get; set; } = string.Empty;

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

    /// <summary>件数</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? Quantity { get; set; }

    /// <summary>重量(kg)</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? Weight { get; set; }

    /// <summary>状态: 0=未配料, 1=已配料</summary>
    public int Status { get; set; } = 0;
}
