using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Domain.Customers;

namespace Diax.Application.EmailMarketing.Pro;

public class LeadNormalizationService : ILeadNormalizationService
{
    private readonly ICustomerRepository _repo;
    private readonly INameNormalizationService _normalizer;

    public LeadNormalizationService(
        ICustomerRepository repo,
        INameNormalizationService normalizer)
    {
        _repo = repo;
        _normalizer = normalizer;
    }

    public async Task<NormalizationStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var all = (await _repo.GetAllAsync(ct)).ToList();

        var total = all.Count;
        var normalized = all.Count(c => c.NormalizedName != null);
        var pending = total - normalized;
        var highConf = all.Count(c => c.NormalizationScore >= 80);
        var lowConf  = all.Count(c => c.NormalizationScore is > 0 and < 80);
        var coverage = total > 0 ? Math.Round(normalized * 100.0 / total, 1) : 0.0;

        return new NormalizationStatsDto
        {
            Total           = total,
            Normalized      = normalized,
            Pending         = pending,
            CoveragePercent = coverage,
            HighConfidence  = highConf,
            LowConfidence   = lowConf,
        };
    }

    public async Task<IReadOnlyList<NormalizationPreviewItemDto>> GetPreviewAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var all = (await _repo.GetAllAsync(ct))
            .OrderBy(c => c.NormalizedName == null ? 0 : 1)
            .ThenBy(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return all.Select(c =>
        {
            var suggestion = _normalizer.Normalize(c.Name, c.Email);
            return new NormalizationPreviewItemDto
            {
                CustomerId            = c.Id.ToString(),
                RawName               = c.Name,
                Email                 = c.Email,
                SuggestedName         = suggestion.NormalizedName.Length > 0 ? suggestion.NormalizedName : null,
                SuggestedScore        = suggestion.Score,
                SuggestedSource       = suggestion.Source.ToString(),
                CurrentNormalizedName = c.NormalizedName,
                CurrentScore          = c.NormalizationScore,
            };
        }).ToList();
    }

    public async Task<RunNormalizationResultDto> RunBatchAsync(
        bool forceReprocess = false, CancellationToken ct = default)
    {
        var all = (await _repo.GetAllAsync(ct)).ToList();

        var toProcess = forceReprocess
            ? all
            : all.Where(c => c.NormalizedName == null).ToList();

        int updated = 0, skipped = 0;

        foreach (var customer in toProcess)
        {
            var result = _normalizer.Normalize(customer.Name, customer.Email);

            if (result.Score == 0 || string.IsNullOrWhiteSpace(result.NormalizedName))
            {
                skipped++;
                continue;
            }

            customer.ApplyNormalization(
                result.NormalizedName,
                "system-deterministic",
                result.Score,
                result.Source);

            await _repo.UpdateAsync(customer, ct);
            updated++;
        }

        return new RunNormalizationResultDto
        {
            Processed = toProcess.Count,
            Updated   = updated,
            Skipped   = skipped,
        };
    }
}
