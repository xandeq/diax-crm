using Diax.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace Diax.Infrastructure.Data;

/// <summary>
/// Implementação do Unit of Work usando EF Core.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DiaxDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(DiaxDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Nenhuma transação foi iniciada.");

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Nenhuma transação foi iniciada.");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
