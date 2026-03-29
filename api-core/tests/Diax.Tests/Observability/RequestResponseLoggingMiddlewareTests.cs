using Diax.Api.Middleware;
using Diax.Application.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Observability;

public class RequestResponseLoggingMiddlewareTests
{
    [Theory]
    [InlineData("/api/v1/personal-control/months/2026/3", "finance")]
    [InlineData("/api/v1/customers", "customers")]
    [InlineData("/api/v1/leads", "leads")]
    [InlineData("/api/v1/email-campaigns/campaigns", "campaigns")]
    [InlineData("/api/v1/ai/management/providers", "ai")]
    [InlineData("/api/v1/logs", "logs")]
    [InlineData("/api/v1/unknown", "core")]
    public void ResolveModule_MapsRouteToExpectedModule(string path, string expectedModule)
    {
        var module = RequestResponseLoggingMiddleware.ResolveModule(path);

        Assert.Equal(expectedModule, module);
    }

    [Fact]
    public async Task InvokeAsync_AddsResponseHeaders_BeforeResponseStarts()
    {
        var appLogService = new Mock<IAppLogService>(MockBehavior.Strict);
        var middleware = new RequestResponseLoggingMiddleware(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("ok");
            },
            NullLogger<RequestResponseLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/ai/generate-image";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, appLogService.Object);

        Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
        Assert.Equal("ai", context.Response.Headers["X-App-Module"].ToString());
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        Assert.Equal("ok", await reader.ReadToEndAsync());
    }
}
