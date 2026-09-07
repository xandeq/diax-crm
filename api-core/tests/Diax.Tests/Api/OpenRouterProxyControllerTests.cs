using System.Text;
using Diax.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Diax.Tests.Api;

/// <summary>
/// Testes do proxy da OpenRouter que rodam SEM rede.
///
/// O que dá para verificar offline é o contrato de borda: resolução da chave (incluindo a ordem
/// de precedência das 3 fontes de config) e o comportamento quando ela não existe. A chamada real
/// ao upstream não é testada aqui — seria não-determinística; a verificação dela é o smoke manual
/// pós-deploy, igual ao proxy da Anthropic.
/// </summary>
public class OpenRouterProxyControllerTests
{
    private static OpenRouterProxyController BuildSut(
        IConfiguration configuration,
        out HttpContext httpContext,
        string requestBody = "{\"model\":\"x\",\"messages\":[]}")
    {
        var factory = new Mock<IHttpClientFactory>();
        var sut = new OpenRouterProxyController(
            factory.Object,
            configuration,
            Mock.Of<ILogger<OpenRouterProxyController>>());

        httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        httpContext.Request.ContentType = "application/json";
        httpContext.Response.Body = new MemoryStream();

        sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return sut;
    }

    private static IConfiguration ConfigWith(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

    private static string ReadBody(HttpContext ctx)
    {
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        return new StreamReader(ctx.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task Post_SemChaveConfigurada_Retorna503EnaoChamaUpstream()
    {
        var sut = BuildSut(ConfigWith(), out var ctx);

        await sut.ProxyPostAsync("chat/completions", CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
        Assert.Contains("OpenRouter API key not configured", ReadBody(ctx));
    }

    [Fact]
    public async Task Get_SemChaveConfigurada_Retorna503()
    {
        var sut = BuildSut(ConfigWith(), out var ctx);

        await sut.ProxyGetAsync("models", CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
        Assert.Contains("OpenRouter API key not configured", ReadBody(ctx));
    }

    [Theory]
    [InlineData("OPENROUTER_API_KEY")]
    [InlineData("OpenRouter:ApiKey")]
    [InlineData("PromptGenerator:OpenRouter:ApiKey")]
    public async Task ChaveEmQualquerUmaDasTresFontes_NaoRetorna503(string configKey)
    {
        // A factory mockada devolve null em CreateClient, entao a chamada estoura ao usar o client.
        // O que este teste prova e o inverso do 503: com a chave presente, o fluxo PASSA da
        // checagem de configuracao. Falha de upstream vira 502 (tratada), nunca 503.
        var sut = BuildSut(ConfigWith((configKey, "sk-or-v1-fake")), out var ctx);

        await sut.ProxyPostAsync("chat/completions", CancellationToken.None);

        Assert.NotEqual(StatusCodes.Status503ServiceUnavailable, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task FalhaAoContactarUpstream_Retorna502ENaoVazaExcecao()
    {
        var sut = BuildSut(ConfigWith(("OPENROUTER_API_KEY", "sk-or-v1-fake")), out var ctx);

        await sut.ProxyPostAsync("chat/completions", CancellationToken.None);

        Assert.Equal(StatusCodes.Status502BadGateway, ctx.Response.StatusCode);
        Assert.Contains("Proxy error", ReadBody(ctx));
    }
}
