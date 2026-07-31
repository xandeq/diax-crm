using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Perfil patrimonial do usuário (F2): perfil de risco, meta e alocação-alvo.
/// GetOrCreate semeia os defaults (builder_all, R$1M em 5 anos, alocação ease-ranked).
/// </summary>
public class WealthProfileService : IApplicationService
{
    private readonly IWealthProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WealthProfileService> _logger;

    public WealthProfileService(
        IWealthProfileRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<WealthProfileService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<WealthProfileResponse>> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching wealth profile for user {UserId}", userId);
            var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
            {
                _logger.LogInformation("Wealth profile not found for user {UserId}, seeding defaults", userId);
                profile = WealthProfile.Create(
                    userId,
                    WealthProfileDefaults.RiskProfile,
                    WealthProfileDefaults.GoalAmount,
                    WealthProfileDefaults.GoalYears,
                    WealthProfileDefaults.TargetAllocationJson());

                await _repository.AddAsync(profile, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<WealthProfileResponse>.Success(MapToResponse(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create wealth profile for user {UserId}", userId);
            return Result.Failure<WealthProfileResponse>(
                new Error("WealthProfile.QueryFailed", "Failed to retrieve wealth profile. Please check server logs for details."));
        }
    }

    public async Task<Result<WealthProfileResponse>> UpdateAsync(
        UpdateWealthProfileRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating wealth profile for user {UserId}", userId);
            var profile = await _repository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
            {
                profile = WealthProfile.Create(
                    userId,
                    WealthProfileDefaults.RiskProfile,
                    WealthProfileDefaults.GoalAmount,
                    WealthProfileDefaults.GoalYears,
                    WealthProfileDefaults.TargetAllocationJson());
                await _repository.AddAsync(profile, cancellationToken);
            }

            profile.UpdateGoal(
                request.RiskProfile ?? profile.RiskProfile,
                request.GoalAmount ?? profile.GoalAmount,
                request.GoalYears ?? profile.GoalYears);

            if (request.TargetAllocation is { Count: > 0 })
            {
                profile.UpdateAllocation(WealthProfileDefaults.SerializeAllocation(request.TargetAllocation));
            }

            await _repository.UpdateAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated wealth profile for user {UserId}", userId);
            return Result<WealthProfileResponse>.Success(MapToResponse(profile));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid wealth profile data for user {UserId}", userId);
            return Result.Failure<WealthProfileResponse>(new Error("WealthProfile.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update wealth profile for user {UserId}", userId);
            return Result.Failure<WealthProfileResponse>(
                new Error("WealthProfile.UpdateFailed", "Failed to update wealth profile. Please check server logs for details."));
        }
    }

    private static WealthProfileResponse MapToResponse(WealthProfile profile)
    {
        return new WealthProfileResponse(
            profile.Id,
            profile.RiskProfile,
            profile.GoalAmount,
            profile.GoalYears,
            WealthProfileDefaults.ParseAllocation(profile.TargetAllocationJson),
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }
}
