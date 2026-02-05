using Asp.Versioning;
using Diax.Application.Auth;
using Diax.Application.Auth.Dtos;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _service;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public UsersController(
        IUserManagementService service,
        ICurrentUserService currentUserService,
        IPermissionService permissionService)
    {
        _service = service;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (!await _permissionService.HasPermissionAsync(userId, "users.manage", ct))
            return Forbid();

        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (!await _permissionService.HasPermissionAsync(userId, "users.manage", ct))
            return Forbid();

        return Ok(await _service.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (!await _permissionService.HasPermissionAsync(userId, "users.manage", ct))
            return Forbid();

        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { message = "O corpo da requisição não pode estar vazio." });

        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (!await _permissionService.HasPermissionAsync(currentUserId, "users.manage", ct))
            return Forbid();

        return Ok(await _service.UpdateAsync(id, request, currentUserId, ct));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var currentUserId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        if (!await _permissionService.HasPermissionAsync(currentUserId, "users.manage", ct))
            return Forbid();

        await _service.DeleteAsync(id, currentUserId, ct);
        return NoContent();
    }

    /// <summary>
    /// Endpoint para o frontend obter informações do usuário logado.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
        return Ok(await _service.GetByIdAsync(userId, ct));
    }
}
