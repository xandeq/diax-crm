using Microsoft.AspNetCore.Authorization;

namespace Diax.Api.Auth;

/// <summary>
/// Configuração das policies de autorização.
///
/// Vive aqui, e não inline em Program.cs, para poder ser testada: a regra que separa a chave de
/// serviço (Admin, acesso total ao CRM) da chave só-de-proxy é uma fronteira de segurança, e uma
/// cópia da regra dentro do teste não provaria nada sobre o que o app realmente registra.
/// </summary>
public static class AuthorizationPolicies
{
    public static void Configure(AuthorizationOptions options)
    {
        // A policy PADRÃO é a que todo [Authorize] sem nome usa. Ela exige usuário autenticado E
        // que ele NÃO seja a identidade da ProxyApiKey. Sem essa segunda condição a chave de proxy
        // abriria o CRM inteiro, porque [Authorize] sozinho só verifica "está autenticado".
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => !ctx.User.IsInRole(ApiKeyAuthenticationOptions.ProxyOnlyRole))
            .Build();

        // Os controllers dos proxies de IA declaram esta policy, que aceita qualquer identidade
        // autenticada — inclusive a de proxy. É o único lugar do sistema onde ela passa.
        options.AddPolicy(ApiKeyAuthenticationOptions.ProxyPolicy, policy => policy
            .RequireAuthenticatedUser());
    }
}
