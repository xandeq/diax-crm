using Diax.Application.Auth;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Diax.Api.Auth;

/// <summary>
/// Exige que o usuário seja admin AGORA (grupo system-admin consultado no banco a cada request).
/// O claim Role do JWT é uma foto do login e vale até o token expirar; quem sai do grupo
/// continuaria passando em [Authorize(Roles = "Admin")]. Use junto, nunca no lugar de [Authorize].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireAdminAttribute : TypeFilterAttribute
{
    public RequireAdminAttribute() : base(typeof(RequireAdminFilter)) { }
}

public sealed class RequireAdminFilter : IAsyncAuthorizationFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public RequireAdminFilter(ICurrentUserService currentUserService, IPermissionService permissionService)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var userId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : null;
        if (!userId.HasValue)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!await _permissionService.IsAdminAsync(userId.Value, context.HttpContext.RequestAborted))
            context.Result = new ForbidResult();
    }
}
