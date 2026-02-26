using AluminumApi.Dtos;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NestingController : ControllerBase
{
    private readonly NestingService _nestingService;

    public NestingController(NestingService nestingService) => _nestingService = nestingService;

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] NestingRequest request)
    {
        var result = await _nestingService.CalculateAsync(request);
        return Ok(result);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmNestingRequest request)
    {
        var result = await _nestingService.ConfirmAsync(request);
        return Ok(result);
    }

    [HttpGet("available-orders")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var result = await _nestingService.GetAvailableOrdersAsync();
        return Ok(result);
    }
}
