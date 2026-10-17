using Diax.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Diax.Tests.Security;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public void Apply_Production_SetsAllHeadersIncludingCsp()
    {
        var headers = new DefaultHttpContext().Response.Headers;

        SecurityHeadersMiddleware.Apply(headers, strictCsp: true);

        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", headers["Referrer-Policy"]);
        Assert.Contains("frame-ancestors 'none'", headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public void Apply_NonProduction_SkipsCspSoSwaggerUiWorks()
    {
        var headers = new DefaultHttpContext().Response.Headers;

        SecurityHeadersMiddleware.Apply(headers, strictCsp: false);

        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.False(headers.ContainsKey("Content-Security-Policy"));
    }
}
