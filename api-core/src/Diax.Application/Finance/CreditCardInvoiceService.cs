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
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreditCardInvoiceService(
        ICreditCardInvoiceRepository invoiceRepository,
        ICreditCardRepository creditCardRepository,
        IExpenseRepository expenseRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _creditCardRepository = creditCardRepository;
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetAllAsync(cancellationToken);
        var response = invoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetByCreditCardIdAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetByCreditCardIdAsync(creditCardId, cancellationToken);
        var response = invoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<IEnumerable<CreditCardInvoiceResponse>>> GetUnpaidInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceRepository.GetUnpaidInvoicesAsync(cancellationToken);
        var response = invoices.Select(MapToResponse);
        return Result<IEnumerable<CreditCardInvoiceResponse>>.Success(response);
    }

    public async Task<Result<CreditCardInvoiceResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure<CreditCardInvoiceResponse>(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }
        return Result<CreditCardInvoiceResponse>.Success(MapToResponse(invoice));
    }

    public async Task<Result<Guid>> CreateOrGetInvoiceAsync(CreateCreditCardInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        // Verify credit card exists
        var creditCard = await _creditCardRepository.GetByIdAsync(request.CreditCardId, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure<Guid>(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        // Check if invoice already exists for this period
        var existingInvoice = await _invoiceRepository.GetByCardAndPeriodAsync(
            request.CreditCardId,
            request.ReferenceMonth,
            request.ReferenceYear,
            cancellationToken);

        if (existingInvoice != null)
        {
            return Result<Guid>.Success(existingInvoice.Id);
        }

        // Calculate closing and due dates
        var closingDate = CalculateClosingDate(request.ReferenceYear, request.ReferenceMonth, creditCard.ClosingDay);
        var dueDate = CalculateDueDate(closingDate, creditCard.DueDay);

        var invoice = new CreditCardInvoice(
            request.CreditCardId,
            request.ReferenceMonth,
            request.ReferenceYear,
            closingDate,
            dueDate
        );

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }

    public async Task<Result<Guid>> GetOrCreateInvoiceForExpenseAsync(Guid creditCardId, DateTime expenseDate, CancellationToken cancellationToken = default)
    {
        var creditCard = await _creditCardRepository.GetByIdAsync(creditCardId, cancellationToken);
        if (creditCard == null)
        {
            return Result.Failure<Guid>(new Error("CreditCard.NotFound", "Credit card not found"));
        }

        // Determine which invoice period this expense belongs to
        var (month, year) = CalculateInvoicePeriod(expenseDate, creditCard.ClosingDay);

        var request = new CreateCreditCardInvoiceRequest(creditCardId, month, year);
        return await CreateOrGetInvoiceAsync(request, cancellationToken);
    }

    public async Task<Result> PayInvoiceAsync(Guid invoiceId, PayCreditCardInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }

        invoice.MarkAsPaid(request.PaymentDate, request.PaidFromAccountId);

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UnpayInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        if (invoice == null)
        {
            return Result.Failure(new Error("CreditCardInvoice.NotFound", "Invoice not found"));
        }

        invoice.MarkAsUnpaid();

        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
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
            invoice.CreditCardId,
            invoice.CreditCard?.Name ?? string.Empty,
            invoice.ReferenceMonth,
            invoice.ReferenceYear,
            invoice.ClosingDate,
            invoice.DueDate,
            invoice.GetTotalAmount(),
            invoice.IsPaid,
            invoice.PaymentDate,
            invoice.PaidFromAccountId,
            invoice.CreatedAt,
            invoice.UpdatedAt
        );
    }
}
