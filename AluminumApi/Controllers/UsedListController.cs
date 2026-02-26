using AluminumApi.Dtos;
using AluminumApi.Models;
using SqlSugar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsedListController : ControllerBase
{
    private readonly ISqlSugarClient _db;

    public UsedListController(ISqlSugarClient db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetList(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var total = new RefAsync<int>();
        var query = _db.Queryable<UsedList, Order, Inventory>(
            (u, o, i) => new JoinQueryInfos(
                JoinType.Left, u.OrderId == o.Id,
                JoinType.Left, u.InventoryId == i.Id
            ));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where((u, o, i) => u.MaterialName.Contains(keyword) || o.OrderId.Contains(keyword) || o.Customer.Contains(keyword));
        }

        var items = await query
            .Select((u, o, i) => new
            {
                u.Id,
                u.MaterialName,
                u.Thickness,
                u.Width,
                u.Length,
                u.Weight,
                u.UseTime,
                OrderId = o.OrderId,
                Customer = o.Customer,
                InventoryInfo = $"厚度{u.Thickness}mm {u.Width}x{u.Length}mm"
            })
            .OrderByDescending(u => u.UseTime)
            .ToPageListAsync(page, pageSize, total);

        return Ok(ApiResult<PageResult<object>>.Ok(new PageResult<object>
        {
            Items = items.Cast<object>().ToList(),
            Total = total.Value,
            Page = page,
            PageSize = pageSize
        }));
    }
}
