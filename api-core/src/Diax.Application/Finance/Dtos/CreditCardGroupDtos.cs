namespace Diax.Application.Finance.Dtos;

public class CreateCreditCardGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Bank { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal SharedLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCreditCardGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Bank { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal SharedLimit { get; set; }
    public bool IsActive { get; set; }
}

public class CreditCardGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bank { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal SharedLimit { get; set; }
    public bool IsActive { get; set; }
    public decimal TotalCardLimits { get; set; }
    public decimal AvailableLimit { get; set; }
    public int CardCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
