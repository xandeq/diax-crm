namespace Diax.Application.EmailMarketing;

/// <summary>
/// Gera as URLs de unsubscribe (com token HMAC) e o CTA default dos emails.
/// Fonte única — o token gerado aqui é o mesmo validado pelo EmailUnsubscribeController.
/// </summary>
public interface IUnsubscribeLinkBuilder
{
    string BuildUnsubscribeUrl(Guid userId, string email);
    string DefaultCtaUrl { get; }
}
