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
/// - "CreditCardSkipped": template uses CreditCard payment but has no CreditCardId set.
/// - "NoInvoiceFound": CreditCard template found but no invoice exists for the target month/group.
/// - "MissingAccount": template has no FinancialAccountId (data integrity issue).
/// - "InvalidAccount": account not found or inactive.
/// - "UnsupportedType": template Type isn't Income or Expense.
/// - "BeforeStartDate": the clamped target day falls before the template's StartDate
///   (e.g. template starts on the 15th but DayOfMonth=10 — the first valid occurrence is the
///   following month). Mirrors RecurringTransaction.GetNextOccurrences gating.
///
/// HasVariableAmount: surfaced from the source template so the UI can prompt the user to confirm
/// the actual value (e.g. condomínio with extra fees, dollarized salary, work-day-based pay).
/// </remarks>
public record CopyRecurringItem(
    Guid TemplateId,
    string Description,
    decimal Amount,
    Guid? CreatedTransactionId,
    string? SkipReason,
    bool HasVariableAmount = false);
