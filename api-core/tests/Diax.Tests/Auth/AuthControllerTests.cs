using Diax.Api.Controllers.V1;
using Diax.Domain.Auth;
using Diax.Infrastructure.Data;
using Diax.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Diax.Tests.Auth;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User("tester@diax.local", PasswordHash.HashPassword("correct-password")));
        await db.SaveChangesAsync();

        var controller = new AuthController(CreateConfiguration(), db, new FakeEnvironment("Production"));

        var result = await controller.Login(new AuthController.LoginRequest("tester@diax.local", "wrong-password"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenDatabaseUserIsValid()
    {
        await using var db = CreateDbContext();
        db.Users.Add(new User("tester@diax.local", PasswordHash.HashPassword("correct-password")));
        await db.SaveChangesAsync();

        var controller = new AuthController(CreateConfiguration(), db, new FakeEnvironment("Production"));

        var result = await controller.Login(new AuthController.LoginRequest("tester@diax.local", "correct-password"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    }

    [Fact]
    public async Task Login_UsesConfigFallback_WhenDatabaseIsEmpty()
    {
        await using var db = CreateDbContext();
        var controller = new AuthController(CreateConfiguration(enableBootstrapAdminLogin: true), db, new FakeEnvironment("Production"));

        var result = await controller.Login(new AuthController.LoginRequest("admin@diax.local", "admin123"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
    }

    [Fact]
    public async Task Login_DoesNotUseConfigFallback_WhenBootstrapLoginIsDisabled()
    {
        await using var db = CreateDbContext();
        var controller = new AuthController(CreateConfiguration(enableBootstrapAdminLogin: false), db, new FakeEnvironment("Production"));

        var result = await controller.Login(new AuthController.LoginRequest("admin@diax.local", "admin123"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static IConfiguration CreateConfiguration(bool enableBootstrapAdminLogin = false)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "DiaxCRM",
                ["Jwt:Audience"] = "DiaxCRM",
                ["Jwt:Key"] = "super-secret-test-key-with-32chars!",
                ["Auth:AdminEmail"] = "admin@diax.local",
                ["Auth:AdminPassword"] = "admin123",
                ["Auth:EnableBootstrapAdminLogin"] = enableBootstrapAdminLogin.ToString(),
            })
            .Build();
    }

    private static DiaxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DiaxDbContext(options);
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public FakeEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "Diax.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
