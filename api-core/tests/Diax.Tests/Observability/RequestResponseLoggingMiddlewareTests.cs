using Diax.Api.Middleware;

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
}
