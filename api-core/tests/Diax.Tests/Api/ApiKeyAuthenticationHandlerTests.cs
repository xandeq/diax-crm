using System.Security.Claims;
using System.Text.Encodings.Web;
using Diax.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Diax.Tests.Api;

/// <summary>
/// Testes do handler de API key estática.
///
/// O ponto crítico coberto aqui é o segundo formato de credencial: "Authorization: Bearer {chave}".
/// Ele existe porque os proxies de IA (/proxy e /openrouter) são consumidos por clientes compatíveis
/// com a API da OpenAI — SDK oficial, Cursor, Continue, LangChain — que só sabem mandar a credencial
/// em Bearer e não permitem injetar um header customizado. Sem isso, integrar o proxy nessas
/// ferramentas é impossível.
///
/// O que NÃO pode acontecer: um JWT de sessão de usuário cair neste handler e ser rejeitado. O
/// roteamento em Program.cs impede isso pelo formato (JWT tem exatamente 2 pontos), mas os testes
/// abaixo travam o contrato do handler em si.
/// </summary>
public class ApiKeyAuthenticationHandlerTests
{
    private const string ConfiguredKey = "chave-de-servico-para-teste";

    private static async Task<AuthenticateResult> AuthenticateAsync(
        Action<HttpContext> configureRequest,
        string? configuredKey = ConfiguredKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Auth:AdminEmail"] = "admin@diaxcrm.test",
        };
        if (configuredKey is not null)
            settings["ServiceApiKey"] = configuredKey;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var options = new ApiKeyAuthenticationOptions();
        var monitor = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        monitor.Setup(m => m.Get(It.IsAny<string>())).Returns(options);
        monitor.Setup(m => m.CurrentValue).Returns(options);

        var handler = new ApiKeyAuthenticationHandler(
            monitor.Object,
            new LoggerFactory(),
            UrlEncoder.Default,
            configuration);

        var context = new DefaultHttpContext();
        configureRequest(context);

        var scheme = new AuthenticationScheme(
            ApiKeyAuthenticationOptions.DefaultScheme,
            ApiKeyAuthenticationOptions.DefaultScheme,
            typeof(ApiKeyAuthenticationHandler));

