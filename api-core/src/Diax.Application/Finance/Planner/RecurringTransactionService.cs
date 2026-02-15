using Diax.Application.Common;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Planner;

public class RecurringTransactionService : IApplicationService
{
    private readonly IRecurringTransactionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurringTransactionService> _logger;

    public RecurringTransactionService(
        IRecurringTransactionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RecurringTransactionService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<RecurringTransactionResponse>>> GetAllAsync(Guid userId)
    {
        try
        {
            var transactions = await _repository.GetAllByUserIdAsync(userId);
            var response = transactions.Select(MapToResponse);
            return Result<IEnumerable<RecurringTransactionResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recurring transactions for user {UserId}", userId);
            return Result.Failure<IEnumerable<RecurringTransactionResponse>>(
                new Error("RecurringTransaction.QueryFailed", "Falha ao recuperar transações recorrentes"));
        }
    }

    public async Task<Result<IEnumerable<RecurringTransactionResponse>>> GetActiveAsync(Guid userId)
    {
        try
        {
            var transactions = await _repository.GetActiveRecurringByUserIdAsync(userId);
            var response = transactions.Select(MapToResponse);
            return Result<IEnumerable<RecurringTransactionResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active recurring transactions for user {UserId}", userId);
            return Result.Failure<IEnumerable<RecurringTransactionResponse>>(
                new Error("RecurringTransaction.QueryFailed", "Falha ao recuperar transações ativas"));
        }
    }

    public async Task<Result<RecurringTransactionResponse>> GetByIdAsync(Guid id, Guid userId)
    {
        try
        {
            var transaction = await _repository.GetByIdAsync(id, userId);
            if (transaction == null)
            {
                return Result.Failure<RecurringTransactionResponse>(
                    new Error("RecurringTransaction.NotFound", "Transação recorrente não encontrada"));
            }
            return Result<RecurringTransactionResponse>.Success(MapToResponse(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recurring transaction {Id} for user {UserId}", id, userId);
            return Result.Failure<RecurringTransactionResponse>(
                new Error("RecurringTransaction.QueryFailed", "Falha ao recuperar transação recorrente"));
        }
    }

    public async Task<Result<RecurringTransactionResponse>> CreateAsync(CreateRecurringTransactionRequest request, Guid userId)
    {
        try
        {
            var transaction = new RecurringTransaction
            {
                UserId = userId,
                Type = request.Type,
                Description = request.Description,
                Amount = request.Amount,
                CategoryId = request.CategoryId,
                FrequencyType = request.FrequencyType,
                DayOfMonth = request.DayOfMonth,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                PaymentMethod = request.PaymentMethod,
                CreditCardId = request.CreditCardId,
                FinancialAccountId = request.FinancialAccountId,
                IsActive = true,
                Priority = request.Priority
            };

            await _repository.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recurring transaction {Id} created for user {UserId}", transaction.Id, userId);
            return Result<RecurringTransactionResponse>.Success(MapToResponse(transaction));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create recurring transaction for user {UserId}", userId);
            return Result.Failure<RecurringTransactionResponse>(
                new Error("RecurringTransaction.CreateFailed", "Falha ao criar transação recorrente"));
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
                    new Error("RecurringTransaction.NotFound", "Transação recorrente não encontrada"));
            }

            await _repository.DeleteAsync(id, userId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recurring transaction {Id} deleted for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recurring transaction {Id} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("RecurringTransaction.DeleteFailed", "Falha ao excluir transação recorrente"));
        }
    }

    private static RecurringTransactionResponse MapToResponse(RecurringTransaction transaction)
    {
        return new RecurringTransactionResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            Type = transaction.Type,
            Description = transaction.Description,
            Amount = transaction.Amount,
            CategoryId = transaction.CategoryId,
            FrequencyType = transaction.FrequencyType,
            DayOfMonth = transaction.DayOfMonth,
            StartDate = transaction.StartDate,
            EndDate = transaction.EndDate,
            PaymentMethod = transaction.PaymentMethod,
            CreditCardId = transaction.CreditCardId,
            FinancialAccountId = transaction.FinancialAccountId,
            IsActive = transaction.IsActive,
            Priority = transaction.Priority,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt
        };
    }
}
