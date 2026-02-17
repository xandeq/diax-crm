using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

/// <summary>
/// Serviço de importação de extratos.
/// Agora cria Transactions unificadas (via Transaction.CreateFromImport)
/// em vez de entidades legadas Income/Expense.
/// </summary>
public class StatementImportService
{
    private readonly IStatementImportRepository _importRepository;
    private readonly IImportedTransactionRepository _importedTxRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly IEnumerable<IFileParser> _parsers;
    private readonly IUnitOfWork _unitOfWork;

    public StatementImportService(
        IStatementImportRepository importRepository,
        IImportedTransactionRepository importedTxRepository,
        ITransactionRepository transactionRepository,
        IFinancialAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository,
        IEnumerable<IFileParser> parsers,
        IUnitOfWork unitOfWork)
    {
        _importRepository = importRepository;
        _importedTxRepository = importedTxRepository;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _creditCardRepository = creditCardRepository;
        _parsers = parsers;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> UploadAsync(
        UploadStatementRequest request,
        string fileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        Guid userId,
        CancellationToken ct = default)
    {
        // 1. Identify parser
        var parser = _parsers.FirstOrDefault(p => contentType.Contains(p.FileType, StringComparison.OrdinalIgnoreCase) ||
                                                  fileName.EndsWith($".{p.FileType}", StringComparison.OrdinalIgnoreCase));

        if (parser == null)
        {
            return Result.Failure<Guid>(new Error("Import.UnsupportedFormat", "Formato de arquivo não suportado."));
        }

        // Logic to fetch CreditCardGroupId if CreditCardId is provided
        Guid? creditCardGroupId = request.CreditCardGroupId;
        if (request.CreditCardId.HasValue && !creditCardGroupId.HasValue)
        {
            var card = await _creditCardRepository.GetByIdAndUserAsync(request.CreditCardId.Value, userId, ct);
            if (card != null && card.CreditCardGroupId.HasValue)
            {
                creditCardGroupId = card.CreditCardGroupId;
            }
        }

        // 2. Create import record
        var import = StatementImport.Create(
            request.ImportType,
            fileName,
            contentType,
            fileSize,
            userId,
            request.FinancialAccountId,
            creditCardGroupId,
            request.CreditCardId);

        await _importRepository.AddAsync(import, ct);

        try
        {
            import.StartProcessing();

            var transactions = new List<ImportedTransaction>();
            await foreach (var parsed in parser.ParseAsync(fileStream, ct))
            {
                var transaction = ImportedTransaction.Create(
                    import.Id,
                    parsed.RawDescription,
                    parsed.Amount,
                    parsed.TransactionDate,
                    userId);

                transactions.Add(transaction);
            }

            if (transactions.Count == 0)
            {
                return Result.Failure<Guid>(new Error("Import.Empty", "Nenhuma transação encontrada no arquivo."));
            }

            await _importedTxRepository.AddRangeAsync(transactions, ct);

            import.Complete(transactions.Count, 0, 0); // Still 0 processed since it's just uploaded
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<Guid>.Success(import.Id);
        }
        catch (Exception ex)
        {
            import.Fail(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<Guid>(new Error("Import.Error", $"Erro ao processar arquivo: {ex.Message}"));
        }
    }

    public async Task<Result<List<StatementImportResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var imports = await _importRepository.GetAllByUserIdAsync(userId, ct);
        var response = imports.OrderByDescending(i => i.CreatedAt)
            .Select(i => new StatementImportResponse(
                i.Id,
                i.ImportType,
                i.FileName,
                i.Status,
                i.TotalRecords,
                i.ProcessedRecords,
                i.FailedRecords,
                i.ErrorMessage,
                i.CreatedAt,
                i.ProcessedAt,
                i.FinancialAccountId,
                i.FinancialAccount?.Name,
                i.CreditCardGroupId,
                i.CreditCardGroup?.Name
            )).ToList();

        return Result<List<StatementImportResponse>>.Success(response);
    }

    public async Task<Result<StatementImportDetailResponse>> GetDetailAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
        {
            return Result.Failure<StatementImportDetailResponse>(new Error("Import.NotFound", "Importação não encontrada."));
        }

        var transactions = await _importedTxRepository.GetByImportIdAsync(id, ct);

        var response = new StatementImportDetailResponse(
            new StatementImportResponse(
                import.Id,
                import.ImportType,
                import.FileName,
                import.Status,
                import.TotalRecords,
                import.ProcessedRecords,
                import.FailedRecords,
                import.ErrorMessage,
                import.CreatedAt,
                import.ProcessedAt,
                import.FinancialAccountId,
                import.FinancialAccount?.Name,
                import.CreditCardGroupId,
                import.CreditCardGroup?.Name
            ),
            transactions.Select(t => new ImportedTransactionResponse(
                t.Id,
                t.RawDescription,
                t.Amount,
                t.TransactionDate,
                t.Status,
                t.MatchedExpenseId,
                t.CreatedExpenseId,
                t.CreatedIncomeId,
                t.CreatedTransactionId,
                t.ErrorMessage
            ))
        );

        return Result<StatementImportDetailResponse>.Success(response);
    }

