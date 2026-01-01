using Diax.Domain.Common;

namespace Diax.Domain.Finance;

public class CreditCard : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string LastFourDigits { get; private set; } = string.Empty;
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }

    public CreditCard(string name, string lastFourDigits, decimal limit, int closingDay, int dueDay)
    {
        Name = name;
        LastFourDigits = lastFourDigits;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
    }

    public void Update(string name, string lastFourDigits, decimal limit, int closingDay, int dueDay)
    {
        Name = name;
        LastFourDigits = lastFourDigits;
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
    }
}
