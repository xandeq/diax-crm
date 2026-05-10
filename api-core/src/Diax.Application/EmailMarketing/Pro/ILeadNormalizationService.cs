using Diax.Application.EmailMarketing.Pro.Dtos;

namespace Diax.Application.EmailMarketing.Pro;

public interface ILeadNormalizationService
{
    Task<NormalizationStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NormalizationPreviewItemDto>> GetPreviewAsync(int page, int pageSize, CancellationToken ct = default);
    Task<RunNormalizationResultDto> RunBatchAsync(bool forceReprocess = false, CancellationToken ct = default);
}
