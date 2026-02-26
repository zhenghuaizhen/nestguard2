using AluminumApi.Dtos;
using AluminumApi.Models;
using SqlSugar;

namespace AluminumApi.Services;

public class VarietyService
{
    private readonly ISqlSugarClient _db;

    public VarietyService(ISqlSugarClient db) => _db = db;

    public async Task<ApiResult<PageResult<Variety>>> GetListAsync(int page, int pageSize, string? keyword)
    {
        var total = new RefAsync<int>();
        var query = _db.Queryable<Variety>();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(v => v.MaterialName.Contains(keyword));

        var items = await query
            .OrderByDescending(v => v.Id)
            .ToPageListAsync(page, pageSize, total);

        return ApiResult<PageResult<Variety>>.Ok(new PageResult<Variety>
        {
            Items = items,
            Total = total.Value,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResult<Variety>> GetByIdAsync(int id)
    {
        var item = await _db.Queryable<Variety>().InSingleAsync(id);
        return item == null
            ? ApiResult<Variety>.Fail("品种不存在")
            : ApiResult<Variety>.Ok(item);
    }

    public async Task<ApiResult<bool>> CreateAsync(Variety item)
    {
        await _db.Insertable(item).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "创建成功");
    }

    public async Task<ApiResult<bool>> UpdateAsync(Variety item)
    {
        await _db.Updateable(item).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "更新成功");
    }

    public async Task<ApiResult<bool>> DeleteAsync(int id)
    {
        await _db.Deleteable<Variety>().In(id).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "删除成功");
    }

    public async Task<ApiResult<bool>> BatchDeleteAsync(List<int> ids)
    {
        await _db.Deleteable<Variety>().In(ids).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "批量删除成功");
    }

    public async Task<List<Variety>> GetAllAsync()
    {
        return await _db.Queryable<Variety>().ToListAsync();
    }
}
