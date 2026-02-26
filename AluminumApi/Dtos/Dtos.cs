namespace AluminumApi.Dtos;

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<MenuDto> Menus { get; set; } = new();
}

public class MenuDto
{
    public string Url { get; set; } = string.Empty;
    public string UrlName { get; set; } = string.Empty;
}

public class ApiResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string message = "操作成功")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class PageResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class NestingRequest
{
    public List<int> OrderIds { get; set; } = new();
    /// <summary>计算模式: fast=快速, precise=精确</summary>
    public string CalcMode { get; set; } = "fast";
    /// <summary>选料策略: minMatch=最小匹配, remnantFirst=余料优先, largeFirst=大板优先</summary>
    public string SelectStrategy { get; set; } = "minMatch";
    /// <summary>切割方向: auto=自动, horizontal=横向优先, vertical=纵向优先</summary>
    public string CutDirection { get; set; } = "auto";
    /// <summary>固定方向: true=零件不旋转</summary>
    public bool FixedDirection { get; set; } = false;
}

/// <summary>
/// 套料方案DTO
/// </summary>
public class NestingPlanDto
{
    public string MaterialName { get; set; } = string.Empty;
    public decimal Thickness { get; set; }
    public List<NestingPlanItem> Items { get; set; } = new();
    public int TotalPlates { get; set; }
    public int CompletedOrders { get; set; }
    /// <summary>成品重量(kg)</summary>
    public decimal ProductWeight { get; set; }
    /// <summary>余料重量(kg)</summary>
    public decimal RemnantWeight { get; set; }
    /// <summary>废料重量(kg)</summary>
    public decimal WasteWeight { get; set; }
    /// <summary>损耗率(%)</summary>
    public decimal WasteRate { get; set; }
    /// <summary>当前利用率(%)</summary>
    public decimal TotalUtilization { get; set; }
    /// <summary>计算用时(秒)</summary>
    public string CalculateTime { get; set; } = string.Empty;
    public decimal TotalWasteArea { get; set; }
    public decimal TotalWasteRate { get; set; }
}

/// <summary>
/// 套料方案项
/// </summary>
public class NestingPlanItem
{
    public int InventoryId { get; set; }
    /// <summary>条码号</summary>
    public string? Barcode { get; set; }
    public decimal SourceWidth { get; set; }
    public decimal SourceLength { get; set; }
    public int MaterialType { get; set; }
    public List<PlacementInfo> Placements { get; set; } = new();
    public decimal Utilization { get; set; }
    public decimal WasteArea { get; set; }
    /// <summary>成品重量(kg)</summary>
    public decimal ProductWeight { get; set; }
    /// <summary>余料重量(kg)</summary>
    public decimal RemnantWeight { get; set; }
    /// <summary>废料重量(kg)</summary>
    public decimal WasteWeight { get; set; }
    /// <summary>是否有余料可入库</summary>
    public bool HasRemnant { get; set; }
    /// <summary>余料长度(mm)</summary>
    public decimal RemnantLength { get; set; }
    /// <summary>余料宽度(mm)</summary>
    public decimal RemnantWidth { get; set; }
}

/// <summary>
/// 放置信息
/// </summary>
public class PlacementInfo
{
    public int OrderId { get; set; }
    public string OrderIdStr { get; set; } = string.Empty;
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Color { get; set; } = "#67C23A";
}

/// <summary>
/// 确认套料请求
/// </summary>
public class ConfirmNestingRequest
{
    public string MaterialName { get; set; } = string.Empty;
    public decimal Thickness { get; set; }
    public List<NestingPlanItem> Items { get; set; } = new();
}

/// <summary>
/// 列筛选响应（获取列的唯一值列表）
/// </summary>
public class ColumnValuesResult
{
    public string Column { get; set; } = string.Empty;
    public List<string> Values { get; set; } = new();
}
