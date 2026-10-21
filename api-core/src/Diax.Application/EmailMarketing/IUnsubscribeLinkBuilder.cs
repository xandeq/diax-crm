namespace Diax.Application.EmailMarketing;

/// <summary>
/// Gera as URLs de unsubscribe (com token HMAC) e o CTA default dos emails.
/// Fonte única — o token gerado aqui é o mesmo validado pelo EmailUnsubscribeController.
/// </summary>
public interface IUnsubscribeLinkBuilder
{
    string BuildUnsubscribeUrl(Guid userId, string email);
    /// <summary>Confere o token de um link gerado por <see cref="BuildUnsubscribeUrl"/> (tempo constante).</summary>
    bool IsValidToken(string userId, string email, string token);
    string DefaultCtaUrl { get; }
}
