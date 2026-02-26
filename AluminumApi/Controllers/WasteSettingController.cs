using AluminumApi.Models;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WasteSettingController : ControllerBase
{
    private readonly WasteSettingService _wasteService;

    public WasteSettingController(WasteSettingService wasteService) => _wasteService = wasteService;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _wasteService.GetAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] WasteSetting setting)
    {
        var result = await _wasteService.SaveAsync(setting);
        return Ok(result);
    }
}
