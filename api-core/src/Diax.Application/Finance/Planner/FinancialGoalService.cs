using Diax.Application.Common;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Planner;

public class FinancialGoalService : IApplicationService
{
    private readonly IFinancialGoalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FinancialGoalService> _logger;

    public FinancialGoalService(
        IFinancialGoalRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<FinancialGoalService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<FinancialGoalResponse>>> GetAllAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Fetching all financial goals for user {UserId}", userId);
            var goals = await _repository.GetAllByUserIdAsync(userId);
            var response = goals.Select(MapToResponse);
            return Result<IEnumerable<FinancialGoalResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial goals for user {UserId}", userId);
            return Result.Failure<IEnumerable<FinancialGoalResponse>>(
                new Error("FinancialGoal.QueryFailed", "Falha ao recuperar metas financeiras"));
        }
    }

    public async Task<Result<IEnumerable<FinancialGoalResponse>>> GetActiveGoalsAsync(Guid userId)
    {
        try
        {
            var goals = await _repository.GetActiveGoalsByUserIdAsync(userId);
            var response = goals.Select(MapToResponse);
            return Result<IEnumerable<FinancialGoalResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active financial goals for user {UserId}", userId);
            return Result.Failure<IEnumerable<FinancialGoalResponse>>(
                new Error("FinancialGoal.QueryFailed", "Falha ao recuperar metas ativas"));
        }
    }

    public async Task<Result<FinancialGoalResponse>> GetByIdAsync(Guid id, Guid userId)
    {
        try
        {
            var goal = await _repository.GetByIdAsync(id, userId);
            if (goal == null)
            {
                return Result.Failure<FinancialGoalResponse>(
                    new Error("FinancialGoal.NotFound", "Meta financeira não encontrada"));
            }
            return Result<FinancialGoalResponse>.Success(MapToResponse(goal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve financial goal {GoalId} for user {UserId}", id, userId);
            return Result.Failure<FinancialGoalResponse>(
                new Error("FinancialGoal.QueryFailed", "Falha ao recuperar meta financeira"));
        }
    }

    public async Task<Result<FinancialGoalResponse>> CreateAsync(CreateFinancialGoalRequest request, Guid userId)
    {
        try
        {
            var goal = new FinancialGoal
            {
                UserId = userId,
                Name = request.Name,
                TargetAmount = request.TargetAmount,
                CurrentAmount = request.CurrentAmount ?? 0,
                TargetDate = request.TargetDate,
                Category = request.Category,
                Priority = request.Priority,
                IsActive = true,
                AutoAllocateSurplus = request.AutoAllocateSurplus
            };

            await _repository.AddAsync(goal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Financial goal {GoalId} created for user {UserId}", goal.Id, userId);
            return Result<FinancialGoalResponse>.Success(MapToResponse(goal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create financial goal for user {UserId}", userId);
            return Result.Failure<FinancialGoalResponse>(
                new Error("FinancialGoal.CreateFailed", "Falha ao criar meta financeira"));
        }
    }

    public async Task<Result<FinancialGoalResponse>> UpdateAsync(Guid id, UpdateFinancialGoalRequest request, Guid userId)
    {
        try
        {
            var goal = await _repository.GetByIdAsync(id, userId);
            if (goal == null)
            {
                return Result.Failure<FinancialGoalResponse>(
                    new Error("FinancialGoal.NotFound", "Meta financeira não encontrada"));
            }

            goal.Name = request.Name;
            goal.TargetAmount = request.TargetAmount;
            goal.TargetDate = request.TargetDate;
            goal.Category = request.Category;
            goal.Priority = request.Priority;
            goal.IsActive = request.IsActive;
            goal.AutoAllocateSurplus = request.AutoAllocateSurplus;

            await _repository.UpdateAsync(goal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Financial goal {GoalId} updated for user {UserId}", id, userId);
            return Result<FinancialGoalResponse>.Success(MapToResponse(goal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update financial goal {GoalId} for user {UserId}", id, userId);
            return Result.Failure<FinancialGoalResponse>(
                new Error("FinancialGoal.UpdateFailed", "Falha ao atualizar meta financeira"));
        }
    }

    public async Task<Result> AddContributionAsync(Guid id, decimal amount, Guid userId)
    {
        try
        {
            var goal = await _repository.GetByIdAsync(id, userId);
            if (goal == null)
            {
                return Result.Failure(
                    new Error("FinancialGoal.NotFound", "Meta financeira não encontrada"));
            }

            goal.AddContribution(amount);
            await _repository.UpdateAsync(goal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added contribution of {Amount} to goal {GoalId}", amount, id);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(new Error("FinancialGoal.InvalidContribution", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add contribution to goal {GoalId}", id);
            return Result.Failure(
                new Error("FinancialGoal.ContributionFailed", "Falha ao adicionar contribuição"));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var exists = await _repository.ExistsAsync(id, userId);
            if (!exists)
            {
                return Result.Failure(
                    new Error("FinancialGoal.NotFound", "Meta financeira não encontrada"));
            }

            await _repository.DeleteAsync(id, userId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Financial goal {GoalId} deleted for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete financial goal {GoalId} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("FinancialGoal.DeleteFailed", "Falha ao excluir meta financeira"));
        }
    }

    private static FinancialGoalResponse MapToResponse(FinancialGoal goal)
    {
        return new FinancialGoalResponse
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            TargetDate = goal.TargetDate,
            Category = goal.Category,
            Priority = goal.Priority,
            IsActive = goal.IsActive,
            AutoAllocateSurplus = goal.AutoAllocateSurplus,
            Progress = goal.GetProgress(),
            RemainingAmount = goal.GetRemainingAmount(),
            IsCompleted = goal.IsCompleted(),
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt ?? goal.CreatedAt
        };
    }
}
