using AluminumApi.Dtos;
using AluminumApi.Models;
using SqlSugar;

namespace AluminumApi.Services;

public class NestingService
{
    private readonly ISqlSugarClient _db;
    private readonly WasteSettingService _wasteService;

    public NestingService(ISqlSugarClient db, WasteSettingService wasteService)
    {
        _db = db;
        _wasteService = wasteService;
    }

    /// <summary>
    /// 获取可套料的订单
    /// </summary>
    public async Task<ApiResult<List<Order>>> GetAvailableOrdersAsync()
    {
        var orders = await _db.Queryable<Order>()
            .Where(o => o.Status == 0)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
        return ApiResult<List<Order>>.Ok(orders);
    }

    /// <summary>
    /// 计算套料方案（优化版）
    /// </summary>
    public async Task<ApiResult<NestingPlanDto>> CalculateAsync(NestingRequest request)
    {
        var startTime = DateTime.Now;
        
        if (request.OrderIds.Count == 0)
            return ApiResult<NestingPlanDto>.Fail("请选择要配料的订单");

        var orders = await _db.Queryable<Order>()
            .Where(o => request.OrderIds.Contains(o.Id))
            .ToListAsync();

        if (orders.Count == 0)
            return ApiResult<NestingPlanDto>.Fail("未找到选中的订单");

        var firstOrder = orders.First();
        var materialName = firstOrder.MaterialName;
        var thickness = firstOrder.Thickness;

        if (orders.Any(o => o.MaterialName != materialName || o.Thickness != thickness))
            return ApiResult<NestingPlanDto>.Fail("所选订单的品种和厚度必须一致");

        // 查询库存
        var inventories = await _db.Queryable<Inventory>()
            .Where(i => i.MaterialName == materialName && i.Thickness == thickness && i.Status == 0)
            .ToListAsync();

        if (inventories.Count == 0)
            return ApiResult<NestingPlanDto>.Fail($"没有匹配的库存物料（品种: {materialName}, 厚度: {thickness}mm）。请先在库存管理中添加相应的库存。");

        // 智能库存选择 - 根据订单需求选择最合适的板材
        inventories = SmartSelectInventory(inventories, orders, request.SelectStrategy);

        // 获取品种密度用于计算重量
        var variety = await _db.Queryable<Variety>()
            .FirstAsync(v => v.MaterialName == materialName);
        var density = variety?.Density ?? 2.71m; // 默认铝板密度

        // 获取废料设置
        var wasteSettings = await _wasteService.GetAsync();
        var minLength = wasteSettings.Data?.MinLength ?? 100;
        var minWidth = wasteSettings.Data?.MinWidth ?? 100;
        var maxWasteArea = wasteSettings.Data?.MaxWasteArea ?? 0.1m; // 平方米

        // 将订单展开为零件列表（考虑件数）
        var allParts = new List<PartInfo>();
        foreach (var order in orders)
        {
            // 验证订单尺寸
            if (order.Width <= 0 || order.Length <= 0)
                return ApiResult<NestingPlanDto>.Fail($"订单 {order.OrderId} 的尺寸无效（宽: {order.Width}, 长: {order.Length}）");
            
            // 确保正确转换decimal?为int
            var qty = (int)(order.Quantity ?? 1);
            if (qty <= 0) qty = 1;
            
            for (int i = 0; i < qty; i++)
            {
                allParts.Add(new PartInfo
                {
                    OrderId = order.Id,
                    OrderIdStr = order.OrderId,
                    Width = order.Width,
                    Length = order.Length,
                    Index = i
                });
            }
        }
        
        // 调试日志：记录展开后的零件数量
        System.Diagnostics.Debug.WriteLine($"[Nesting] 展开零件总数: {allParts.Count}, 按订单: {string.Join(", ", allParts.GroupBy(p => p.OrderIdStr).Select(g => $"{g.Key}:{g.Count()}"))}");

        // 按面积降序排列零件（大件先排）- 这是排样算法的最佳实践
        allParts = allParts.OrderByDescending(p => p.Width * p.Length).ToList();

        var planItems = new List<NestingPlanItem>();
        var remainingParts = new List<PartInfo>(allParts);
        
        // 用于跟踪同一库存条码的使用次数
        var barcodeUsageCount = new Dictionary<string, int>();

        foreach (var inv in inventories)
        {
            if (remainingParts.Count == 0) break;
            
            // 获取该库存的数量（默认为1）
            var boardCount = inv.Quantity ?? 1;
            
            // 根据数量处理多张相同规格的板
            for (int boardIndex = 0; boardIndex < boardCount && remainingParts.Count > 0; boardIndex++)
            {
                var placements = new List<PlacementInfo>();
                var placedParts = new List<PartInfo>();
                
                // 使用优化的放置算法
                decimal currentX = 0;
                decimal currentY = 0;
                decimal rowHeight = 0;

                foreach (var part in remainingParts.ToList())
                {
                    // 尝试放置零件 - 使用改进的旋转决策逻辑
                    var placed = TryPlacePartOptimized(
                        part, inv, ref currentX, ref currentY, ref rowHeight,
                        request.CutDirection, request.FixedDirection, placements);

                    if (placed != null)
                    {
                        placements.Add(placed);
                        placedParts.Add(part);
                    }
                }

                if (placements.Count > 0)
                {
                    var usedArea = placements.Sum(p => p.Width * p.Length);
                    var totalArea = inv.Width * inv.Length;
                    var wasteArea = totalArea - usedArea;
                    
                    // 计算重量
                    decimal CalculateWeight(decimal area, decimal thick, decimal dens) 
                        => area * thick * dens / 1000000m;
                    
                    var productWeight = CalculateWeight(usedArea, thickness, density);
                    
                    // 计算余料
                    var (remnantWeight, wasteWeight, hasRemnant, remnantLength, remnantWidth) = CalculateRemnantAndWaste(
                        inv, placements, wasteArea, thickness, density, minLength, minWidth, maxWasteArea);
                    
                    // 生成唯一条码（如果同一规格有多张板）
                    var usageKey = inv.Barcode ?? inv.Id.ToString();
                    if (!barcodeUsageCount.ContainsKey(usageKey))
                        barcodeUsageCount[usageKey] = 0;
                    barcodeUsageCount[usageKey]++;
                    
                    var displayBarcode = boardCount > 1 
                        ? $"{inv.Barcode}-{boardIndex + 1}" 
                        : inv.Barcode;

                    planItems.Add(new NestingPlanItem
                    {
                        InventoryId = inv.Id,
                        Barcode = displayBarcode,
                        SourceWidth = inv.Width,
                        SourceLength = inv.Length,
                        MaterialType = (int)inv.MaterialType,
                        Placements = placements,
                        Utilization = totalArea > 0 ? Math.Round(usedArea / totalArea * 100, 2) : 0,
                        WasteArea = wasteArea,
                        ProductWeight = Math.Round(productWeight, 2),
                        RemnantWeight = Math.Round(remnantWeight, 2),
                        WasteWeight = Math.Round(wasteWeight, 2),
                        HasRemnant = hasRemnant,
                        RemnantLength = remnantLength,
                        RemnantWidth = remnantWidth
                    });

                    foreach (var part in placedParts)
                        remainingParts.Remove(part);
                }
                
                // 如果当前板没有放置任何零件，跳过后续相同规格的板
                // （因为如果这块板放不下，相同规格的板也放不下）
                if (placements.Count == 0)
                    break;
            }
        }

        var totalUsedArea = planItems.Sum(p => p.Placements.Sum(x => x.Width * x.Length));
        var totalSourceArea = planItems.Sum(p => p.SourceWidth * p.SourceLength);
        var totalProductWeight = planItems.Sum(p => p.ProductWeight);
        var totalRemnantWeight = planItems.Sum(p => p.RemnantWeight);
        var totalWasteWeight = planItems.Sum(p => p.WasteWeight);
        var totalSourceWeight = totalProductWeight + totalRemnantWeight + totalWasteWeight;
        
        // 计算用时
        var elapsed = DateTime.Now - startTime;
        var calculateTime = elapsed.TotalSeconds < 1 
            ? $"{(elapsed.TotalMilliseconds):F0}ms" 
            : $"{elapsed.TotalSeconds:F2}s";

        var result = new NestingPlanDto
        {
            MaterialName = materialName,
            Thickness = thickness,
            Items = planItems,
            TotalPlates = planItems.Count,
            CompletedOrders = orders.Count - remainingParts.Select(p => p.OrderId).Distinct().Count(),
            ProductWeight = Math.Round(totalProductWeight, 2),
            RemnantWeight = Math.Round(totalRemnantWeight, 2),
            WasteWeight = Math.Round(totalWasteWeight, 2),
            WasteRate = totalSourceWeight > 0 ? Math.Round(totalWasteWeight / totalSourceWeight * 100, 2) : 0,
            TotalUtilization = totalSourceArea > 0 ? Math.Round(totalUsedArea / totalSourceArea * 100, 2) : 0,
            CalculateTime = calculateTime,
            TotalWasteArea = planItems.Sum(p => p.WasteArea),
            TotalWasteRate = totalSourceArea > 0 ? Math.Round(planItems.Sum(p => p.WasteArea) / totalSourceArea * 100, 2) : 0
        };

        return ApiResult<NestingPlanDto>.Ok(result, $"计算完成，使用{planItems.Count}块板材");
    }

