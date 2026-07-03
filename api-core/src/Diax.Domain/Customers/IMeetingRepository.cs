using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public interface IMeetingRepository : IRepository<Meeting>
{
    /// <summary>Reuniões confirmadas do usuário no intervalo [fromUtc, toUtc) — para calcular disponibilidade.</summary>
    Task<List<Meeting>> GetConfirmedInRangeAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<List<Meeting>> GetUpcomingAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default);
}
