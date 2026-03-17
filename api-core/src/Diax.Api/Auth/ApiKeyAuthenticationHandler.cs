using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Diax.Api.Auth;

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
        // 1. Verifica se o header está presente
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
            return Task.FromResult(AuthenticateResult.NoResult()); // Deixa o próximo scheme tentar

        var providedKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        // 2. Lê a chave configurada no servidor
        var configuredKey = _configuration["ServiceApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            Logger.LogWarning("ApiKey auth: X-Api-Key header recebido, mas 'ServiceApiKey' não está configurado no servidor.");
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
}