    public async Task<Result<StatementImportPostPreviewResponse>> PreviewPostAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
            return Result.Failure<StatementImportPostPreviewResponse>(new Error("Import.NotFound", "Importação não encontrada."));

        var transactions = await _importedTxRepository.GetByImportIdAsync(id, ct);

        var alreadyCreated = transactions.Count(t => t.CreatedTransactionId != null || t.CreatedExpenseId != null || t.CreatedIncomeId != null);

        var response = new StatementImportPostPreviewResponse(
            Total: transactions.Count(),
            ExpensesToCreate: 0, // Legacy — mantido para compatibilidade
            IncomesToCreate: 0,  // Legacy — mantido para compatibilidade
            TransactionsToCreate: transactions.Count(t =>
                t.CreatedTransactionId == null && t.CreatedExpenseId == null && t.CreatedIncomeId == null
                && t.Status != ImportTransactionStatus.Skipped && t.Amount != 0),
            AlreadyCreated: alreadyCreated,
            ToIgnore: transactions.Count(t => t.Amount == 0 || t.Status == ImportTransactionStatus.Skipped),
            Failed: transactions.Count(t => t.Status == ImportTransactionStatus.Error)
        );

        return Result<StatementImportPostPreviewResponse>.Success(response);
    }

    /// <summary>
    /// Posta as transações importadas, criando entidades Transaction unificadas.
    /// Detecta tipo (Income/Expense) pelo sinal do Amount.
    /// Transferências podem ser reclassificadas depois pelo usuário.
    /// </summary>
    public async Task<Result<StatementImportPostResponse>> PostAsync(Guid id, StatementImportPostRequest request, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
            return Result.Failure<StatementImportPostResponse>(new Error("Import.NotFound", "Importação não encontrada."));

        if (import.Status != ImportStatus.Completed)
            return Result.Failure<StatementImportPostResponse>(new Error("Import.InvalidStatus", "Apenas importações concluídas podem ser postadas."));

        if (import.ImportType == StatementImportType.FinancialAccount && !import.FinancialAccountId.HasValue)
            return Result.Failure<StatementImportPostResponse>(new Error("Import.MissingAccount", "Conta financeira não vinculada à importação."));

        if (import.ImportType == StatementImportType.CreditCard && !import.CreditCardId.HasValue)
            return Result.Failure<StatementImportPostResponse>(new Error("Import.MissingCard", "Cartão de crédito não vinculado à importação."));

        FinancialAccount? financialAccount = null;
        if (import.FinancialAccountId.HasValue)
        {
            financialAccount = await _accountRepository.GetByIdAndUserAsync(import.FinancialAccountId.Value, userId, ct);
            if (financialAccount == null)
                return Result.Failure<StatementImportPostResponse>(new Error("Import.AccountNotFound", "Conta financeira não encontrada."));
        }

        var importedTransactions = (await _importedTxRepository.GetByImportIdAsync(id, ct)).ToList();

        // Default categories (seed data)
        var defaultExpenseCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000014"); // Não Categorizado
        var defaultIncomeCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008");  // Outros

        // Fetch existing transactions for duplicate detection
        var existingTransactions = await _transactionRepository.GetAllByUserIdAsync(userId, ct);

        int createdTransactions = 0, skipped = 0, failed = 0;

        foreach (var importedTx in importedTransactions)
        {
            // Idempotency: already processed
            if (importedTx.CreatedTransactionId != null || importedTx.CreatedExpenseId != null || importedTx.CreatedIncomeId != null)
            { skipped++; continue; }

            if (importedTx.Status == ImportTransactionStatus.Skipped || importedTx.Amount == 0)
            {
                if (importedTx.Amount == 0 && importedTx.Status != ImportTransactionStatus.Skipped)
                    importedTx.MarkAsSkipped("Valor zero");
                skipped++; continue;
            }

            if (importedTx.Status == ImportTransactionStatus.Error && !request.Force)
            { failed++; continue; }

            try
            {
                var amount = Math.Abs(importedTx.Amount);
                var isExpense = importedTx.Amount < 0;
                var isCreditCard = import.ImportType == StatementImportType.CreditCard;

                // Determine transaction type
                var txType = isExpense ? TransactionType.Expense : TransactionType.Income;

                // Determine raw bank type
                var rawBankType = isExpense
                    ? (isCreditCard ? RawBankType.CreditPurchase : RawBankType.DebitPurchase)
                    : (isCreditCard ? RawBankType.Refund : RawBankType.Deposit);

                // Determine payment method
                var paymentMethod = isCreditCard ? PaymentMethod.CreditCard : PaymentMethod.BankTransfer;

                // Determine category
                var categoryId = isExpense ? defaultExpenseCategoryId : defaultIncomeCategoryId;

                // Determine status
                var status = isExpense
                    ? (isCreditCard ? TransactionStatus.Pending : TransactionStatus.Paid)
                    : TransactionStatus.Paid;

                // Credit card income (refund) — skip if CC
                if (!isExpense && isCreditCard)
                {
                    importedTx.MarkAsSkipped("Receitas em cartão de crédito não suportadas automaticamente ainda.");
                    skipped++; continue;
                }

                // Duplicate detection
                var isDuplicate = existingTransactions.Any(t =>
                    t.Amount == amount &&
                    t.Date.Date == importedTx.TransactionDate.Date &&
                    t.Description == importedTx.RawDescription &&
                    t.Type == txType &&
                    ((!isCreditCard && t.FinancialAccountId == import.FinancialAccountId) ||
                     (isCreditCard && t.CreditCardId == import.CreditCardId)));

                if (isDuplicate)
                {
                    importedTx.MarkAsSkipped(isExpense
                        ? "Já existe uma despesa idêntica registrada"
                        : "Já existe uma receita idêntica registrada");
                    skipped++; continue;
                }

                // Create unified Transaction
                var transaction = Transaction.CreateFromImport(
                    description: importedTx.RawDescription,
                    amount: amount,
                    date: importedTx.TransactionDate,
                    type: txType,
                    paymentMethod: paymentMethod,
                    categoryId: categoryId,
                    financialAccountId: isCreditCard ? null : import.FinancialAccountId,
                    creditCardId: isCreditCard ? import.CreditCardId : null,
                    creditCardInvoiceId: null,
                    userId: userId,
                    rawDescription: importedTx.RawDescription,
                    rawBankType: rawBankType,
                    status: status);

                // Apply balance impact (only non-CC expenses debit the account)
                if (financialAccount != null)
                {
                    if (txType == TransactionType.Income)
                        financialAccount.Credit(amount);
                    else if (txType == TransactionType.Expense && !isCreditCard)
                        financialAccount.Debit(amount);
                }

                await _transactionRepository.AddAsync(transaction, ct);
                importedTx.MarkAsCreated(transaction.Id);
                createdTransactions++;
            }
            catch (Exception ex)
            {
                importedTx.MarkAsError(ex.Message);
                failed++;
            }
        }

        // Update import stats
        import.Complete(import.TotalRecords, import.ProcessedRecords + createdTransactions, import.FailedRecords + failed);

        if (financialAccount != null)
            await _accountRepository.UpdateAsync(financialAccount, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<StatementImportPostResponse>.Success(new StatementImportPostResponse(
            CreatedExpenses: 0,       // Legacy — mantido para compatibilidade
            CreatedIncomes: 0,        // Legacy — mantido para compatibilidade
            CreatedTransactions: createdTransactions,
            Skipped: skipped,
            Failed: failed
        ));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
            return Result.Failure(new Error("Import.NotFound", "Importação não encontrada."));

        var transactions = await _importedTxRepository.GetByImportIdAsync(id, ct);

        if (transactions.Any(t => t.CreatedTransactionId != null || t.CreatedExpenseId != null || t.CreatedIncomeId != null))
            return Result.Failure(new Error("Import.AlreadyPosted", "Não é possível excluir uma importação que já possui lançamentos postados."));

        await _importRepository.DeleteAsync(import, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
