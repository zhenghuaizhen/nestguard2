using AluminumApi.Dtos;
using AluminumApi.Models;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VarietyController : ControllerBase
{
    private readonly VarietyService _varietyService;

    public VarietyController(VarietyService varietyService) => _varietyService = varietyService;

    [HttpGet]
    public async Task<IActionResult> GetList(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var result = await _varietyService.GetListAsync(page, pageSize, keyword);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _varietyService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Variety item)
    {
        var result = await _varietyService.CreateAsync(item);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Variety item)
    {
        var result = await _varietyService.UpdateAsync(item);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _varietyService.DeleteAsync(id);
        return Ok(result);
    }

    [HttpPost("batch-delete")]
    public async Task<IActionResult> BatchDelete([FromBody] List<int> ids)
    {
        var result = await _varietyService.BatchDeleteAsync(ids);
        return Ok(result);
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var items = await _varietyService.GetAllAsync();
        return Ok(ApiResult<List<Variety>>.Ok(items));
    }
}
