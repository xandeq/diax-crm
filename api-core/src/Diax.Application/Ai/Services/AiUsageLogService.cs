using Diax.Application.Ai.Dtos;
using Diax.Domain.AI;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Ai.Services;

public interface IAiUsageLogService
{
    Task<Result<AiUsageSummaryResponseDto>> GetSummaryAsync(
        AiUsageSummaryRequestDto request,
        CancellationToken cancellationToken = default);
}

public class AiUsageLogService : IAiUsageLogService
{
    private readonly IAiUsageLogRepository _repository;
    private readonly ILogger<AiUsageLogService> _logger;

    public AiUsageLogService(
        IAiUsageLogRepository repository,
        ILogger<AiUsageLogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<AiUsageSummaryResponseDto>> GetSummaryAsync(
        AiUsageSummaryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _repository.GetSummaryAsync(
                request.StartDate,
                request.EndDate,
                request.ProviderId,
                request.ModelId,
                request.UserId,
                cancellationToken);

            // Map to DTO
            var byProviderDto = summary.ByProvider.ToDictionary(
                kvp => kvp.Key,
                kvp => new ProviderUsageDto(
                    kvp.Value.ProviderId,
                    kvp.Value.ProviderName,
                    kvp.Value.RequestCount,
                    kvp.Value.TotalCost
                )
            );

            var response = new AiUsageSummaryResponseDto(
                TotalRequests: summary.TotalRequests,
                TotalTokensInput: summary.TotalTokensInput,
                TotalTokensOutput: summary.TotalTokensOutput,
                TotalTokens: summary.TotalTokens,
                TotalCostEstimated: summary.TotalCostEstimated,
                ByProvider: byProviderDto,
                PeriodStart: request.StartDate,
                PeriodEnd: request.EndDate
            );

            return Result<AiUsageSummaryResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI usage summary");
            return Result<AiUsageSummaryResponseDto>.Failure(
                new Error("AiUsage.QueryFailed", "Falha ao obter sumário de uso de IA"));
        }
    }
}
