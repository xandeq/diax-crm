namespace Diax.Domain.Common;

/// <summary>
/// Interface para Unit of Work pattern.
/// Garante transações atômicas entre múltiplos repositórios.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa uma ação dentro de uma estratégia de execução (resiliência) e transação.
    /// Necessário ao usar EnableRetryOnFailure no EF Core.
    /// </summary>
    Task<T> ExecuteStrategyAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
