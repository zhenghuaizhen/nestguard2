using AluminumApi.Dtos;
using AluminumApi.Models;
using AluminumApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AluminumApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService) => _userService = userService;

    [HttpGet]
    public async Task<IActionResult> GetList(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var result = await _userService.GetListAsync(page, pageSize, keyword);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        var result = await _userService.CreateAsync(user);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] User user)
    {
        var result = await _userService.UpdateAsync(user);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return Ok(result);
    }
}
