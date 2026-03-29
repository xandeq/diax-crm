using System.Security.Claims;
using Diax.Api.Controllers.V1;
using Diax.Application.Ai.HumanizeText;
using Diax.Application.AI;
using Diax.Domain.Auth;
using Diax.Infrastructure.Data;
using Diax.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.AI;

public class AiHumanizeTextControllerTests
{
    [Fact]
    public async Task Humanize_ReturnsBadRequest_WhenModelIsMissing()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var catalogService = new Mock<IAiCatalogService>(MockBehavior.Strict);
        var service = new Mock<IHumanizeTextService>(MockBehavior.Strict);

        var controller = CreateController(service.Object, catalogService.Object, db, user.Email);

        var result = await controller.Humanize(
            new HumanizeTextRequestDto("openai", null, "friendly", "texto"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Humanize_ReturnsForbidden_WhenUserLacksAccess()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var catalogService = new Mock<IAiCatalogService>();
        var service = new Mock<IHumanizeTextService>(MockBehavior.Strict);

        catalogService
            .Setup(x => x.ValidateUserAccessAsync(user.Id, "openai", "gpt-4o-mini", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(service.Object, catalogService.Object, db, user.Email);

        var result = await controller.Humanize(
            new HumanizeTextRequestDto("openai", "gpt-4o-mini", "friendly", "texto"),
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Humanize_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        await using var db = CreateDbContext();
        var user = await SeedUserAsync(db);
        var catalogService = new Mock<IAiCatalogService>();
        var service = new Mock<IHumanizeTextService>();

        catalogService
            .Setup(x => x.ValidateUserAccessAsync(user.Id, "openai", "gpt-4o-mini", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        service
            .Setup(x => x.HumanizeAsync(It.IsAny<HumanizeTextRequestDto>(), user.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("payload inválido"));

        var controller = CreateController(service.Object, catalogService.Object, db, user.Email);

        var result = await controller.Humanize(
            new HumanizeTextRequestDto("openai", "gpt-4o-mini", "friendly", "texto"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    private static AiHumanizeTextController CreateController(
        IHumanizeTextService service,
        IAiCatalogService catalogService,
        DiaxDbContext db,
        string email)
    {
        var controller = new AiHumanizeTextController(
            service,
            catalogService,
            db,
            NullLogger<AiHumanizeTextController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Email, email) },
                        authenticationType: "TestAuth"))
            }
        };

        return controller;
    }

    private static DiaxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DiaxDbContext(options);
    }

    private static async Task<User> SeedUserAsync(DiaxDbContext db)
    {
        var user = new User("ai-tester@diax.local", PasswordHash.HashPassword("123456"));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
