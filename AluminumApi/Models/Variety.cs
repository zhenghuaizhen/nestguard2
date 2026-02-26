using SqlSugar;

namespace AluminumApi.Models;

/// <summary>
/// 品种设置表/Material（严格按照需求文档）
/// </summary>
[SugarTable("Materials")]
public class Variety
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>品种</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>密度</summary>
    [SugarColumn(DecimalDigits = 4)]
    public decimal Density { get; set; } = 2.7m;

    /// <summary>成本单价(元/kg)</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? CostPerKg { get; set; }

    /// <summary>最低库存(kg)</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? MinStock { get; set; }

    /// <summary>最高库存(kg)</summary>
    [SugarColumn(DecimalDigits = 2, IsNullable = true)]
    public decimal? MaxStock { get; set; }
}