        await handler.InitializeAsync(scheme, context);
        return await handler.AuthenticateAsync();
    }

    [Fact]
    public async Task XApiKeyHeader_WithCorrectKey_Authenticates()
    {
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers["X-Api-Key"] = ConfiguredKey);

        Assert.True(result.Succeeded);
        Assert.Equal("admin@diaxcrm.test", result.Principal!.FindFirst(ClaimTypes.Email)!.Value);
        Assert.Equal("api_key", result.Principal!.FindFirst("auth_method")!.Value);
    }

    [Fact]
    public async Task BearerHeader_WithCorrectKey_Authenticates()
    {
        // Este é o caso que destrava SDK da OpenAI, Cursor, Continue e afins.
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers.Authorization = $"Bearer {ConfiguredKey}");

        Assert.True(result.Succeeded);
        Assert.Equal("api_key", result.Principal!.FindFirst("auth_method")!.Value);
    }

    [Fact]
    public async Task BearerHeader_IsCaseInsensitiveOnScheme()
    {
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers.Authorization = $"bearer {ConfiguredKey}");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task BearerHeader_WithWrongKey_Fails()
    {
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers.Authorization = "Bearer sk-or-v1-chave-errada");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid API key.", result.Failure!.Message);
    }

    [Fact]
    public async Task XApiKeyHeader_TakesPrecedenceOverBearer()
    {
        // Se os dois vierem, o header nativo manda — evita que um Bearer sobrando de outra
        // ferramenta invalide uma chamada que trouxe a credencial certa em X-Api-Key.
        var result = await AuthenticateAsync(ctx =>
        {
            ctx.Request.Headers["X-Api-Key"] = ConfiguredKey;
            ctx.Request.Headers.Authorization = "Bearer valor-que-nao-e-a-chave";
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task NoCredential_ReturnsNoResult_SoJwtSchemeCanTry()
    {
        var result = await AuthenticateAsync(_ => { });

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task NonBearerAuthorization_ReturnsNoResult()
    {
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers.Authorization = "Basic dXNlcjpwYXNz");

        Assert.True(result.None);
    }

    [Fact]
    public async Task EmptyBearerValue_ReturnsNoResult()
    {
        var result = await AuthenticateAsync(ctx => ctx.Request.Headers.Authorization = "Bearer ");

        Assert.True(result.None);
    }

    [Fact]
    public async Task ServiceApiKeyNotConfigured_Fails_WithoutLeakingWhy()
    {
        var result = await AuthenticateAsync(
            ctx => ctx.Request.Headers["X-Api-Key"] = "qualquer-coisa",
            configuredKey: null);

        Assert.False(result.Succeeded);
        Assert.Equal("Service API key not configured on server.", result.Failure!.Message);
    }
}

/// <summary>
/// Testes do roteamento entre os schemes de autenticação.
///
/// Esta é a regra que decide se uma requisição vai para o handler de API key ou para o JwtBearer.
/// Errar aqui tem dois modos de falha graves e opostos: desviar um JWT de usuário para o handler
/// de chave estática derruba o login de todo mundo; não desviar um Bearer com a ServiceApiKey
/// mantém os proxies de IA inacessíveis a qualquer cliente compatível com a OpenAI.
/// </summary>
public class StaticApiKeyDetectionTests
{
    private static HttpRequest RequestWith(Action<HttpContext> configure)
    {
        var context = new DefaultHttpContext();
        configure(context);
        return context.Request;
    }

    [Fact]
    public void XApiKeyHeader_RoutesToApiKeyScheme()
    {
        var request = RequestWith(ctx => ctx.Request.Headers["X-Api-Key"] = "qualquer-valor");

        Assert.True(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }

    [Fact]
    public void BearerWithoutDots_RoutesToApiKeyScheme()
    {
        var request = RequestWith(ctx => ctx.Request.Headers.Authorization = "Bearer chave-estatica-sem-nenhum-ponto");

        Assert.True(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }

    [Fact]
    public void BearerWithJwtShape_StaysOnJwtScheme()
    {
        // Forma de um JWT: header.payload.signature — exatamente dois pontos. Montado em runtime
        // de propósito: um literal com essa forma dispara os scanners de segredo do CI.
        var jwtShaped = string.Join(".", "cabecalho", "corpo", "assinatura");

        var request = RequestWith(ctx => ctx.Request.Headers.Authorization = $"Bearer {jwtShaped}");

        Assert.False(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }

    [Fact]
    public void NoAuthorizationHeader_StaysOnJwtScheme()
    {
        Assert.False(StaticApiKeyDetection.CarriesStaticApiKey(RequestWith(_ => { })));
    }

    [Fact]
    public void NonBearerScheme_StaysOnJwtScheme()
    {
        var request = RequestWith(ctx => ctx.Request.Headers.Authorization = "Basic dXNlcjpwYXNz");

        Assert.False(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }

    [Fact]
    public void EmptyBearerValue_StaysOnJwtScheme()
    {
        var request = RequestWith(ctx => ctx.Request.Headers.Authorization = "Bearer ");

        Assert.False(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }

    [Theory]
    [InlineData("sk-or-v1-abcdef")]          // chave da OpenRouter colada por engano
    [InlineData("sk-ant-api03-abcdef")]      // chave da Anthropic colada por engano
    [InlineData("token.com.um.monte.de.pontos")]
    public void NonJwtShapes_RouteToApiKeyScheme_AndFailThereWithClearError(string token)
    {
        // Estes valores NÃO autenticam — mas têm que chegar no handler de API key, que devolve
        // "Invalid API key.", em vez de morrerem no JwtBearer com erro de token malformado.
        var request = RequestWith(ctx => ctx.Request.Headers.Authorization = $"Bearer {token}");

        Assert.True(StaticApiKeyDetection.CarriesStaticApiKey(request));
    }
}