    /// <summary>
    /// 零件信息
    /// </summary>
    private class PartInfo
    {
        public int OrderId { get; set; }
        public string OrderIdStr { get; set; } = string.Empty;
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public int Index { get; set; }
    }

    /// <summary>
    /// 尝试放置单个零件（优化版）
    /// </summary>
    private PlacementInfo? TryPlacePartOptimized(
        PartInfo part, Inventory inv, 
        ref decimal currentX, ref decimal currentY, ref decimal rowHeight,
        string cutDirection, bool fixedDirection, List<PlacementInfo> existingPlacements)
    {
        // 根据切割方向确定尝试顺序
        bool tryHorizontalFirst = cutDirection switch
        {
            "horizontal" => true,
            "vertical" => false,
            _ => true
        };

        // 尝试在当前行放置
        var result = TryPlaceInCurrentRowOptimized(part, inv, currentX, currentY, rowHeight, tryHorizontalFirst, fixedDirection);
        
        if (result.success)
        {
            // 更新位置
            currentX += result.width;
            rowHeight = Math.Max(rowHeight, result.length);
            
            return new PlacementInfo
            {
                OrderId = part.OrderId,
                OrderIdStr = part.OrderIdStr,
                X = result.x,
                Y = result.y,
                Width = result.width,
                Length = result.length,
                Status = "使用",
                Color = result.rotated ? "#409EFF" : "#67C23A"
            };
        }

        // 尝试换行放置
        if (rowHeight > 0)
        {
            var newY = currentY + rowHeight;
            
            // 检查新行是否有足够空间
            if (newY + Math.Max(part.Width, part.Length) <= inv.Length)
            {
                // 重置到新行起点
                currentY = newY;
                currentX = 0;
                rowHeight = 0;
                
                // 重新尝试放置
                result = TryPlaceInCurrentRowOptimized(part, inv, currentX, currentY, 0, tryHorizontalFirst, fixedDirection);
                
                if (result.success)
                {
                    currentX = result.x + result.width;
                    rowHeight = result.length;
                    
                    return new PlacementInfo
                    {
                        OrderId = part.OrderId,
                        OrderIdStr = part.OrderIdStr,
                        X = result.x,
                        Y = result.y,
                        Width = result.width,
                        Length = result.length,
                        Status = "使用",
                        Color = result.rotated ? "#409EFF" : "#67C23A"
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 尝试在当前行放置零件（优化版）
    /// 改进的旋转决策逻辑，考虑更多因素
    /// </summary>
    private (bool success, decimal x, decimal y, decimal width, decimal length, bool rotated) TryPlaceInCurrentRowOptimized(
        PartInfo part, Inventory inv, decimal currentX, decimal currentY, decimal rowHeight,
        bool tryHorizontalFirst, bool fixedDirection)
    {
        // 检查两种放置方式的可行性
        bool canPlaceNormal = currentX + part.Width <= inv.Width && currentY + part.Length <= inv.Length;
        bool canPlaceRotated = !fixedDirection && currentX + part.Length <= inv.Width && currentY + part.Width <= inv.Length;
        
        // 如果两种都可以，使用更智能的选择策略
        if (canPlaceNormal && canPlaceRotated)
        {
            // 计算两种放置方式的综合评分
            var normalScore = CalculatePlacementScore(part.Width, part.Length, currentX, currentY, rowHeight, inv);
            var rotatedScore = CalculatePlacementScore(part.Length, part.Width, currentX, currentY, rowHeight, inv);
            
            // 选择评分更高的方向
            if (rotatedScore > normalScore)
            {
                return (true, currentX, currentY, part.Length, part.Width, true);
            }
            else
            {
                return (true, currentX, currentY, part.Width, part.Length, false);
            }
        }
        
        // 只有一种可行
        if (canPlaceNormal)
        {
            return (true, currentX, currentY, part.Width, part.Length, false);
        }
        
        if (canPlaceRotated)
        {
            return (true, currentX, currentY, part.Length, part.Width, true);
        }

        return (false, 0, 0, 0, 0, false);
    }
    
    /// <summary>
    /// 计算放置位置的综合评分
    /// 考虑空间利用率、对齐度、后续放置潜力等因素
    /// </summary>
    private decimal CalculatePlacementScore(decimal width, decimal length, decimal currentX, decimal currentY, decimal rowHeight, Inventory inv)
    {
        // 1. 空间利用率评分 (0-1)
        decimal spaceUtilization = (width * length) / (inv.Width * inv.Length);
        
        // 2. 对齐度评分 (0-1) - 越靠近左下角越好
        decimal alignmentScore = (1 - (currentX / inv.Width)) * 0.6m + (1 - (currentY / inv.Length)) * 0.4m;
        
        // 3. 后续放置潜力评分 (0-1)
        decimal remainingWidth = inv.Width - (currentX + width);
        decimal remainingHeight = inv.Length - Math.Max(rowHeight, length);
        decimal potentialScore = (remainingWidth / inv.Width) * 0.5m + (remainingHeight / inv.Length) * 0.5m;
        
        // 综合评分 (加权平均)
        return spaceUtilization * 0.4m + alignmentScore * 0.3m + potentialScore * 0.3m;
    }

    /// <summary>
    /// 智能库存选择 - 根据订单需求选择最合适的板材
    /// </summary>
    private List<Inventory> SmartSelectInventory(List<Inventory> inventories, List<Order> orders, string strategy)
    {
        // 计算总需求面积
        decimal totalRequiredArea = orders.Sum(o => (o.Width * o.Length * (o.Quantity ?? 1)));
        
        // 按策略排序
        var sortedInventories = ApplySelectStrategy(inventories, strategy);
        
        // 如果是精确匹配策略，尝试找到刚好满足需求的板材组合
        if (strategy == "minMatch")
        {
            // 先按面积升序排列
            var byArea = sortedInventories.OrderBy(i => i.Width * i.Length).ToList();
            
            // 使用贪心算法选择最小的足够板材
            var selected = new List<Inventory>();
            decimal currentArea = 0;
            
            foreach (var inv in byArea)
            {
                selected.Add(inv);
                currentArea += inv.Width * inv.Length * (inv.Quantity ?? 1);
                
                if (currentArea >= totalRequiredArea)
                    break;
            }
            
            return selected;
        }
        
        return sortedInventories;
    }

    /// <summary>
    /// 根据选料策略排序库存
    /// </summary>
    private List<Inventory> ApplySelectStrategy(List<Inventory> inventories, string strategy)
    {
        return strategy switch
        {
            "minMatch" => inventories.OrderBy(i => i.Width * i.Length).ToList(),
            "remnantFirst" => inventories
                .OrderBy(i => i.MaterialType != MaterialType.Remnant)
                .ThenBy(i => i.Width * i.Length)
                .ToList(),
            "largeFirst" => inventories.OrderByDescending(i => i.Width * i.Length).ToList(),
            "smartMatch" => SmartMatchStrategy(inventories, strategy), // 新增智能匹配策略
            _ => inventories.OrderBy(i => i.Width * i.Length).ToList()
        };
    }

    /// <summary>
    /// 智能匹配策略 - 根据订单特征选择最合适的板材
    /// </summary>
    private List<Inventory> SmartMatchStrategy(List<Inventory> inventories, string strategy)
    {
        // 默认按面积升序排列
        return inventories.OrderBy(i => i.Width * i.Length).ToList();
    }

    /// <summary>
    /// 计算余料和废料重量
    /// </summary>
    private (decimal remnantWeight, decimal wasteWeight, bool hasRemnant, decimal remnantLength, decimal remnantWidth) CalculateRemnantAndWaste(
        Inventory inv, List<PlacementInfo> placements, decimal wasteArea,
        decimal thickness, decimal density, decimal minLength, decimal minWidth, decimal maxWasteArea)
    {
        if (placements.Count == 0)
        {
            return (0, 0, false, 0, 0);
        }

        // 找出已使用区域的边界
        decimal maxY = placements.Max(p => p.Y + p.Length);
        decimal maxX = placements.Max(p => p.X + p.Width);
        
        // 计算右侧剩余区域（X方向剩余）
        decimal rightRemnantWidth = inv.Width - maxX;
        decimal rightRemnantLength = maxY;
        decimal rightRemnantArea = rightRemnantWidth * rightRemnantLength;
        
        // 计算底部剩余区域（Y方向剩余）
        decimal bottomRemnantLength = inv.Length - maxY;
        decimal bottomRemnantWidth = inv.Width;
        decimal bottomRemnantArea = bottomRemnantLength * bottomRemnantWidth;
        
        // 转换最大废料面积为mm²
        decimal maxWasteAreaMm2 = maxWasteArea * 1000000m;
        
        // 优先检查底部余料（通常是更大的区域）
        bool bottomIsRemnant = bottomRemnantLength >= minLength 
                            && bottomRemnantWidth >= minWidth 
                            && bottomRemnantArea >= maxWasteAreaMm2;
        
        // 检查右侧余料
        bool rightIsRemnant = rightRemnantWidth >= minWidth 
                           && rightRemnantLength >= minLength 
                           && rightRemnantArea >= maxWasteAreaMm2;
        
        // 计算重量
        decimal CalculateWeight(decimal area) => area * thickness * density / 1000000m;
        
        if (bottomIsRemnant)
        {
            // 底部有可用余料
            decimal remnantWeight = CalculateWeight(bottomRemnantArea);
            // 废料 = 总剩余 - 余料（简化计算）
            decimal wasteWeight = CalculateWeight(wasteArea) - remnantWeight;
            return (remnantWeight, Math.Max(0, wasteWeight), true, bottomRemnantLength, bottomRemnantWidth);
        }
        else if (rightIsRemnant)
        {
            // 右侧有可用余料
            decimal remnantWeight = CalculateWeight(rightRemnantArea);
            decimal wasteWeight = CalculateWeight(wasteArea) - remnantWeight;
            return (remnantWeight, Math.Max(0, wasteWeight), true, rightRemnantLength, rightRemnantWidth);
        }
        else
        {
            // 全部视为废料
            return (0, CalculateWeight(wasteArea), false, 0, 0);
        }
    }

    /// <summary>
    /// 确认采用套料方案
    /// </summary>
    public async Task<ApiResult<bool>> ConfirmAsync(ConfirmNestingRequest request)
    {
        try
        {
            _db.Ado.BeginTran();

            foreach (var item in request.Items)
            {
                var inv = await _db.Queryable<Inventory>().InSingleAsync(item.InventoryId);
                if (inv != null)
                {
                    // 标记原库存为已使用
                    inv.Status = 1;
                    await _db.Updateable(inv).ExecuteCommandAsync();

                    // 如果有余料，生成新的余料库存记录
                    if (item.HasRemnant && item.RemnantLength > 0 && item.RemnantWidth > 0)
                    {
                        var variety = await _db.Queryable<Variety>()
                            .FirstAsync(v => v.MaterialName == request.MaterialName);
                        var density = variety?.Density ?? 2.71m;

                        var remnantArea = item.RemnantLength * item.RemnantWidth;
                        var remnantWeight = remnantArea * request.Thickness * density / 1000000m;

                        var remnantInventory = new Inventory
                        {
                            Barcode = $"{inv.Barcode}-R",
                            MaterialName = request.MaterialName,
                            MaterialType = MaterialType.Remnant,
                            Thickness = request.Thickness,
                            Length = item.RemnantLength,
                            Width = item.RemnantWidth,
                            Quantity = 1,
                            Weight = Math.Round(remnantWeight, 2),
                            Status = 0,
                            RootMaterialId = inv.Id
                        };
                        await _db.Insertable(remnantInventory).ExecuteCommandAsync();
                    }
                }

                // 记录套料历史
                foreach (var placement in item.Placements.Where(p => p.Status == "使用"))
                {
                    var variety = await _db.Queryable<Variety>()
                        .FirstAsync(v => v.MaterialName == request.MaterialName);
                    var density = variety?.Density ?? 2.71m;
                    
                    var partArea = placement.Width * placement.Length;
                    var partWeight = Math.Round(partArea * request.Thickness * density / 1000000m, 2);
                    
                    var usedList = new UsedList
                    {
                        InventoryId = item.InventoryId,
                        OrderId = placement.OrderId,
                        MaterialName = request.MaterialName,
                        Thickness = request.Thickness,
                        Width = placement.Width,
                        Length = placement.Length,
                        Weight = partWeight,
                        UseTime = DateTime.Now
                    };
                    await _db.Insertable(usedList).ExecuteCommandAsync();
                }

                // 更新订单状态为已配料
                var orderIds = item.Placements.Where(p => p.Status == "使用").Select(p => p.OrderId).Distinct().ToList();
                foreach (var orderId in orderIds)
                {
                    var order = await _db.Queryable<Order>().InSingleAsync(orderId);
                    if (order != null)
                    {
                        order.Status = 1;
                        await _db.Updateable(order).ExecuteCommandAsync();
                    }
                }
            }

            _db.Ado.CommitTran();
            return ApiResult<bool>.Ok(true, "方案已采用，余料已自动入库");
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            return ApiResult<bool>.Fail($"确认失败: {ex.Message}");
        }
    }
}