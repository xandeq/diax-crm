namespace Diax.Application.Finance.Dtos;

public class FinancialSummaryRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class FinancialSummaryResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalPaidExpenses { get; set; }
    public decimal TotalPendingExpenses { get; set; }
    public decimal PendingCash { get; set; }  // Pending expenses paid with cash/pix/debit
    public decimal PendingCredit { get; set; }  // Unpaid credit card invoices
    public decimal NetCashFlow { get; set; }  // Income - PaidExpenses
    public decimal ProjectedCashFlow { get; set; }  // Income - AllExpenses
    public int TotalExpenseCount { get; set; }
    public int PaidExpenseCount { get; set; }
    public int PendingExpenseCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
}
