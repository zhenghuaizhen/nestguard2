using AluminumApi.Dtos;
using AluminumApi.Models;
using SqlSugar;

namespace AluminumApi.Services;

public class WasteSettingService
{
    private readonly ISqlSugarClient _db;

    public WasteSettingService(ISqlSugarClient db) => _db = db;

    public async Task<ApiResult<WasteSetting>> GetAsync()
    {
        var setting = await _db.Queryable<WasteSetting>().FirstAsync();
        return ApiResult<WasteSetting>.Ok(setting ?? new WasteSetting());
    }

    public async Task<ApiResult<bool>> SaveAsync(WasteSetting setting)
    {
        var existing = await _db.Queryable<WasteSetting>().FirstAsync();
        if (existing != null)
        {
            setting.Id = existing.Id;
            await _db.Updateable(setting).ExecuteCommandAsync();
        }
        else
        {
            await _db.Insertable(setting).ExecuteCommandAsync();
        }
        return ApiResult<bool>.Ok(true, "保存成功");
    }
}
