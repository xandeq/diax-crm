using Diax.Application.EmailMarketing.Pro.Dtos;

namespace Diax.Application.EmailMarketing.Pro;

public interface ILeadNormalizationService
{
    Task<NormalizationStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NormalizationPreviewItemDto>> GetPreviewAsync(int page, int pageSize, CancellationToken ct = default);
    Task<RunNormalizationResultDto> RunBatchAsync(bool forceReprocess = false, CancellationToken ct = default);
    Task ApproveAsync(Guid customerId, string approvedName, CancellationToken ct = default);
    Task<NameNormalizationResult> NormalizeWithAiAsync(Guid customerId, CancellationToken ct = default);
}
