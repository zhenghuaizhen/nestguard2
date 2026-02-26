using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 废料尺寸设置（严格按照需求文档）
/// </summary>
[SugarTable("WasteSettings")]
public class WasteSetting
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>最小长度(mm)，小于此值视为废料</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal MinLength { get; set; } = 100;

    /// <summary>最小宽度(mm)，小于此值视为废料</summary>
    [SugarColumn(DecimalDigits = 2)]
    public decimal MinWidth { get; set; } = 100;

    /// <summary>最大废料面积(平方米)，小于此值视为废料</summary>
    [SugarColumn(DecimalDigits = 4)]
    public decimal MaxWasteArea { get; set; } = 0.1m;
}
