using Diax.Domain.TaxDocuments;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class TaxDocumentRepository : Repository<TaxDocument>, ITaxDocumentRepository
{
    public TaxDocumentRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<List<TaxDocument>> GetByUserAsync(
        Guid userId,
        int? fiscalYear,
        TaxDocumentType? institutionType,
        string? search,
        CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking().Where(x => x.UserId == userId);

        if (fiscalYear.HasValue)
            query = query.Where(x => x.FiscalYear == fiscalYear.Value);

        if (institutionType.HasValue)
            query = query.Where(x => x.InstitutionType == institutionType.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.InstitutionName.Contains(search));

        return await query
            .OrderByDescending(x => x.FiscalYear)
            .ThenBy(x => x.InstitutionName)
            .ToListAsync(ct);
    }

    public async Task<List<int>> GetFiscalYearsByUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.FiscalYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
    }

    public async Task<TaxDocument?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
    }
}
