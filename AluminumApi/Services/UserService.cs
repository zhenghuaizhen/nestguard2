using AluminumApi.Dtos;
using AluminumApi.Models;
using SqlSugar;

namespace AluminumApi.Services;

public class UserService
{
    private readonly ISqlSugarClient _db;

    public UserService(ISqlSugarClient db) => _db = db;

    public async Task<ApiResult<PageResult<User>>> GetListAsync(int page, int pageSize, string? keyword)
    {
        var total = new RefAsync<int>();
        var query = _db.Queryable<User>();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.UserName.Contains(keyword) || u.UserId.Contains(keyword));

        var items = await query
            .OrderByDescending(u => u.Id)
            .ToPageListAsync(page, pageSize, total);

        // Remove password from response
        foreach (var item in items)
            item.Password = "";

        return ApiResult<PageResult<User>>.Ok(new PageResult<User>
        {
            Items = items,
            Total = total.Value,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResult<User>> GetByIdAsync(int id)
    {
        var user = await _db.Queryable<User>().InSingleAsync(id);
        if (user == null)
            return ApiResult<User>.Fail("用户不存在");

        user.Password = "";
        return ApiResult<User>.Ok(user);
    }

    public async Task<ApiResult<bool>> CreateAsync(User user)
    {
        // Check if userId already exists
        var exists = await _db.Queryable<User>().Where(u => u.UserId == user.UserId).AnyAsync();
        if (exists)
            return ApiResult<bool>.Fail("用户ID已存在");

        // Hash password (simple hash for demo)
        user.Password = HashPassword(user.Password);
        
        await _db.Insertable(user).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "创建成功");
    }

    public async Task<ApiResult<bool>> UpdateAsync(User user)
    {
        var existing = await _db.Queryable<User>().InSingleAsync(user.Id);
        if (existing == null)
            return ApiResult<bool>.Fail("用户不存在");

        // Only update allowed fields
        existing.UserName = user.UserName;
        existing.Tel = user.Tel;
        existing.Role = user.Role;
        existing.Status = user.Status;
        
        // Update password only if provided
        if (!string.IsNullOrWhiteSpace(user.Password) && user.Password != existing.Password)
            existing.Password = HashPassword(user.Password);

        await _db.Updateable(existing).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "更新成功");
    }

    public async Task<ApiResult<bool>> DeleteAsync(int id)
    {
        // Prevent deleting the last admin
        var user = await _db.Queryable<User>().InSingleAsync(id);
        if (user?.UserId == "admin")
            return ApiResult<bool>.Fail("不能删除管理员账户");

        await _db.Deleteable<User>().In(id).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "删除成功");
    }

    private string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
