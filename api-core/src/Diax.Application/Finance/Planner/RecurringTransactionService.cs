using Diax.Application.Common;
using Diax.Application.Finance.Planner.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Domain.Finance.Planner;
using Diax.Domain.Finance.Planner.Repositories;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

using PlannerTransactionType = Diax.Domain.Finance.Planner.TransactionType;

namespace Diax.Application.Finance.Planner;

public class RecurringTransactionService : IApplicationService
{
    private readonly IRecurringTransactionRepository _repository;
    private readonly IFinancialAccountRepository _financialAccountRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurringTransactionService> _logger;

    public RecurringTransactionService(
        IRecurringTransactionRepository repository,
        IFinancialAccountRepository financialAccountRepository,
        ICreditCardRepository creditCardRepository,
        IUnitOfWork unitOfWork,
        ILogger<RecurringTransactionService> logger)
    {
        _repository = repository;
        _financialAccountRepository = financialAccountRepository;
        _creditCardRepository = creditCardRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<RecurringTransactionResponse>>> GetAllAsync(Guid userId)
    {
        try
        {
            var transactions = await _repository.GetAllByUserIdAsync(userId);
            return Result<IEnumerable<RecurringTransactionResponse>>.Success(transactions.Select(MapToResponse));
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
            return Result<IEnumerable<RecurringTransactionResponse>>.Success(transactions.Select(MapToResponse));
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
            var validationError = await ValidateRequestAsync(
                request.Type,
                request.ItemKind,
                request.Description,
                request.Amount,
                request.CategoryId,
                request.DayOfMonth,
                request.StartDate,
                request.EndDate,
                request.PaymentMethod,
                request.CreditCardId,
                request.FinancialAccountId,
                userId);

            if (validationError != null)
                return Result.Failure<RecurringTransactionResponse>(validationError);

            var existsDuplicate = await _repository.ExistsDuplicateAsync(
                userId,
                request.Description.Trim(),
                request.DayOfMonth,
                request.Amount,
                (Diax.Domain.Finance.TransactionType)request.Type,
                request.ItemKind);

            if (existsDuplicate)
            {
                return Result.Failure<RecurringTransactionResponse>(
                    new Error("RecurringTransaction.Duplicate", "Já existe um item recorrente semelhante"));
            }

            var transaction = new RecurringTransaction
            {
                UserId = userId,
                Type = request.Type,
                ItemKind = request.ItemKind,
                Description = request.Description.Trim(),
                Details = request.Details?.Trim(),
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

    public async Task<Result<RecurringTransactionResponse>> UpdateAsync(Guid id, UpdateRecurringTransactionRequest request, Guid userId)
    {
        try
        {
            var recurring = await _repository.GetByIdAsync(id, userId);
            if (recurring == null)
            {
                return Result.Failure<RecurringTransactionResponse>(
                    new Error("RecurringTransaction.NotFound", "Transação recorrente não encontrada"));
            }

            var validationError = await ValidateRequestAsync(
                request.Type,
                request.ItemKind,
                request.Description,
                request.Amount,
                request.CategoryId,
                request.DayOfMonth,
                request.StartDate,
                request.EndDate,
                request.PaymentMethod,
                request.CreditCardId,
                request.FinancialAccountId,
                userId);

            if (validationError != null)
                return Result.Failure<RecurringTransactionResponse>(validationError);

            var existsDuplicate = await _repository.ExistsDuplicateAsync(
                userId,
                request.Description.Trim(),
                request.DayOfMonth,
                request.Amount,
                (Diax.Domain.Finance.TransactionType)request.Type,
                request.ItemKind,
                id);

            if (existsDuplicate)
            {
                return Result.Failure<RecurringTransactionResponse>(
                    new Error("RecurringTransaction.Duplicate", "Já existe um item recorrente semelhante"));
            }

            recurring.Update(
                request.Type,
                request.Description.Trim(),
                request.Amount,
                request.CategoryId,
                request.FrequencyType,
                request.DayOfMonth,
                request.StartDate,
                request.EndDate,
                request.PaymentMethod,
                request.CreditCardId,
                request.FinancialAccountId,
                request.IsActive,
                request.Priority,
                request.Details?.Trim(),
                request.ItemKind);

            await _repository.UpdateAsync(recurring);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Recurring transaction {Id} updated for user {UserId}", recurring.Id, userId);
            return Result<RecurringTransactionResponse>.Success(MapToResponse(recurring));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recurring transaction {Id} for user {UserId}", id, userId);
            return Result.Failure<RecurringTransactionResponse>(
                new Error("RecurringTransaction.UpdateFailed", "Falha ao atualizar transação recorrente"));
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
            ItemKind = transaction.ItemKind,
            Description = transaction.Description,
            Details = transaction.Details,
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
            IsSubscription = transaction.ItemKind == RecurringItemKind.Subscription,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt ?? transaction.CreatedAt
        };
    }

    private async Task<Error?> ValidateRequestAsync(
        PlannerTransactionType type,
        RecurringItemKind itemKind,
        string description,
        decimal amount,
        Guid categoryId,
        int dayOfMonth,
        DateTime startDate,
        DateTime? endDate,
        PaymentMethod paymentMethod,
        Guid? creditCardId,
        Guid? financialAccountId,
        Guid userId)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new Error("RecurringTransaction.InvalidDescription", "Descrição é obrigatória");

        if (amount <= 0)
            return new Error("RecurringTransaction.InvalidAmount", "Valor deve ser maior que zero");

        if (categoryId == Guid.Empty)
            return new Error("RecurringTransaction.InvalidCategory", "Categoria é obrigatória");

        if (dayOfMonth < 1 || dayOfMonth > 31)
            return new Error("RecurringTransaction.InvalidDay", "Dia do mês deve estar entre 1 e 31");

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
            return new Error("RecurringTransaction.InvalidPeriod", "Data final deve ser maior ou igual à data inicial");

        if (paymentMethod == PaymentMethod.CreditCard)
        {
            if (!creditCardId.HasValue)
                return new Error("RecurringTransaction.CardRequired", "Transações com cartão de crédito exigem um cartão");

            var card = await _creditCardRepository.GetByIdAndUserAsync(creditCardId.Value, userId);
            if (card == null)
                return new Error("RecurringTransaction.CardNotFound", "Cartão de crédito não encontrado");

            if (financialAccountId.HasValue)
                return new Error("RecurringTransaction.InvalidAccountLink", "Transações com cartão não podem vincular conta financeira");
        }
        else
        {
            if (!financialAccountId.HasValue)
                return new Error("RecurringTransaction.AccountRequired", "Transações sem cartão exigem uma conta financeira");

            var account = await _financialAccountRepository.GetByIdAndUserAsync(financialAccountId.Value, userId);
            if (account == null)
                return new Error("RecurringTransaction.AccountNotFound", "Conta financeira não encontrada");

            if (creditCardId.HasValue)
                return new Error("RecurringTransaction.InvalidCardLink", "Transações sem cartão não podem vincular cartão de crédito");
        }

        if (itemKind == RecurringItemKind.Subscription && type != PlannerTransactionType.Expense)
            return new Error("RecurringTransaction.InvalidSubscription", "Assinaturas devem ser cadastradas como despesa");

        return null;
    }
}
