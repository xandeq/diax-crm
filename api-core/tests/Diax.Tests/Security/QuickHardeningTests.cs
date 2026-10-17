using Diax.Api.Controllers.V1;
using Diax.Infrastructure.EmailImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Security;

public class QuickHardeningTests
{
    [Fact]
    public void ChecklistsController_RequiresAuthentication()
    {
        var hasAuthorize = typeof(ChecklistsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Length > 0;

        Assert.True(hasAuthorize);
    }

    [Theory]
    [InlineData("..\\..\\appsettings")]
    [InlineData("../../web")]
    [InlineData("not-a-guid")]
    public async Task DeleteImageAsync_WithNonGuidId_IsRejectedBeforeTouchingDisk(string imageId)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "diax-img-test-" + Guid.NewGuid().ToString("N"));
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.WebRootPath).Returns(tempRoot);
        env.SetupGet(e => e.ContentRootPath).Returns(tempRoot);

        var svc = new EmailImageStorageService(
            env.Object, new Mock<IHttpContextAccessor>().Object,
            NullLogger<EmailImageStorageService>.Instance);

        var result = await svc.DeleteImageAsync(imageId);

        Assert.False(result.IsSuccess);
        Assert.Contains("Validation", result.Error.Code);
    }
}
