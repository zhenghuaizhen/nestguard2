using System.Text;
using AluminumApi.Models;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

// ===== SqlSugar =====
builder.Services.AddSingleton<ISqlSugarClient>(sp =>
{
    var db = new SqlSugarScope(new ConnectionConfig
    {
        ConnectionString = "DataSource=aluminum.db",
        DbType = DbType.Sqlite,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });
    return db;
});

// ===== JWT Authentication =====
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AluminumApi_SecretKey_2024!@#$%^&*()";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AluminumApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AluminumApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// ===== Services =====
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<WasteSettingService>();
builder.Services.AddScoped<NestingService>();
builder.Services.AddScoped<VarietyService>();
builder.Services.AddScoped<UserService>();

// ===== Controllers + CORS =====
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// ===== Init Database =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    db.DbMaintenance.CreateDatabase();
    db.CodeFirst.InitTables<User>();
    db.CodeFirst.InitTables<Menu>();
    db.CodeFirst.InitTables<UserRole>();
    db.CodeFirst.InitTables<Order>();
    db.CodeFirst.InitTables<Inventory>();
    db.CodeFirst.InitTables<WasteSetting>();
    db.CodeFirst.InitTables<UsedList>();
    db.CodeFirst.InitTables<Variety>();

    // Seed default admin user
    if (!db.Queryable<User>().Any(u => u.UserId == "admin"))
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "admin123", salt, 100000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        var passwordHash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        db.Insertable(new User
        {
            UserId = "admin",
            UserName = "管理员",
            Password = passwordHash,
            Tel = "13800138000"
        }).ExecuteCommand();
    }

    // Seed default waste setting
    if (!db.Queryable<WasteSetting>().Any())
    {
        db.Insertable(new WasteSetting
        {
            MinLength = 100,
            MinWidth = 100,
            MaxWasteArea = 0.1m
        }).ExecuteCommand();
    }

    // Seed default menus
    if (!db.Queryable<Menu>().Any())
    {
        var menus = new[]
        {
            new Menu { Url = "/dashboard", UrlName = "主控制台" },
            new Menu { Url = "/order", UrlName = "订单管理" },
            new Menu { Url = "/inventory", UrlName = "库存管理" },
            new Menu { Url = "/variety", UrlName = "品种设置" },
            new Menu { Url = "/waste", UrlName = "废料设置" },
            new Menu { Url = "/nesting", UrlName = "智能套料任务" },
            new Menu { Url = "/result", UrlName = "历史记录" },
            new Menu { Url = "/user", UrlName = "用户管理" }
        };
        db.Insertable(menus).ExecuteCommand();
    }

    // Seed admin user role (all menus)
    var adminUser = db.Queryable<User>().First(u => u.UserId == "admin");
    if (adminUser != null && !db.Queryable<UserRole>().Any(r => r.UserId == adminUser.Id))
    {
        var allMenus = db.Queryable<Menu>().ToList();
        var userRoles = allMenus.Select(m => new UserRole { UserId = adminUser.Id, MenuId = m.Id }).ToArray();
        db.Insertable(userRoles).ExecuteCommand();
    }

    // Seed default materials
    if (!db.Queryable<Variety>().Any())
    {
        var materials = new[]
        {
            new Variety { MaterialName = "1050纯铝板", Density = 2.71m },
            new Variety { MaterialName = "1060纯铝板", Density = 2.71m },
            new Variety { MaterialName = "3003防锈铝板", Density = 2.73m },
            new Variety { MaterialName = "5052铝镁合金板", Density = 2.68m },
            new Variety { MaterialName = "5083铝镁合金板", Density = 2.66m },
            new Variety { MaterialName = "6061铝镁硅合金板", Density = 2.70m },
            new Variety { MaterialName = "7075超硬铝板", Density = 2.81m }
        };
        db.Insertable(materials).ExecuteCommand();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
