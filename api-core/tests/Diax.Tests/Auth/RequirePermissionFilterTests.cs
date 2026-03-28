using Diax.Api.Auth;
using Diax.Application.Auth;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Diax.Tests.Auth;

public class RequirePermissionFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_ReturnsUnauthorized_WhenUserIsNotAuthenticated()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);

        var permissionService = new Mock<IPermissionService>(MockBehavior.Strict);
        var filter = new RequirePermissionFilter("users.manage", currentUser.Object, permissionService.Object);
        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_ReturnsForbid_WhenPermissionIsMissing()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.HasPermissionAsync(It.IsAny<Guid>(), "users.manage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var filter = new RequirePermissionFilter("users.manage", currentUser.Object, permissionService.Object);
        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_AllowsRequest_WhenPermissionExists()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.HasPermissionAsync(It.IsAny<Guid>(), "users.manage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var filter = new RequirePermissionFilter("users.manage", currentUser.Object, permissionService.Object);
        var context = CreateContext();

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
