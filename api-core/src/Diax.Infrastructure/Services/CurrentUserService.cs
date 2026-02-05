using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Diax.Application.Common;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Services;

/// <summary>
/// Provides access to the currently authenticated user's information
/// by resolving the user from JWT claims and the database.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly DiaxDbContext _db;

    private bool _isResolved;
    private Guid? _userId;
    private string? _email;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        DiaxDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public Guid? UserId
    {
        get
        {
            ResolveUserIfNeeded();
            return _userId;
        }
    }

    public string? Email
    {
        get
        {
            ResolveUserIfNeeded();
            return _email;
        }
    }

    public bool IsAuthenticated => UserId.HasValue;

    private void ResolveUserIfNeeded()
    {
        if (_isResolved)
            return;

        _isResolved = true;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        // Extract email from JWT claims (supports multiple claim types for robustness)
        _email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(_email))
            return;

        // Resolve UserId from AdminUsers table by email
        // Using synchronous query to avoid async in property getter
        // This is cached per request (scoped lifetime)
        var adminUser = _db.AdminUsers
            .AsNoTracking()
            .FirstOrDefault(u => u.Email == _email);

        _userId = adminUser?.Id;
    }
}
