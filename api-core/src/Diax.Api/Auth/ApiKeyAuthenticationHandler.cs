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

        // 2. Lê a chave configurada no servidor
        var configuredKey = _configuration["ServiceApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            Logger.LogWarning("ApiKey auth: credencial estatica recebida, mas 'ServiceApiKey' nao esta configurado no servidor.");
            return Task.FromResult(AuthenticateResult.Fail("Service API key not configured on server."));
        }

        // 3. Compara em tempo constante (previne timing attacks)
        //    Faz hash de ambos para normalizar o comprimento antes de comparar.
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedKey));

        if (!CryptographicOperations.FixedTimeEquals(providedHash, expectedHash))
        {
            Logger.LogWarning("ApiKey auth: chave inválida recebida de {IP}", Request.HttpContext.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        // 4. Autentica como Admin (acesso de serviço)
        var serviceEmail = _configuration["Auth:AdminEmail"] ?? "service@diaxcrm.internal";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, serviceEmail),
            new Claim(JwtRegisteredClaimNames.Email, serviceEmail),
            new Claim(ClaimTypes.Email, serviceEmail),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("auth_method", "api_key"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogDebug("ApiKey auth: acesso de serviço autenticado para {Email}", serviceEmail);

        return Task.FromResult(AuthenticateResult.Success(ticket));
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
