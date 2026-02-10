using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class StatementImportService
{
    private readonly IStatementImportRepository _importRepository;
    private readonly IImportedTransactionRepository _transactionRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly ICreditCardRepository _creditCardRepository; // New dependency
    private readonly IEnumerable<IFileParser> _parsers;
    private readonly IUnitOfWork _unitOfWork;

    public StatementImportService(
        IStatementImportRepository importRepository,
        IImportedTransactionRepository transactionRepository,
        IExpenseRepository expenseRepository,
        IIncomeRepository incomeRepository,
        IFinancialAccountRepository accountRepository,
        ICreditCardRepository creditCardRepository, // New dependency
        IEnumerable<IFileParser> parsers,
        IUnitOfWork unitOfWork)
    {
        _importRepository = importRepository;
        _transactionRepository = transactionRepository;
        _expenseRepository = expenseRepository;
        _incomeRepository = incomeRepository;
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

            await _transactionRepository.AddRangeAsync(transactions, ct);

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

        var transactions = await _transactionRepository.GetByImportIdAsync(id, ct);

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
                t.ErrorMessage
            ))
        );

        return Result<StatementImportDetailResponse>.Success(response);
    }

    public async Task<Result<StatementImportPostPreviewResponse>> PreviewPostAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
        {
            return Result.Failure<StatementImportPostPreviewResponse>(new Error("Import.NotFound", "Importação não encontrada."));
        }

        var transactions = await _transactionRepository.GetByImportIdAsync(id, ct);

        var response = new StatementImportPostPreviewResponse(
            Total: transactions.Count(),
            ExpensesToCreate: transactions.Count(t => t.Amount < 0 && t.CreatedExpenseId == null && t.Status != ImportTransactionStatus.Skipped),
            IncomesToCreate: transactions.Count(t => t.Amount > 0 && t.CreatedIncomeId == null && t.Status != ImportTransactionStatus.Skipped),
            AlreadyCreated: transactions.Count(t => t.CreatedExpenseId != null || t.CreatedIncomeId != null),
            ToIgnore: transactions.Count(t => t.Amount == 0 || t.Status == ImportTransactionStatus.Skipped),
            Failed: transactions.Count(t => t.Status == ImportTransactionStatus.Error)
        );

        return Result<StatementImportPostPreviewResponse>.Success(response);
    }

    public async Task<Result<StatementImportPostResponse>> PostAsync(Guid id, StatementImportPostRequest request, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
        {
            return Result.Failure<StatementImportPostResponse>(new Error("Import.NotFound", "Importação não encontrada."));
        }

        if (import.Status != ImportStatus.Completed)
        {
            return Result.Failure<StatementImportPostResponse>(new Error("Import.InvalidStatus", "Apenas importações concluídas podem ser postadas."));
        }

        if (import.ImportType == StatementImportType.FinancialAccount && !import.FinancialAccountId.HasValue)
        {
            return Result.Failure<StatementImportPostResponse>(new Error("Import.MissingAccount", "Conta financeira não vinculada à importação."));
        }

        if (import.ImportType == StatementImportType.CreditCard && !import.CreditCardId.HasValue)
        {
             return Result.Failure<StatementImportPostResponse>(new Error("Import.MissingCard", "Cartão de crédito não vinculado à importação."));
        }

        FinancialAccount? financialAccount = null;
        if (import.FinancialAccountId.HasValue)
        {
            financialAccount = await _accountRepository.GetByIdAndUserAsync(import.FinancialAccountId.Value, userId, ct);
            if (financialAccount == null)
            {
                return Result.Failure<StatementImportPostResponse>(new Error("Import.AccountNotFound", "Conta financeira não encontrada."));
            }
        }

        var transactions = (await _transactionRepository.GetByImportIdAsync(id, ct)).ToList();

        int createdExpenses = 0;
        int createdIncomes = 0;
        int skipped = 0;
        int failed = 0;

        // Default categories from seed data
        var defaultExpenseCategoryId = Guid.Parse("20000000-0000-0000-0000-000000000014"); // Não Categorizado
        var defaultIncomeCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000008"); // Outros

        // Fetch relevant records for idempotency check
        var existingIncomesAll = await _incomeRepository.GetAllByUserIdAsync(userId, ct);
        var existingExpensesAll = await _expenseRepository.GetAllByUserIdAsync(userId, ct);

        foreach (var transaction in transactions)
        {
            // Idempotency check (already processed in this import)
            if (transaction.CreatedExpenseId != null || transaction.CreatedIncomeId != null)
            {
                skipped++;
                continue;
            }

            if (transaction.Status == ImportTransactionStatus.Skipped || transaction.Amount == 0)
            {
                if (transaction.Amount == 0 && transaction.Status != ImportTransactionStatus.Skipped)
                {
                    transaction.MarkAsSkipped("Valor zero");
                }
                skipped++;
                continue;
            }

            if (transaction.Status == ImportTransactionStatus.Error && !request.Force)
            {
                failed++;
                continue;
            }

            try
            {
                if (transaction.Amount < 0)
                {
                    var amount = Math.Abs(transaction.Amount);

                    // Check for duplicate expense
                    var isDuplicate = existingExpensesAll.Any(e =>
                        e.Amount == amount &&
                        e.Date.Date == transaction.TransactionDate.Date &&
                        e.Description == transaction.RawDescription &&
                        (
                            (import.ImportType == StatementImportType.FinancialAccount && e.FinancialAccountId == import.FinancialAccountId) ||
                            (import.ImportType == StatementImportType.CreditCard && e.CreditCardId == import.CreditCardId)
                        ));

                    if (isDuplicate)
                    {
                        transaction.MarkAsSkipped("Já existe uma despesa idêntica registrada");
                        skipped++;
                        continue;
                    }

                    // Create Expense
                    var expense = new Expense(
                        description: transaction.RawDescription,
                        amount: amount,
                        date: transaction.TransactionDate,
                        paymentMethod: import.ImportType == StatementImportType.CreditCard ? PaymentMethod.CreditCard : PaymentMethod.BankTransfer,
                        expenseCategoryId: defaultExpenseCategoryId,
                        isRecurring: false,
                        userId: userId,
                        financialAccountId: import.ImportType == StatementImportType.FinancialAccount ? import.FinancialAccountId : null,
                        creditCardId: import.ImportType == StatementImportType.CreditCard ? import.CreditCardId : null,
                        status: import.ImportType == StatementImportType.CreditCard ? ExpenseStatus.Pending : ExpenseStatus.Paid, // Credit Card expenses are pending invoice payment
                        paidDate: import.ImportType == StatementImportType.CreditCard ? null : transaction.TransactionDate
                    );

                    if (financialAccount != null)
                    {
                        financialAccount.Debit(amount);
                    }

                    await _expenseRepository.AddAsync(expense, ct);
                    transaction.MarkAsCreated(expenseId: expense.Id);
                    createdExpenses++;
                }
                else
                {
                    // Income on Credit Card? Usually a refund or payment.
                    // For now, let's treat it as Income but loosely.
                    // However, Income entities usually require FinancialAccountId.
                    // If it's a Credit Card, we might not be able to create an Income easily unless we support CreditCard Incomes (refunds).
                    // Current Income entity: financialAccountId is required? Let's check.
                    // Checking Income.cs...
                    // For now, if it's Credit Card, we might skip Incomes or handle them differently.
                    // Let's assume for now we only support Expenses for Credit Cards or block Incomes if no Financial Account.

                    if (import.ImportType == StatementImportType.CreditCard)
                    {
                         transaction.MarkAsSkipped("Receitas em cartão de crédito não suportadas automaticamente ainda.");
                         skipped++;
                         continue;
                    }

                    // Check for duplicate income
                    var isDuplicate = existingIncomesAll.Any(i =>
                        i.Amount == transaction.Amount &&
                        i.Date.Date == transaction.TransactionDate.Date &&
                        i.Description == transaction.RawDescription &&
                        i.FinancialAccountId == financialAccount!.Id);

                    if (isDuplicate)
                    {
                        transaction.MarkAsSkipped("Já existe uma receita idêntica registrada");
                        skipped++;
                        continue;
                    }

                    // Create Income
                    var income = new Income(
                        description: transaction.RawDescription,
                        amount: transaction.Amount,
                        date: transaction.TransactionDate,
                        paymentMethod: PaymentMethod.Pix,
                        incomeCategoryId: defaultIncomeCategoryId,
                        isRecurring: false,
                        financialAccountId: financialAccount!.Id,
                        userId: userId
                    );

                    financialAccount!.Credit(transaction.Amount);
                    await _incomeRepository.AddAsync(income, ct);
                    transaction.MarkAsCreated(incomeId: income.Id);
                    createdIncomes++;
                }
            }
            catch (Exception ex)
            {
                transaction.MarkAsError(ex.Message);
                failed++;
            }
        }

        // Update import stats
        import.Complete(import.TotalRecords, import.ProcessedRecords + createdExpenses + createdIncomes, import.FailedRecords + failed);

        if (financialAccount != null)
        {
            await _accountRepository.UpdateAsync(financialAccount, ct);
        }
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<StatementImportPostResponse>.Success(new StatementImportPostResponse(
            CreatedExpenses: createdExpenses,
            CreatedIncomes: createdIncomes,
            Skipped: skipped,
            Failed: failed
        ));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var import = await _importRepository.GetByIdAndUserAsync(id, userId, ct);
        if (import == null)
        {
            return Result.Failure(new Error("Import.NotFound", "Importação não encontrada."));
        }

        var transactions = await _transactionRepository.GetByImportIdAsync(id, ct);

        if (transactions.Any(t => t.CreatedExpenseId != null || t.CreatedIncomeId != null))
        {
            return Result.Failure(new Error("Import.AlreadyPosted", "Não é possível excluir uma importação que já possui lançamentos postados."));
        }

        await _importRepository.DeleteAsync(import, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
