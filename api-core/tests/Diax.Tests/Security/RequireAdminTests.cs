using System.Reflection;
using Diax.Api.Auth;
using Diax.Api.Controllers.V1;
using Diax.Application.Auth;
using Diax.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Diax.Tests.Security;

/// <summary>
/// O claim Role do JWT é foto do login (token longo, sem revogação). Toda rota de admin precisa
/// também da checagem ao vivo [RequireAdmin], senão quem sai do grupo system-admin continua admin.
/// </summary>
public class RequireAdminTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static async Task<IActionResult?> Run(bool authenticated, Guid? userId, bool isAdmin)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.IsAuthenticated).Returns(authenticated);
        current.SetupGet(x => x.UserId).Returns(userId);
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.IsAdminAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(isAdmin);

        var ctx = new AuthorizationFilterContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
        await new RequireAdminFilter(current.Object, permissions.Object).OnAuthorizationAsync(ctx);
        return ctx.Result;
    }

    [Fact]
    public async Task AdminNoBanco_Passa() => Assert.Null(await Run(true, UserId, isAdmin: true));

    [Fact]
    public async Task ClaimAntigoMasForaDoGrupo_Forbid() =>
        Assert.IsType<ForbidResult>(await Run(true, UserId, isAdmin: false));

    [Fact]
    public async Task SemUsuario_Unauthorized()
    {
        Assert.IsType<UnauthorizedResult>(await Run(false, null, isAdmin: true));
        Assert.IsType<UnauthorizedResult>(await Run(true, null, isAdmin: true));
    }

    private static bool HasRoleAdmin(MemberInfo m) =>
        m.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any(a => a.Roles?.Split(',').Contains("Admin") == true);

    private static bool HasRequireAdmin(MemberInfo m) => m.GetCustomAttributes<RequireAdminAttribute>(inherit: true).Any();

    [Fact]
    public void TodaRotaComRoleAdmin_TemTambemRequireAdmin()
    {
        var faltando = new List<string>();
        foreach (var type in typeof(AuthController).Assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var classRole = HasRoleAdmin(type);
            var classLive = HasRequireAdmin(type);
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!m.GetCustomAttributes<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>().Any()) continue;
                if ((classRole || HasRoleAdmin(m)) && !(classLive || HasRequireAdmin(m)))
                    faltando.Add($"{type.Name}.{m.Name}");
            }
        }
        Assert.Empty(faltando);
    }

    [Theory]
    [InlineData(typeof(HealthController), nameof(HealthController.SchemaPatch))]
    [InlineData(typeof(EmailImagesController), nameof(EmailImagesController.DeleteImage))]
    public void RotasSensiveis_ExigemAdminAoVivo(Type controller, string action)
    {
        var m = controller.GetMethod(action)!;
        Assert.True(HasRequireAdmin(m) || HasRequireAdmin(controller), $"{controller.Name}.{action} sem [RequireAdmin]");
    }
}
