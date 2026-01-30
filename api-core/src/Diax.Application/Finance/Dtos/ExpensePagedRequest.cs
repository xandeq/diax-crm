using Diax.Shared.Models;
using Diax.Domain.Finance;

namespace Diax.Application.Finance.Dtos;

public class ExpensePagedRequest : PagedRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public ExpenseStatus? Status { get; set; }
}
