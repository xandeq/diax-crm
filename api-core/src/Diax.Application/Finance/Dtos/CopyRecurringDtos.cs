namespace Diax.Application.Finance.Dtos;

/// <summary>
/// Result of materialising recurring templates into actual Transactions for a month.
/// </summary>
public record CopyRecurringMonthResult(
    int Year,
    int Month,
    IReadOnlyList<CopyRecurringItem> Created,
    IReadOnlyList<CopyRecurringItem> Skipped);

/// <summary>
/// One row in CopyRecurringMonthResult. SkipReason is null for created entries.
/// </summary>
/// <remarks>
/// SkipReason values:
/// - "AlreadyExists": a Transaction with this RecurringTransactionId already exists for the target month.
/// - "CreditCardSkipped": template uses CreditCard payment; v1 doesn't auto-resolve invoices.
/// - "MissingAccount": template has no FinancialAccountId (data integrity issue).
/// - "InvalidAccount": account not found or inactive.
/// - "UnsupportedType": template Type isn't Income or Expense.
/// </remarks>
public record CopyRecurringItem(
    Guid TemplateId,
    string Description,
    decimal Amount,
    Guid? CreatedTransactionId,
    string? SkipReason);
