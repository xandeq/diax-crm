using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class CreditCardGroup : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Bank { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public decimal SharedLimit { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public ICollection<CreditCard> Cards { get; private set; } = new List<CreditCard>();
    public ICollection<CreditCardInvoice> Invoices { get; private set; } = new List<CreditCardInvoice>();

    // Parameterless constructor for EF
    private CreditCardGroup() { }

    public CreditCardGroup(
        string name,
        string? bank,
        int closingDay,
        int dueDay,
        decimal sharedLimit,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Card group name cannot be empty", nameof(name));

        if (closingDay < 1 || closingDay > 31)
            throw new ArgumentException("Closing day must be between 1 and 31", nameof(closingDay));

        if (dueDay < 1 || dueDay > 31)
            throw new ArgumentException("Due day must be between 1 and 31", nameof(dueDay));

        if (sharedLimit < 0)
            throw new ArgumentException("Shared limit cannot be negative", nameof(sharedLimit));

        Name = name;
        Bank = bank;
        ClosingDay = closingDay;
        DueDay = dueDay;
        SharedLimit = sharedLimit;
        IsActive = isActive;
    }

    public void Update(
        string name,
        string? bank,
        int closingDay,
        int dueDay,
        decimal sharedLimit,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Card group name cannot be empty", nameof(name));

        if (closingDay < 1 || closingDay > 31)
            throw new ArgumentException("Closing day must be between 1 and 31", nameof(closingDay));

        if (dueDay < 1 || dueDay > 31)
            throw new ArgumentException("Due day must be between 1 and 31", nameof(dueDay));

        if (sharedLimit < 0)
            throw new ArgumentException("Shared limit cannot be negative", nameof(sharedLimit));

        Name = name;
        Bank = bank;
        ClosingDay = closingDay;
        DueDay = dueDay;
        SharedLimit = sharedLimit;
        IsActive = isActive;
    }

    public decimal GetTotalCardLimits()
    {
        return Cards.Where(c => c.IsActive).Sum(c => c.Limit);
    }

    public decimal GetAvailableLimit()
    {
        var totalUsed = Cards.Where(c => c.IsActive).Sum(c => c.Limit);
        return SharedLimit - totalUsed;
    }
}
