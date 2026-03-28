using Diax.Application.Auth;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Diax.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permissionKey)
        : base(typeof(RequirePermissionFilter))
    {
        Arguments = new object[] { permissionKey };
    }
}

public sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _permissionKey;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;

    public RequirePermissionFilter(
        string permissionKey,
        ICurrentUserService currentUserService,
        IPermissionService permissionService)
    {
        _permissionKey = permissionKey;
        _currentUserService = currentUserService;
        _permissionService = permissionService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!await _permissionService.HasPermissionAsync(userId.Value, _permissionKey, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
        }
    }
}
