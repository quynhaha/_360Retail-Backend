using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Identity.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

namespace _360Retail.Services.Identity.API.Controllers.SuperAdmin;

[ApiController]
[Route("api/super-admin/users")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminUsersController : ControllerBase
{
    private readonly ISuperAdminUserService _service;

    public SuperAdminUsersController(ISuperAdminUserService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
