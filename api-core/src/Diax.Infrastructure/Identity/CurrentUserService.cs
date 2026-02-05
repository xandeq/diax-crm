using System.Security.Claims;
using Diax.Domain.Common;
using Diax.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;

namespace Diax.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private Guid? _userId;
    private bool _userIdResolved;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public Guid? UserId
    {
        get
        {
            if (_userIdResolved) return _userId;

            _userId = ResolveUserId();
            _userIdResolved = true;
            return _userId;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    private Guid? ResolveUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        // We use IServiceScopeFactory here to create a short-lived DbContext
        // specifically to resolve the user ID, avoiding circular dependency
        // with DiaxDbContext which depends on ICurrentUserService.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiaxDbContext>();

        // Use IgnoreQueryFilters to avoid reaching for CurrentUserService during this internal lookup
        var adminUser = db.AdminUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefault(u => u.Email == email);

        return adminUser?.Id;
    }
}
