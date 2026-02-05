using Asp.Versioning;
using Diax.Domain.Auth;
using Diax.Infrastructure.Data;
using Diax.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diax.Api.Controllers.V1;

[Authorize(Roles = "Admin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DiaxDbContext _db;

    public UsersController(DiaxDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _db.AdminUsers
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserResponse(u.Id, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var emailExists = await _db.AdminUsers
            .AnyAsync(u => u.Email == request.Email.Trim(), cancellationToken);

        if (emailExists)
            return Conflict(new { message = "A user with this email already exists." });

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return BadRequest(new { message = "Invalid role. Allowed values: User, Admin." });

        var passwordHash = PasswordHash.HashPassword(request.Password);
        var user = new AdminUser(request.Email, passwordHash, role: role);

        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new UserResponse(user.Id, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.AdminUsers
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
            return NotFound(new { message = "User not found." });

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _db.AdminUsers
                .CountAsync(u => u.Role == UserRole.Admin, cancellationToken);

            if (adminCount <= 1)
                return BadRequest(new { message = "Cannot delete the last administrator." });
        }

        _db.AdminUsers.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    public record CreateUserRequest(string Email, string Password, string Role = "User");

    public record UserResponse(Guid Id, string Email, string Role, bool IsActive, DateTime CreatedAt);
}
