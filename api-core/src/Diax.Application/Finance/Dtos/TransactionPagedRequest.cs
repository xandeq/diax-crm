using Diax.Domain.Finance;
using Diax.Shared.Models;

namespace Diax.Application.Finance.Dtos;

public class TransactionPagedRequest : PagedRequest
{
    public TransactionType? Type { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? FinancialAccountId { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public TransactionStatus? Status { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? CreditCardInvoiceId { get; set; }
}
