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
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService) => _orderService = orderService;

    [HttpGet]
    public async Task<IActionResult> GetList(int page = 1, int pageSize = 20, string? keyword = null, string? customer = null, int? status = null)
    {
        var result = await _orderService.GetListAsync(page, pageSize, keyword, customer, status);
        return Ok(result);
    }

    [HttpGet("column-values/{column}")]
    public async Task<IActionResult> GetColumnValues(string column)
    {
        var result = await _orderService.GetColumnValuesAsync(column);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orderService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order)
    {
        var result = await _orderService.CreateAsync(order);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Order order)
    {
        var result = await _orderService.UpdateAsync(order);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _orderService.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] List<int> ids)
    {
        var result = await _orderService.BatchDeleteAsync(ids);
        return Ok(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Ok(ApiResult<bool>.Fail("请选择文件"));

        using var stream = file.OpenReadStream();
        var rows = stream.Query<Order>(excelType: ExcelType.XLSX).ToList();
        var result = await _orderService.ImportAsync(rows);
        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var orders = await _orderService.GetAllAsync();
        var stream = new MemoryStream();
        await stream.SaveAsAsync(orders);
        stream.Position = 0;
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "orders.xlsx");
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var bytes = _orderService.GetTemplate();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "order_template.xlsx");
    }
}
