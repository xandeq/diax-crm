using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Diax.Api.Auth;

/// <summary>
/// Decide se uma requisição carrega a ServiceApiKey estática, para o PolicyScheme de autenticação
/// rotear entre o handler de API key e o de JWT.
/// </summary>
public static class StaticApiKeyDetection
{
    /// <summary>
    /// Verdadeiro quando a requisição traz a chave estática — seja no header nativo
    /// <c>X-Api-Key</c>, seja em <c>Authorization: Bearer {chave}</c>.
    ///
    /// O segundo formato existe para clientes compatíveis com a API da OpenAI (SDK oficial,
    /// Cursor, Continue, LangChain), que só mandam credencial em Bearer e não permitem header
    /// customizado. Distinguir de um JWT é inequívoco: um JWT é sempre
    /// <c>header.payload.signature</c>, ou seja, exatamente dois pontos. Um Bearer com essa forma
    /// nunca é desviado para o handler de API key — continua indo para o JwtBearer.
    /// </summary>
    public static bool CarriesStaticApiKey(HttpRequest request)
    {
        if (request.Headers.ContainsKey("X-Api-Key"))
            return true;

        var authHeader = request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Bearer ".Length..].Trim();
        return token.Length > 0 && token.Count(c => c == '.') != 2;
    }
}

/// <summary>
/// Opções para autenticação por API Key estática.
/// Lida com chamadas machine-to-machine (ex: n8n workflows) que não podem
/// usar tokens JWT de curta duração.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";

    /// <summary>
    /// Role atribuída a quem entra com a <c>ProxyApiKey</c>. É deliberadamente estéril: serve só
    /// para a policy dos proxies de IA reconhecê-la, e a policy padrão recusá-la em todo o resto.
    /// </summary>
    public const string ProxyOnlyRole = "ProxyClient";

    /// <summary>Nome da policy que os controllers de proxy usam.</summary>
    public const string ProxyPolicy = "ProxyAccess";

    /// <summary>Nome do header HTTP onde a chave será lida.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";
}

/// <summary>
/// Handler de autenticação por API Key estática.
///
/// Uso: enviar o header <c>X-Api-Key: {chave}</c> em vez de <c>Authorization: Bearer {jwt}</c>.
/// A chave é configurada via <c>ServiceApiKey</c> em appsettings / AWS SM / env var DIAX_ServiceApiKey.
///
/// Usado principalmente por workflows n8n (scheduler) que chamam endpoints como
/// POST /outreach/segment, POST /outreach/send e POST /customers/import.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Lê a chave. Dois formatos são aceitos:
        //    a) X-Api-Key: {chave}          — formato nativo, usado pelos workflows n8n
        //    b) Authorization: Bearer {chave} — para clientes compatíveis com a API da OpenAI
        //       (SDK oficial, Cursor, Continue, LangChain) que só mandam credencial em Bearer e
        //       não permitem header customizado. O PolicyScheme em Program.cs só desvia para cá
        //       um Bearer que NÃO tem formato de JWT, então não há colisão com sessões de usuário.
        var providedKey = ReadProvidedKey();
        if (string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult()); // Deixa o próximo scheme tentar

        // 2. Lê as duas chaves configuradas no servidor.
        //    ServiceApiKey  → acesso de serviço COMPLETO (Admin). É a que os workflows n8n usam.
        //    ProxyApiKey    → acesso EXCLUSIVO aos proxies de IA (/proxy e /openrouter).
        //
        //    A separação existe porque a ServiceApiKey autentica como Admin: quem a tiver lê e
        //    escreve clientes, leads, usuários e logs de auditoria. Distribuí-la para uma máquina
        //    de trabalho ou uma ferramenta de terceiros só para consumir os proxies entregaria o
        //    CRM inteiro junto. A ProxyApiKey é descartável e não abre mais nada.
        var serviceKey = _configuration["ServiceApiKey"];
        var proxyKey = _configuration["ProxyApiKey"];

        if (string.IsNullOrWhiteSpace(serviceKey) && string.IsNullOrWhiteSpace(proxyKey))
        {
            Logger.LogWarning("ApiKey auth: credencial estatica recebida, mas nem 'ServiceApiKey' nem 'ProxyApiKey' estao configurados no servidor.");
            return Task.FromResult(AuthenticateResult.Fail("Service API key not configured on server."));
        }

        // 3. Compara em tempo constante (previne timing attacks). Faz hash de ambos para
        //    normalizar o comprimento antes de comparar. As duas comparações são sempre
        //    executadas para não vazar por tempo qual das chaves casou.
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));
        var matchedService = MatchesConfiguredKey(providedHash, serviceKey);
        var matchedProxy = MatchesConfiguredKey(providedHash, proxyKey);

        if (!matchedService && !matchedProxy)
        {
            Logger.LogWarning("ApiKey auth: chave inválida recebida de {IP}", Request.HttpContext.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        // 4. Monta a identidade. A ServiceApiKey vence quando as duas casam — situação que só
        //    aconteceria se alguém configurasse o mesmo valor nas duas, o que é erro de operação.
        var serviceEmail = _configuration["Auth:AdminEmail"] ?? "service@diaxcrm.internal";

        var claims = matchedService
            ? new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, serviceEmail),
                new Claim(JwtRegisteredClaimNames.Email, serviceEmail),
                new Claim(ClaimTypes.Email, serviceEmail),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("auth_method", "api_key"),
            }
            : new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "proxy@diaxcrm.internal"),
                new Claim(JwtRegisteredClaimNames.Email, "proxy@diaxcrm.internal"),
                new Claim(ClaimTypes.Email, "proxy@diaxcrm.internal"),
                new Claim(ClaimTypes.Role, ApiKeyAuthenticationOptions.ProxyOnlyRole),
                new Claim("auth_method", "proxy_key"),
            };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug("ApiKey auth: acesso de serviço autenticado para {Email}", serviceEmail);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Compara em tempo constante o hash recebido contra o hash de uma chave configurada.
    /// Retorna false quando a chave não está configurada, sem curto-circuitar antes do hash.
    /// </summary>
    private static bool MatchesConfiguredKey(byte[] providedHash, string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(configuredKey))
            return false;

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }

    /// <summary>
    /// Extrai a chave estática de <c>X-Api-Key</c> ou, na falta dele, de
    /// <c>Authorization: Bearer {chave}</c>. Retorna null quando nenhum dos dois traz valor.
    /// </summary>
    private string? ReadProvidedKey()
    {
        if (Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            var fromHeader = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fromHeader))
                return fromHeader;
        }

        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (token.Length > 0)
                return token;
        }

        return null;
    }
}
