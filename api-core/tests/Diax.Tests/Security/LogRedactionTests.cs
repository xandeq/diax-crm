using Diax.Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Diax.Tests.Security;

// 21/10: o filtro antigo bloqueava só 5 nomes; X-Integration-Key & cia iam em texto puro para app_logs.
public class LogRedactionTests
{
    [Theory]
    [InlineData("X-Integration-Key")]
    [InlineData("X-Alert-Api-Key")]
    [InlineData("X-Log-Api-Key")]
    [InlineData("X-Cron-Key")]
    [InlineData("X-Mailjet-Token")]
    [InlineData("X-Brevo-Signature")]
    [InlineData("svix-signature")]
    [InlineData("X-Twilio-Signature")]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("X-Qualquer-Header-Novo")]
    public void SafeHeaders_DropsAnythingNotAllowListed(string header)
    {
        var headers = new HeaderDictionary { [header] = "segredo-123", ["User-Agent"] = "curl/8" };

        var safe = LogRedaction.SafeHeaders(headers);

        Assert.False(safe.ContainsKey(header));
        Assert.DoesNotContain("segredo-123", string.Join("|", safe.Values));
        Assert.Equal("curl/8", safe["User-Agent"]);
    }

    [Theory]
    [InlineData("?secret=abc&event=x", "?secret=***&event=x")]
    [InlineData("?event=x&api_key=abc", "?event=x&api_key=***")]
    [InlineData("?token=AbC-_9&uid=1&email=a%40b.com", "?token=***&uid=1&email=a%40b.com")]
    [InlineData("?page=2&search=clinica", "?page=2&search=clinica")]
    [InlineData("", "")]
    public void SafeQuery_MasksSecretLookingParams(string input, string expected)
    {
        Assert.Equal(expected, LogRedaction.SafeQuery(input));
    }
}
