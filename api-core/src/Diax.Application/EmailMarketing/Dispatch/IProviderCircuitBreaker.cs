namespace Diax.Application.EmailMarketing.Dispatch;

public interface IProviderCircuitBreaker
{
    bool IsOpen(string providerKey);
    void RecordSuccess(string providerKey);
    void RecordFailure(string providerKey, string? errorMessage);
}
