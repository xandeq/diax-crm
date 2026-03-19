using Diax.Domain.AI;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI.Services;

/// <summary>
/// Service for tracking AI usage for audit and cost analysis
/// </summary>
public interface IAiUsageTrackingService
{
    Task LogUsageAsync(
        Guid userId,
        Guid providerId,
        Guid modelId,
        string featureType,
        TimeSpan duration,
        bool success,
        string requestId,
        int? inputTokens = null,
        int? outputTokens = null,
        string? errorMessage = null,
        string? errorCategory = null,
        int? httpStatusCode = null,
        CancellationToken cancellationToken = default);
}

public class AiUsageTrackingService : IAiUsageTrackingService
{
    private readonly IAiUsageLogRepository _repository;
    private readonly ILogger<AiUsageTrackingService> _logger;

    public AiUsageTrackingService(
        IAiUsageLogRepository repository,
        ILogger<AiUsageTrackingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task LogUsageAsync(
        Guid userId,
        Guid providerId,
        Guid modelId,
        string featureType,
        TimeSpan duration,
        bool success,
        string requestId,
        int? inputTokens = null,
        int? outputTokens = null,
        string? errorMessage = null,
        string? errorCategory = null,
        int? httpStatusCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            decimal? estimatedCost = null;
            if (inputTokens.HasValue && outputTokens.HasValue)
            {
                var inputCost  = (inputTokens.Value  / 1000m) * 0.03m;
                var outputCost = (outputTokens.Value / 1000m) * 0.06m;
                estimatedCost = inputCost + outputCost;
            }

            var log = new AiUsageLog(
                userId: userId,
                providerId: providerId,
                modelId: modelId,
                featureType: featureType,
                duration: duration,
                success: success,
                requestId: requestId,
                inputTokens: inputTokens,
                outputTokens: outputTokens,
                estimatedCost: estimatedCost,
                errorMessage: errorMessage,
                errorCategory: errorCategory,
                httpStatusCode: httpStatusCode
            );

            await _repository.CreateAsync(log, cancellationToken);

            _logger.LogInformation(
                "AI usage logged: User={UserId}, Provider={ProviderId}, Model={ModelId}, " +
                "Feature={FeatureType}, Success={Success}, Category={ErrorCategory}, Tokens={Tokens}",
                userId, providerId, modelId, featureType, success,
                errorCategory ?? "—",
                (inputTokens ?? 0) + (outputTokens ?? 0));
        }
        catch (Exception ex)
        {
            // Never fail the request because of a logging error
            _logger.LogError(ex, "Failed to log AI usage for request {RequestId}", requestId);
        }
    }
}
