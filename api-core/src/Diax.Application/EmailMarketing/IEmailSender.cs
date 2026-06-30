namespace Diax.Application.EmailMarketing;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailSendMessage message, CancellationToken cancellationToken = default);

    // Overload rico para o endpoint unificado — cada provider Tier 1 deve implementar.
    // Default retorna Fail gracioso para providers que ainda não foram adaptados.
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(EmailSendResult.Fail($"{GetType().Name} não suporta o overload EmailMessage."));
}
