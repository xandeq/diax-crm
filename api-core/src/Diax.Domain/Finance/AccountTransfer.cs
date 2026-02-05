using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Representa uma transferência entre duas contas financeiras
/// </summary>
public class AccountTransfer : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public Guid FromFinancialAccountId { get; private set; }
    public virtual FinancialAccount? FromFinancialAccount { get; private set; }

    public Guid ToFinancialAccountId { get; private set; }
    public virtual FinancialAccount? ToFinancialAccount { get; private set; }

    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; } = string.Empty;

    protected AccountTransfer() { }

    public AccountTransfer(
        Guid fromFinancialAccountId,
        Guid toFinancialAccountId,
        decimal amount,
        DateTime date,
        string description,
        Guid userId)
    {
        if (fromFinancialAccountId == Guid.Empty)
            throw new ArgumentException("Source account cannot be empty", nameof(fromFinancialAccountId));

        if (toFinancialAccountId == Guid.Empty)
            throw new ArgumentException("Destination account cannot be empty", nameof(toFinancialAccountId));

        if (fromFinancialAccountId == toFinancialAccountId)
            throw new ArgumentException("Source and destination accounts must be different");

        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        FromFinancialAccountId = fromFinancialAccountId;
        ToFinancialAccountId = toFinancialAccountId;
        Amount = amount;
        Date = date;
        Description = description;
        UserId = userId;
    }

    public void Update(
        Guid fromFinancialAccountId,
        Guid toFinancialAccountId,
        decimal amount,
        DateTime date,
        string description)
    {
        if (fromFinancialAccountId == Guid.Empty)
            throw new ArgumentException("Source account cannot be empty", nameof(fromFinancialAccountId));

        if (toFinancialAccountId == Guid.Empty)
            throw new ArgumentException("Destination account cannot be empty", nameof(toFinancialAccountId));

        if (fromFinancialAccountId == toFinancialAccountId)
            throw new ArgumentException("Source and destination accounts must be different");

        if (amount <= 0)
            throw new ArgumentException("Transfer amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        FromFinancialAccountId = fromFinancialAccountId;
        ToFinancialAccountId = toFinancialAccountId;
        Amount = amount;
        Date = date;
        Description = description;
    }
}
