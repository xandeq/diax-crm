using Diax.Application.Common;
using Diax.Application.Finance.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance;
using Diax.Shared.Results;

namespace Diax.Application.Finance;

public class CreditCardInvoiceService : IApplicationService
{
    private readonly ICreditCardInvoiceRepository _invoiceRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ICreditCardGroupRepository _creditCardGroupRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardInvoiceService(
        ICreditCardInvoiceRepository invoiceRepository,
        ICreditCardRepository creditCardRepository,
        ICreditCardGroupRepository creditCardGroupRepository,
        IExpenseRepository expenseRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _creditCardRepository = creditCardRepository;
        _creditCardGroupRepository = creditCardGroupRepository;
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var response = invoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetByCreditCardIdAsync(Guid creditCardId, Guid userId, CancellationToken cancellationToken = default)
    {
        // First verify the card belongs to the user
        var card = await _creditCardRepository.GetByIdAndUserAsync(creditCardId, userId, cancellationToken);
        if (card == null)
        {
            return Result.Failure<IEnumerable<CreditCardInvoiceResponse>>(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        var invoices = await _invoiceRepository.GetByCreditCardIdAsync(creditCardId, cancellationToken);
        var response = invoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetUnpaidInvoicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetAllByUserIdAsync(userId, cancellationToken);
        var unpaidInvoices = invoices.Where(i => !i.IsPaid);
        var response = unpaidInvoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<CreditCardInvoiceResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure<CreditCardInvoiceResponse>(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }
        return Result<CreditCardInvoiceResponse>.Success(MapToResponse(invoice));
    }

    public async Task<Result<Guid>> CreateOrGetInvoiceAsync(CreateCreditCardInvoiceRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        Guid groupId;

        // Resolve group ID from either CreditCardGroupId or CreditCardId
        if (request.CreditCardGroupId.HasValue)
        {
            groupId = request.CreditCardGroupId.Value;
        }
        else if (request.CreditCardId.HasValue)
        {
            // Lookup card to get its group
            var card = await _creditCardRepository.GetByIdAndUserAsync(request.CreditCardId.Value, userId, cancellationToken);
            if (card == null)
            {
                return Result.Failure<Guid>(new Error("CreditCard.NotFound", "Credit card not found"));
            }
            if (!card.CreditCardGroupId.HasValue)
            {
                return Result.Failure<Guid>(new Error("CreditCard.NoGroup", "Credit card is not associated with a group"));
            }
            groupId = card.CreditCardGroupId.Value;
        }
        else
        {
            return Result.Failure<Guid>(new Error("CreditCardInvoice.InvalidRequest", "Either CreditCardId or CreditCardGroupId must be provided"));
        }

        // Verify group exists and belongs to user
        var group = await _creditCardGroupRepository.GetByIdAndUserAsync(groupId, userId, cancellationToken);
        if (group == null)
        {
            return Result.Failure<Guid>(new Error("CreditCardGroup.NotFound", "Credit card group not found"));
        }

        // Check if invoice already exists for this group and period
        var existingInvoice = await _invoiceRepository.GetByGroupAndPeriodAsync(
            groupId,
            request.ReferenceMonth,
            request.ReferenceYear,
            cancellationToken);

        if (existingInvoice != null)
        {
            return Result<Guid>.Success(existingInvoice.Id);
        }

        // Calculate closing and due dates using group's dates
        var closingDate = CalculateClosingDate(request.ReferenceYear, request.ReferenceMonth, group.ClosingDay);
        var dueDate = CalculateDueDate(closingDate, group.DueDay);

        var invoice = new CreditCardInvoice(
            groupId,
            request.ReferenceMonth,
            request.ReferenceYear,
            closingDate,
            dueDate,
            userId
        );

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }

    public async Task<Result<Guid>> GetOrCreateInvoiceForExpenseAsync(Guid creditCardId, Guid userId, DateTime expenseDate, CancellationToken cancellationToken = default)
    {
        var creditCard = await _creditCardRepository.GetByIdAndUserAsync(creditCardId, userId, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure<Guid>(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        if (!creditCard.CreditCardGroupId.HasValue)
        {
            return Result.Failure<Guid>(new Error("CreditCard.NoGroup", "Credit card is not associated with a group"));
        }

        var group = await _creditCardGroupRepository.GetByIdAndUserAsync(creditCard.CreditCardGroupId.Value, userId, cancellationToken);
        if (group == null)
        {
            return Result.Failure<Guid>(new Error("CreditCardGroup.NotFound", "Credit card group not found"));
        }

        // Determine which invoice period this expense belongs to
        var (month, year) = CalculateInvoicePeriod(expenseDate, group.ClosingDay);

        var request = new CreateCreditCardInvoiceRequest(null, group.Id, month, year);
        return await CreateOrGetInvoiceAsync(request, userId, cancellationToken);
    }

    public async Task<Result> PayInvoiceAsync(Guid invoiceId, PayCreditCardInvoiceRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAndUserAsync(invoiceId, userId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }

        invoice.MarkAsPaid(request.PaymentDate, request.PaidFromAccountId, request.StatementAmount);

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UnpayInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAndUserAsync(invoiceId, userId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }

        invoice.MarkAsUnpaid();

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetStatementAmountAsync(Guid invoiceId, decimal? amount, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAndUserAsync(invoiceId, userId, cancellationToken);
        if (invoice == null)
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));

        invoice.SetStatementAmount(amount);
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAndUserAsync(id, userId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }

        await _invoiceRepository.DeleteAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Calculates which invoice period an expense belongs to based on expense date and card closing day
    /// </summary>
    private static (int Month, int Year) CalculateInvoicePeriod(DateTime expenseDate, int closingDay)
    {
        // If expense is after closing day, it belongs to next month's invoice
        if (expenseDate.Day > closingDay)
        {
            var nextMonth = expenseDate.AddMonths(1);
            return (nextMonth.Month, nextMonth.Year);
        }

        // Otherwise, it belongs to current month's invoice
        return (expenseDate.Month, expenseDate.Year);
    }

    private static DateTime CalculateClosingDate(int year, int month, int closingDay)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var actualClosingDay = Math.Min(closingDay, daysInMonth);
        return new DateTime(year, month, actualClosingDay);
    }

    private static DateTime CalculateDueDate(DateTime closingDate, int dueDay)
    {
        var nextMonth = closingDate.AddMonths(1);
        var daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var actualDueDay = Math.Min(dueDay, daysInNextMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, actualDueDay);
    }

    private static CreditCardInvoiceResponse MapToResponse(CreditCardInvoice invoice)
    {
        return new CreditCardInvoiceResponse(
            invoice.Id,
            invoice.CreditCardGroupId,
            invoice.CreditCardGroup?.Name ?? string.Empty,
            invoice.ReferenceMonth,
            invoice.ReferenceYear,
            invoice.ClosingDate,
            invoice.DueDate,
            invoice.GetTotalAmount(),
            invoice.IsPaid,
            invoice.PaymentDate,
            invoice.PaidFromAccountId,
            invoice.StatementAmount,
            invoice.CreatedAt,
            invoice.UpdatedAt
        );
    }
}
