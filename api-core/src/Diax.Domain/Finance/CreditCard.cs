using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class CreditCard : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string LastFourDigits { get; private set; } = string.Empty;
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }
    public CardBrand Brand { get; private set; }
    public CardKind CardKind { get; private set; }
    public bool IsActive { get; private set; }

    // Foreign key to group (nullable for backward compatibility during migration)
    public Guid? CreditCardGroupId { get; private set; }

    // Navigation property
    public CreditCardGroup? CreditCardGroup { get; private set; }

    public CreditCard(
        string name,
        string lastFourDigits,
        decimal limit,
        int closingDay,
        int dueDay,
        Guid userId,
        CardBrand brand = CardBrand.Unknown,
        CardKind cardKind = CardKind.Physical,
        bool isActive = true,
        Guid? creditCardGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Card name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(lastFourDigits) || lastFourDigits.Length != 4)
            throw new ArgumentException("Last four digits must be 4 characters", nameof(lastFourDigits));

        if (closingDay < 1 || closingDay > 31)
            throw new ArgumentException("Closing day must be between 1 and 31", nameof(closingDay));

        if (dueDay < 1 || dueDay > 31)
            throw new ArgumentException("Due day must be between 1 and 31", nameof(dueDay));

        if (limit < 0)
            throw new ArgumentException("Limit cannot be negative", nameof(limit));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        Name = name;
        LastFourDigits = lastFourDigits;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        UserId = userId;
        Brand = brand;
        CardKind = cardKind;
        IsActive = isActive;
        CreditCardGroupId = creditCardGroupId;
    }

    public void Update(
        string name,
        string lastFourDigits,
        decimal limit,
        int closingDay,
        int dueDay,
        CardBrand brand,
        CardKind cardKind,
        bool isActive,
        Guid? creditCardGroupId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Card name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(lastFourDigits) || lastFourDigits.Length != 4)
            throw new ArgumentException("Last four digits must be 4 characters", nameof(lastFourDigits));

        if (closingDay < 1 || closingDay > 31)
            throw new ArgumentException("Closing day must be between 1 and 31", nameof(closingDay));

        if (dueDay < 1 || dueDay > 31)
            throw new ArgumentException("Due day must be between 1 and 31", nameof(dueDay));

        if (limit < 0)
            throw new ArgumentException("Limit cannot be negative", nameof(limit));

        Name = name;
        LastFourDigits = lastFourDigits;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
        Brand = brand;
        CardKind = cardKind;
        IsActive = isActive;
        CreditCardGroupId = creditCardGroupId;
    }
}
