using AluminumApi.Dtos;
using AluminumApi.Models;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService) => _inventoryService = inventoryService;

    [HttpGet]
    public async Task<IActionResult> GetList(
        int page = 1, 
        int pageSize = 20, 
        string? keyword = null,
        string? materialName = null,
        int? status = null,
        string? spec = null,
        int? materialType = null)
    {
        var result = await _inventoryService.GetListAsync(page, pageSize, keyword, materialName, status, spec, materialType);
        return Ok(result);
    }

    [HttpGet("column-values/{column}")]
    public async Task<IActionResult> GetColumnValues(string column)
    {
        var result = await _inventoryService.GetColumnValuesAsync(column);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _inventoryService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Inventory item)
    {
        var result = await _inventoryService.CreateAsync(item);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Inventory item)
    {
        var result = await _inventoryService.UpdateAsync(item);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _inventoryService.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResult<bool>.Fail("请选择文件"));

        using var stream = file.OpenReadStream();
        var rows = stream.Query<Inventory>(excelType: ExcelType.XLSX).ToList();
        var result = await _inventoryService.ImportAsync(rows);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var items = await _inventoryService.GetAllAsync();
        var stream = new MemoryStream();
        await stream.SaveAsAsync(items);
        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory.xlsx");
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var bytes = _inventoryService.GetTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory_template.xlsx");
    }
}
