using AluminumApi.Dtos;
using AluminumApi.Models;
using MiniExcelLibs;
using SqlSugar;

namespace AluminumApi.Services;

public class InventoryService
{
    private readonly ISqlSugarClient _db;

    public InventoryService(ISqlSugarClient db) => _db = db;

    public async Task<ApiResult<PageResult<Inventory>>> GetListAsync(int page, int pageSize, string? keyword, string? materialName, int? status, string? spec, int? materialType)
    {
        var total = new RefAsync<int>();
        var query = _db.Queryable<Inventory>();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(i => i.MaterialName.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(materialName))
            query = query.Where(i => i.MaterialName.Contains(materialName));

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (materialType.HasValue)
            query = query.Where(i => (int)i.MaterialType == materialType.Value);

        if (!string.IsNullOrWhiteSpace(spec))
        {
            // Search in Width, Length, Thickness as spec
            query = query.Where(i => 
                i.Width.ToString().Contains(spec) || 
                i.Length.ToString().Contains(spec) || 
                i.Thickness.ToString().Contains(spec));
        }

        var items = await query
            .OrderByDescending(i => i.Id)
            .ToPageListAsync(page, pageSize, total);

        return ApiResult<PageResult<Inventory>>.Ok(new PageResult<Inventory>
        {
            Items = items,
            Total = total.Value,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// 获取列的唯一值
    /// </summary>
    public async Task<ApiResult<ColumnValuesResult>> GetColumnValuesAsync(string column)
    {
        var values = new List<string>();
        switch (column.ToLower())
        {
            case "materialname":
                values = await _db.Queryable<Inventory>()
                    .GroupBy(i => i.MaterialName)
                    .Select(i => i.MaterialName)
                    .ToListAsync();
                break;
            case "materialtype":
                values = new List<string> { "母材", "余料", "次品" };
                break;
            case "thickness":
                var thicknesses = await _db.Queryable<Inventory>()
                    .GroupBy(i => i.Thickness)
                    .Select(i => i.Thickness)
                    .ToListAsync();
                values = thicknesses.Select(t => t.ToString()).ToList();
                break;
        }
        return ApiResult<ColumnValuesResult>.Ok(new ColumnValuesResult { Column = column, Values = values });
    }

    public async Task<ApiResult<Inventory>> GetByIdAsync(int id)
    {
        var item = await _db.Queryable<Inventory>().InSingleAsync(id);
        return item == null
            ? ApiResult<Inventory>.Fail("库存不存在")
            : ApiResult<Inventory>.Ok(item);
    }

    public async Task<ApiResult<bool>> CreateAsync(Inventory item)
    {
        item.Status = 0; // 可用

        // Calculate weight from quantity * density if quantity is provided
        if (item.Quantity.HasValue && item.Quantity > 0)
        {
            var variety = await _db.Queryable<Variety>()
                .Where(v => v.MaterialName == item.MaterialName)
                .FirstAsync();

            if (variety != null)
            {
                // Weight (kg) = Volume (mm³) * Density (g/cm³) / 1,000,000
                var volume = item.Width * item.Length * item.Thickness * item.Quantity.Value;
                item.Weight = Math.Round(volume * variety.Density / 1_000_000m, 2);
            }
        }

        await _db.Insertable(item).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "创建成功");
    }

    public async Task<ApiResult<bool>> UpdateAsync(Inventory item)
    {
        // Recalculate weight if quantity changed
        if (item.Quantity.HasValue && item.Quantity > 0)
        {
            var variety = await _db.Queryable<Variety>()
                .Where(v => v.MaterialName == item.MaterialName)
                .FirstAsync();

            if (variety != null)
            {
                var volume = item.Width * item.Length * item.Thickness * item.Quantity.Value;
                item.Weight = Math.Round(volume * variety.Density / 1_000_000m, 2);
            }
        }

        await _db.Updateable(item).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "更新成功");
    }

    public async Task<ApiResult<bool>> DeleteAsync(int id)
    {
        await _db.Deleteable<Inventory>().In(id).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "删除成功");
    }

    public async Task<List<Inventory>> GetAllAsync()
    {
        return await _db.Queryable<Inventory>().ToListAsync();
    }

    public async Task<ApiResult<bool>> ImportAsync(List<Inventory> items)
    {
        foreach (var i in items)
            i.Status = 0;
        await _db.Insertable(items).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, $"成功导入{items.Count}条库存");
    }

    /// <summary>
    /// 获取导入模板
    /// </summary>
    public byte[] GetTemplate()
    {
        var template = new List<Inventory>
        {
            new Inventory { MaterialName = "5052铝镁合金板", Thickness = 2.0m, Width = 1000, Length = 2000, MaterialType = MaterialType.MotherBoard, Quantity = 10, Weight = 5.4m }
        };
        using var stream = new MemoryStream();
        stream.SaveAs(template);
        return stream.ToArray();
    }
}
