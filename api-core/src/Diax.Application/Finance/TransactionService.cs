using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;
using System.Linq.Expressions;

namespace Diax.Application.Finance;

/// <summary>
/// Serviço unificado de transações financeiras.
/// Substitui IncomeService + ExpenseService com lógica de saldo por tipo:
/// - Income → Credit na conta
/// - Expense (cash) → Debit na conta
/// - Expense (credit card) → Sem impacto em conta
/// - Transfer → Sem impacto (gerido via AccountTransferService)
/// - Ignored → Sem impacto
/// </summary>
public class TransactionService : IApplicationService
{
    private readonly ITransactionRepository _repository;
    private readonly ITransactionCategoryRepository _categoryRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IImportedTransactionRepository _importedTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(
        ITransactionRepository repository,
        ITransactionCategoryRepository categoryRepository,
        IFinancialAccountRepository accountRepository,
        IImportedTransactionRepository importedTransactionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _accountRepository = accountRepository;
        _importedTransactionRepository = importedTransactionRepository;
        _unitOfWork = unitOfWork;
    }

    // ── Queries ──────────────────────────────────────────────────

    public async Task<Result<IEnumerable<TransactionResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _repository.GetAllByUserIdAsync(userId, ct);
        return Result<IEnumerable<TransactionResponse>>.Success(items.Select(MapToResponse));
    }

    public async Task<Result<TransactionResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
            return Result.Failure<TransactionResponse>(Errors.NotFound);

        return Result<TransactionResponse>.Success(MapToResponse(tx));
    }

    public async Task<Result<IEnumerable<TransactionResponse>>> GetByTypeAsync(TransactionType type, Guid userId, CancellationToken ct = default)
    {
        var items = await _repository.GetByTypeAsync(type, userId, ct);
        return Result<IEnumerable<TransactionResponse>>.Success(items.Select(MapToResponse));
    }

    public async Task<Result<IEnumerable<TransactionResponse>>> GetByMonthAsync(int year, int month, Guid userId, CancellationToken ct = default)
    {
        var items = await _repository.GetByMonthAsync(year, month, userId, ct);
        return Result<IEnumerable<TransactionResponse>>.Success(items.Select(MapToResponse));
    }

    public async Task<Result<PagedResult<TransactionResponse>>> GetPagedAsync(TransactionPagedRequest request, Guid userId, CancellationToken ct = default)
    {
        Expression<Func<Transaction, bool>> predicate = t =>
            t.UserId == userId &&
            (!request.Type.HasValue || t.Type == request.Type.Value) &&
            (!request.StartDate.HasValue || t.Date >= request.StartDate.Value) &&
            (!request.EndDate.HasValue || t.Date <= request.EndDate.Value) &&
            (!request.CategoryId.HasValue || t.CategoryId == request.CategoryId.Value) &&
            (!request.FinancialAccountId.HasValue || t.FinancialAccountId == request.FinancialAccountId.Value) &&
            (!request.MinAmount.HasValue || t.Amount >= request.MinAmount.Value) &&
            (!request.MaxAmount.HasValue || t.Amount <= request.MaxAmount.Value) &&
            (!request.Status.HasValue || t.Status == request.Status.Value) &&
            (!request.CreditCardId.HasValue || t.CreditCardId == request.CreditCardId.Value) &&
            (!request.CreditCardInvoiceId.HasValue || t.CreditCardInvoiceId == request.CreditCardInvoiceId.Value) &&
            (string.IsNullOrWhiteSpace(request.Search) || t.Description.Contains(request.Search));

        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy = request.SortBy?.ToLower() switch
        {
            "date" => q => request.SortDescending ? q.OrderByDescending(t => t.Date) : q.OrderBy(t => t.Date),
            "amount" => q => request.SortDescending ? q.OrderByDescending(t => t.Amount) : q.OrderBy(t => t.Amount),
            "description" => q => request.SortDescending ? q.OrderByDescending(t => t.Description) : q.OrderBy(t => t.Description),
            "type" => q => request.SortDescending ? q.OrderByDescending(t => t.Type) : q.OrderBy(t => t.Type),
            _ => q => q.OrderByDescending(t => t.Date)
        };

        var paged = await _repository.GetPagedAsync(request.Page, request.PageSize, predicate, orderBy, ct);
        var items = paged.Items.Select(MapToResponse);
        var result = new PagedResult<TransactionResponse>(items, paged.TotalCount, paged.Page, paged.PageSize);

        return Result<PagedResult<TransactionResponse>>.Success(result);
    }

    public async Task<Result<IEnumerable<TransactionResponse>>> GetByStatusAsync(TransactionStatus status, Guid userId, CancellationToken ct = default)
    {
        var items = await _repository.GetAllByUserIdAsync(userId, ct);
        var filtered = items.Where(t => t.Status == status);
        return Result<IEnumerable<TransactionResponse>>.Success(filtered.Select(MapToResponse));
    }

    // ── Commands ─────────────────────────────────────────────────

    public async Task<Result<Guid>> CreateAsync(CreateTransactionRequest request, Guid userId, CancellationToken ct = default)
    {
        // Validar categoria se informada
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAndUserAsync(request.CategoryId.Value, userId, ct);
            if (category == null || !category.IsActive)
                return Result.Failure<Guid>(new Error("Transaction.InvalidCategory", "Categoria inválida ou inativa"));
        }

        // Validar conta financeira quando necessária
        FinancialAccount? account = null;
        if (request.FinancialAccountId.HasValue)
        {
            account = await _accountRepository.GetByIdAndUserAsync(request.FinancialAccountId.Value, userId, ct);
            if (account == null)
                return Result.Failure<Guid>(new Error("Transaction.InvalidAccount", "Conta financeira não encontrada"));
            if (!account.IsActive)
                return Result.Failure<Guid>(new Error("Transaction.InactiveAccount", "Conta financeira inativa"));
        }

        // Criar entidade conforme tipo
        Transaction transaction;
        switch (request.Type)
        {
            case TransactionType.Income:
                if (!request.FinancialAccountId.HasValue)
                    return Result.Failure<Guid>(new Error("Transaction.AccountRequired", "Receita requer conta financeira"));

                transaction = Transaction.CreateIncome(
                    request.Description, request.Amount, request.Date,
                    request.PaymentMethod, request.CategoryId,
                    request.IsRecurring, request.FinancialAccountId.Value, userId,
                    request.Details, paidDate: request.PaidDate);
                break;

            case TransactionType.Expense:
                transaction = Transaction.CreateExpense(
                    request.Description, request.Amount, request.Date,
                    request.PaymentMethod, request.CategoryId,
                    request.IsRecurring, userId,
                    request.CreditCardId, request.CreditCardInvoiceId,
                    request.FinancialAccountId, request.Status, request.PaidDate,
                    request.Details, null, request.IsSubscription,
                    request.HasVariableAmount);
                break;

            case TransactionType.Transfer:
                // Transferências entre contas são criadas via AccountTransferService.
                // Se alguém criar manualmente (cenário raro), exige conta.
                if (!request.FinancialAccountId.HasValue)
                    return Result.Failure<Guid>(new Error("Transaction.AccountRequired", "Transferência requer conta financeira"));

                var groupId = Guid.NewGuid();
                transaction = Transaction.CreateTransfer(
                    request.Description, request.Amount, request.Date,
                    request.FinancialAccountId.Value, userId, groupId);
                break;

            case TransactionType.Ignored:
                transaction = Transaction.CreateFromImport(
                    request.Description, request.Amount, request.Date,
                    TransactionType.Ignored, request.PaymentMethod,
                    request.CategoryId, request.FinancialAccountId,
                    request.CreditCardId, request.CreditCardInvoiceId,
                    userId, request.Description, RawBankType.Unknown,
                    TransactionStatus.Paid);
                break;

            default:
                return Result.Failure<Guid>(new Error("Transaction.InvalidType", "Tipo de transação inválido"));
        }

        // Aplicar impacto no saldo
        ApplyBalanceImpact(account, transaction.Type, transaction.Amount, transaction.PaymentMethod);

        await _repository.AddAsync(transaction, ct);
        if (account != null)
            await _accountRepository.UpdateAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(transaction.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateTransactionRequest request, Guid userId, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
            return Result.Failure(Errors.NotFound);

        // Validar categoria
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAndUserAsync(request.CategoryId.Value, userId, ct);
            if (category == null || !category.IsActive)
                return Result.Failure(new Error("Transaction.InvalidCategory", "Categoria inválida ou inativa"));
        }

        // Validar nova conta
        FinancialAccount? newAccount = null;
        if (request.FinancialAccountId.HasValue)
        {
            newAccount = await _accountRepository.GetByIdAndUserAsync(request.FinancialAccountId.Value, userId, ct);
            if (newAccount == null)
                return Result.Failure(new Error("Transaction.InvalidAccount", "Conta financeira não encontrada"));
            if (!newAccount.IsActive)
                return Result.Failure(new Error("Transaction.InactiveAccount", "Conta financeira inativa"));
        }

        // Reverter saldo antigo se conta/valor mudou
        var accountChanged = tx.FinancialAccountId != request.FinancialAccountId;
        var amountChanged = tx.Amount != request.Amount;
        var paymentMethodChanged = tx.PaymentMethod != request.PaymentMethod;

        if (accountChanged || amountChanged || paymentMethodChanged)
        {
            // Reverter impacto antigo
            if (tx.FinancialAccountId.HasValue)
            {
                var oldAccount = await _accountRepository.GetByIdAndUserAsync(tx.FinancialAccountId.Value, userId, ct);
                if (oldAccount != null)
                {
                    ReverseBalanceImpact(oldAccount, tx.Type, tx.Amount, tx.PaymentMethod);
                    await _accountRepository.UpdateAsync(oldAccount, ct);
                }
            }

            // Aplicar novo impacto
            if (newAccount != null)
            {
                ApplyBalanceImpact(newAccount, tx.Type, request.Amount, request.PaymentMethod);
                await _accountRepository.UpdateAsync(newAccount, ct);
            }
        }

        tx.Update(
            request.Description, request.Amount, request.Date,
            request.PaymentMethod, request.CategoryId, request.IsRecurring,
            request.FinancialAccountId, request.CreditCardId,
            request.CreditCardInvoiceId, request.Status, request.PaidDate,
            request.Details, request.IsSubscription, request.HasVariableAmount);

        await _repository.UpdateAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
        {
            var any = await _repository.GetByIdAsync(id, ct);
            if (any != null)
                Console.WriteLine($"[TransactionService.DeleteAsync] ERRO: Transação {id} pertence ao usuário {any.UserId}, tentativa por {userId}");
            else
                Console.WriteLine($"[TransactionService.DeleteAsync] ERRO: Transação {id} não existe");

            return Result.Failure(Errors.NotFound);
        }

        // Resetar ImportedTransactions vinculadas
        var linked = await _importedTransactionRepository.GetByTransactionIdAsync(id, ct);
        foreach (var imp in linked)
        {
            imp.Reset();
            await _importedTransactionRepository.UpdateAsync(imp, ct);
        }

        // Reverter saldo
        if (tx.FinancialAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAndUserAsync(tx.FinancialAccountId.Value, userId, ct);
            if (account != null)
            {
                ReverseBalanceImpact(account, tx.Type, tx.Amount, tx.PaymentMethod);
                await _accountRepository.UpdateAsync(account, ct);
            }
        }

        await _repository.DeleteAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<BulkDeleteResponse>> DeleteRangeAsync(BulkDeleteRequest request, Guid userId, CancellationToken ct = default)
    {
        if (request.Ids == null || !request.Ids.Any())
            return Result<BulkDeleteResponse>.Success(new BulkDeleteResponse(true, 0, 0, new List<string>()));

        var emptyIds = request.Ids.Where(id => id == Guid.Empty).ToList();
        if (emptyIds.Any())
            return Result.Failure<BulkDeleteResponse>(new Error("General.InvalidIds", $"{emptyIds.Count} IDs inválidos foram enviados"));

        if (request.Ids.Count > 100)
            return Result.Failure<BulkDeleteResponse>(new Error("General.BatchLimitExceeded", "O limite máximo é de 100 itens por exclusão."));

        Console.WriteLine($"[TransactionService.DeleteRangeAsync] Exclusão de {request.Ids.Count} transações, user {userId}");

        return await _unitOfWork.ExecuteStrategyAsync(async (cToken) =>
        {
            await _unitOfWork.BeginTransactionAsync(cToken);
            try
            {
                int deletedCount = 0;
                var errors = new List<string>();

                foreach (var id in request.Ids)
                {
                    var result = await DeleteAsync(id, userId, cToken);
                    if (!result.IsSuccess)
                    {
                        if (result.Error.Code == "Transaction.NotFound")
                        {
                            errors.Add($"{result.Error.Code}:{id}");
                            continue;
                        }
                        await _unitOfWork.RollbackTransactionAsync(cToken);
                        return Result.Failure<BulkDeleteResponse>(result.Error);
                    }
                    deletedCount++;
                }

                await _unitOfWork.CommitTransactionAsync(cToken);
                return Result<BulkDeleteResponse>.Success(new BulkDeleteResponse(true, deletedCount, errors.Count, errors));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cToken);
                return Result.Failure<BulkDeleteResponse>(new Error("General.BulkDeleteError", "Erro ao excluir em massa: " + ex.Message));
            }
        }, ct);
    }

    // ── Reclassify ──────────────────────────────────────────────

    /// <summary>
    /// Reclassifica o tipo financeiro de uma transação.
    /// Reverte o saldo do tipo antigo e aplica o do novo tipo.
    /// </summary>
    public async Task<Result> ReclassifyAsync(Guid id, ReclassifyTransactionRequest request, Guid userId, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
            return Result.Failure(Errors.NotFound);

        if (tx.Type == request.NewType)
            return Result.Success(); // Nada a mudar

        // Reverter saldo do tipo antigo
        if (tx.FinancialAccountId.HasValue)
        {
            var account = await _accountRepository.GetByIdAndUserAsync(tx.FinancialAccountId.Value, userId, ct);
            if (account != null)
            {
                ReverseBalanceImpact(account, tx.Type, tx.Amount, tx.PaymentMethod);
                ApplyBalanceImpact(account, request.NewType, tx.Amount, tx.PaymentMethod);
                await _accountRepository.UpdateAsync(account, ct);
            }
        }

        tx.Reclassify(request.NewType, request.TransferGroupId);

        await _repository.UpdateAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ── Status ──────────────────────────────────────────────────

    public async Task<Result> MarkAsPaidAsync(Guid id, Guid userId, DateTime? paidDate = null, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
            return Result.Failure(Errors.NotFound);

        tx.MarkAsPaid(paidDate);
        await _repository.UpdateAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> MarkAsPendingAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var tx = await _repository.GetByIdAndUserAsync(id, userId, ct);
        if (tx == null)
            return Result.Failure(Errors.NotFound);

        tx.MarkAsPending();
        await _repository.UpdateAsync(tx, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ── Balance helpers ─────────────────────────────────────────

    /// <summary>
    /// Aplica o impacto no saldo da conta conforme o tipo da transação.
    /// Income → Credit | Expense (cash) → Debit | Transfer/Ignored → nenhum
    /// </summary>
    private static void ApplyBalanceImpact(FinancialAccount? account, TransactionType type, decimal amount, PaymentMethod paymentMethod)
    {
        if (account == null) return;

        switch (type)
        {
            case TransactionType.Income:
                account.Credit(amount);
                break;
            case TransactionType.Expense:
                if (paymentMethod != PaymentMethod.CreditCard)
                    account.Debit(amount);
                break;
            // Transfer e Ignored não afetam saldo — gerido via AccountTransferService
            case TransactionType.Transfer:
            case TransactionType.Ignored:
            default:
                break;
        }
    }

    /// <summary>
    /// Reverte o impacto no saldo (operação inversa de ApplyBalanceImpact).
    /// </summary>
    private static void ReverseBalanceImpact(FinancialAccount? account, TransactionType type, decimal amount, PaymentMethod paymentMethod)
    {
        if (account == null) return;

        switch (type)
        {
            case TransactionType.Income:
                account.Debit(amount);
                break;
            case TransactionType.Expense:
                if (paymentMethod != PaymentMethod.CreditCard)
                    account.Credit(amount);
                break;
            case TransactionType.Transfer:
            case TransactionType.Ignored:
            default:
                break;
        }
    }

    // ── Mapping ─────────────────────────────────────────────────

    private static TransactionResponse MapToResponse(Transaction tx)
    {
        return new TransactionResponse(
            tx.Id,
            tx.Description,
            tx.Amount,
            tx.Date,
            tx.Type,
            tx.RawBankType,
            tx.RawDescription,
            tx.Details,
            tx.PaymentMethod,
            tx.CategoryId,
            tx.Category?.Name,
            tx.IsRecurring,
            tx.IsSubscription,
            tx.HasVariableAmount,
            tx.FinancialAccountId,
            tx.FinancialAccount?.Name,
            tx.CreditCardId,
            tx.CreditCard?.Name,
            tx.CreditCardInvoiceId,
            tx.Status,
            tx.PaidDate,
            tx.TransferGroupId,
            tx.AccountTransferId,
            tx.CreatedAt,
            tx.UpdatedAt);
    }

    // ── Error constants ─────────────────────────────────────────

    private static class Errors
    {
        public static readonly Error NotFound = new("Transaction.NotFound", "Transação não encontrada");
    }
}
