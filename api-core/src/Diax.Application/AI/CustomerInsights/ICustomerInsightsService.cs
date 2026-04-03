namespace Diax.Application.AI.CustomerInsights;

public interface ICustomerInsightsService
{
    Task<GenerateInsightsResponseDto> GenerateInsightsAsync(
        GenerateInsightsRequestDto request,
        Guid userId,
        CancellationToken ct = default);
}
