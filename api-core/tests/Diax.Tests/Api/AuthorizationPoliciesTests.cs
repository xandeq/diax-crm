using System.Security.Claims;
using Diax.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Diax.Tests.Api;

/// <summary>
/// Testes da fronteira entre a chave de serviço e a chave só-de-proxy.
///
/// Por que isto importa: a ServiceApiKey autentica como Admin, então quem a tiver lê e escreve
/// clientes, leads, usuários e logs de auditoria. Ela nunca deveria ser distribuída só para
/// consumir os proxies de IA — daí a ProxyApiKey, que é descartável.
///
/// A regra que sustenta isso é sutil: [Authorize] sozinho só exige "autenticado", então sem uma
/// policy padrão restritiva a identidade de proxy passaria em TODOS os controllers do sistema. Os
/// testes abaixo exercitam a configuração real registrada pelo app, não uma cópia dela.
/// </summary>
public class AuthorizationPoliciesTests
{
    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static AuthorizationOptions BuildOptions()
    {
        var options = new AuthorizationOptions();
        AuthorizationPolicies.Configure(options);
        return options;
    }

    private static ClaimsPrincipal PrincipalWithRole(string role) =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Email, "quem@exemplo.test") },
            authenticationType: "TestAuth"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private static async Task<bool> AllowedByDefaultPolicy(ClaimsPrincipal user)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(user, resource: null, BuildOptions().DefaultPolicy);
        return result.Succeeded;
    }

    private static async Task<bool> AllowedByProxyPolicy(ClaimsPrincipal user)
    {
        var result = await BuildAuthorizationService()
            .AuthorizeAsync(user, resource: null, ApiKeyAuthenticationOptions.ProxyPolicy);
        return result.Succeeded;
    }

    [Fact]
    public async Task ProxyIdentity_IsRejectedByTheDefaultPolicy()
    {
        // Esta é A garantia: a chave de proxy não abre nenhum controller comum do CRM.
        var proxyUser = PrincipalWithRole(ApiKeyAuthenticationOptions.ProxyOnlyRole);

        Assert.False(await AllowedByDefaultPolicy(proxyUser));
    }

    [Fact]
    public async Task ProxyIdentity_IsAcceptedByTheProxyPolicy()
    {
        var proxyUser = PrincipalWithRole(ApiKeyAuthenticationOptions.ProxyOnlyRole);

        Assert.True(await AllowedByProxyPolicy(proxyUser));
    }

    [Fact]
    public async Task AdminIdentity_KeepsAccessEverywhere()
    {
        // A ServiceApiKey e os workflows n8n não podem ser afetados por esta mudança.
        var admin = PrincipalWithRole("Admin");

        Assert.True(await AllowedByDefaultPolicy(admin));
        Assert.True(await AllowedByProxyPolicy(admin));
    }

    [Fact]
    public async Task OrdinaryUserIdentity_KeepsAccessEverywhere()
    {
        // Sessão normal de usuário via JWT — não pode ter regredido.
        var user = PrincipalWithRole("User");

        Assert.True(await AllowedByDefaultPolicy(user));
        Assert.True(await AllowedByProxyPolicy(user));
    }

    [Fact]
    public async Task AnonymousIsRejectedByBothPolicies()
    {
        Assert.False(await AllowedByDefaultPolicy(Anonymous()));
        Assert.False(await AllowedByProxyPolicy(Anonymous()));
    }

    [Fact]
    public void ProxyPolicyIsActuallyRegistered()
    {
        // Se a policy sumir do registro, [Authorize(Policy = ...)] lança em runtime e os proxies
        // caem com 500 em vez de 401 — falha que só apareceria em produção.
        Assert.NotNull(BuildOptions().GetPolicy(ApiKeyAuthenticationOptions.ProxyPolicy));
    }
}
