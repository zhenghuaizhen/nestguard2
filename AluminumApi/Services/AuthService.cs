using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AluminumApi.Dtos;
using AluminumApi.Models;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;

namespace AluminumApi.Services;

public class AuthService
{
    private readonly ISqlSugarClient _db;
    private readonly IConfiguration _config;

    public AuthService(ISqlSugarClient db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<ApiResult<LoginResult>> LoginAsync(LoginDto dto)
    {
        var user = await _db.Queryable<User>()
            .FirstAsync(u => u.UserId == dto.Username);

        if (user == null || !VerifyPassword(dto.Password, user.Password))
            return ApiResult<LoginResult>.Fail("用户名或密码错误");

        // 获取用户菜单权限
        var menus = await _db.Queryable<Menu, UserRole>((m, r) => new JoinQueryInfos(
                JoinType.Inner, m.Id == r.MenuId
            ))
            .Where((m, r) => r.UserId == user.Id)
            .Select((m, r) => m)
            .ToListAsync();

        var token = GenerateToken(user);
        return ApiResult<LoginResult>.Ok(new LoginResult
        {
            Token = token,
            Username = user.UserId,
            DisplayName = user.UserName,
            Menus = menus.Select(m => new MenuDto
            {
                Url = m.Url,
                UrlName = m.UrlName
            }).ToList()
        });
    }

    public async Task<ApiResult<bool>> RegisterAsync(RegisterDto dto)
    {
        var exists = await _db.Queryable<User>()
            .AnyAsync(u => u.UserId == dto.Username);

        if (exists)
            return ApiResult<bool>.Fail("用户名已存在");

        var user = new User
        {
            UserId = dto.Username,
            UserName = dto.DisplayName ?? dto.Username,
            Password = HashPassword(dto.Password)
        };

        await _db.Insertable(user).ExecuteCommandAsync();
        return ApiResult<bool>.Ok(true, "注册成功");
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "AluminumApi_SecretKey_2024!@#$%^&*()"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserId),
            new Claim(ClaimTypes.Role, "user")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "AluminumApi",
            audience: _config["Jwt:Audience"] ?? "AluminumApp",
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var storedHash = Convert.FromBase64String(parts[1]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, storedHash);
    }
}
