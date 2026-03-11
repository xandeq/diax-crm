using Diax.Domain.Common;

namespace Diax.Domain.TaxDocuments;

public interface ITaxDocumentRepository : IRepository<TaxDocument>
{
    Task<List<TaxDocument>> GetByUserAsync(
        Guid userId,
        int? fiscalYear,
        TaxDocumentType? institutionType,
        string? search,
        CancellationToken ct = default);

    Task<List<int>> GetFiscalYearsByUserAsync(Guid userId, CancellationToken ct = default);

    Task<TaxDocument?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}
