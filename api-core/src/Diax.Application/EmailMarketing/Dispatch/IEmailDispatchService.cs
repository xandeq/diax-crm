namespace Diax.Application.EmailMarketing.Dispatch;

public interface IEmailDispatchService
{
    Task<EmailDispatchResult> DispatchAsync(EmailDispatchRequest request, CancellationToken ct = default);
}
