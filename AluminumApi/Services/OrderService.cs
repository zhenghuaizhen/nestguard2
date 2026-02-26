using AluminumApi.Dtos;
using AluminumApi.Models;
using MiniExcelLibs;
using SqlSugar;

namespace AluminumApi.Services;

public class OrderService
{
    private readonly ISqlSugarClient _db;

    public OrderService(ISqlSugarClient db) => _db = db;

    public async Task<ApiResult<PageResult<Order>>> GetListAsync(int page, int pageSize, string? keyword, string? customer, int? status)
    {
        var total = new RefAsync<int>();
        var query = _db.Queryable<Order>();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(o => o.OrderId.Contains(keyword) || o.Customer.Contains(keyword) || o.MaterialName.Contains(keyword));

        if (!string.IsNullOrWhiteSpace(customer))
            query = query.Where(o => o.Customer.Contains(customer));

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var items = await query
            .OrderByDescending(o => o.Id)
            .ToPageListAsync(page, pageSize, total);

        return ApiResult<PageResult<Order>>.Ok(new PageResult<Order>
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
                values = await _db.Queryable<Order>()
                    .GroupBy(o => o.MaterialName)
                    .Select(o => o.MaterialName)
                    .ToListAsync();
                break;
            case "status":
                values = new List<string> { "未配料", "已配料" };
                break;
            case "thickness":
                var thicknesses = await _db.Queryable<Order>()
                    .GroupBy(o => o.Thickness)
                    .Select(o => o.Thickness)
                    .ToListAsync();
                values = thicknesses.Select(t => t.ToString()).ToList();
                break;
            case "customer":
                values = await _db.Queryable<Order>()
                    .GroupBy(o => o.Customer)
                    .Select(o => o.Customer)
                    .ToListAsync();
                break;
        }
        return ApiResult<ColumnValuesResult>.Ok(new ColumnValuesResult { Column = column, Values = values });
    }

    public async Task<ApiResult<Order>> GetByIdAsync(int id)
    {
        var order = await _db.Queryable<Order>().InSingleAsync(id);
        return order == null
            ? ApiResult<Order>.Fail("订单不存在")
            : ApiResult<Order>.Ok(order);
    }

    public async Task<ApiResult<bool>> CreateAsync(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId))
            order.OrderId = $"ORD{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

        order.Status = 0; // 未配料

        // Calculate weight from quantity * density if quantity is provided
        if (order.Quantity.HasValue && order.Quantity > 0)
        {
            var variety = await _db.Queryable<Variety>()
                .Where(v => v.MaterialName == order.MaterialName)
                .FirstAsync();

            if (variety != null)
            {
                // Weight (kg) = Volume (mm³) * Density (g/cm³) / 1,000,000
                // Volume = Width * Length * Thickness * Quantity
                var volume = order.Width * order.Length * order.Thickness * order.Quantity.Value;
                order.Weight = Math.Round(volume * variety.Density / 1_000_000m, 2);
            }
        }

        await _db.Insertable(order).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "创建成功");
    }

    public async Task<ApiResult<bool>> UpdateAsync(Order order)
    {
        // Recalculate weight if quantity changed
        if (order.Quantity.HasValue && order.Quantity > 0)
        {
            var variety = await _db.Queryable<Variety>()
                .Where(v => v.MaterialName == order.MaterialName)
                .FirstAsync();

            if (variety != null)
            {
                var volume = order.Width * order.Length * order.Thickness * order.Quantity.Value;
                order.Weight = Math.Round(volume * variety.Density / 1_000_000m, 2);
            }
        }

        await _db.Updateable(order).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "更新成功");
    }

    public async Task<ApiResult<bool>> DeleteAsync(int id)
    {
        await _db.Deleteable<Order>().In(id).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "删除成功");
    }

    public async Task<ApiResult<bool>> BatchDeleteAsync(List<int> ids)
    {
        await _db.Deleteable<Order>().In(ids).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "批量删除成功");
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Queryable<Order>().ToListAsync();
    }

    public async Task<ApiResult<bool>> ImportAsync(List<Order> orders)
    {
        foreach (var o in orders)
        {
            if (string.IsNullOrWhiteSpace(o.OrderId))
                o.OrderId = $"ORD{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
            o.Status = 0;
        }
        await _db.Insertable(orders).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, $"成功导入{orders.Count}条订单");
    }

    /// <summary>
    /// 获取导入模板
    /// </summary>
    public byte[] GetTemplate()
    {
        var template = new List<Order>
        {
            new Order { OrderId = "ORD001", Customer = "示例客户", MaterialName = "5052铝镁合金板", Thickness = 2.0m, Width = 1000, Length = 2000, Quantity = 10, Weight = 5.4m }
        };
        using var stream = new MemoryStream();
        stream.SaveAs(template);
        return stream.ToArray();
    }
}
