using Asp.Versioning;
using Diax.Application.Auth;
using Diax.Application.Auth.Dtos;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _service;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserManagementService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await _service.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { message = "O corpo da requisição não pode estar vazio." });

        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        return Ok(await _service.UpdateAsync(id, request, currentUserId, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        await _service.DeleteAsync(id, currentUserId, ct);
        return NoContent();
    }
}
