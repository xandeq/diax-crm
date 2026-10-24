using Diax.Domain.Common;

namespace Diax.Domain.Customers;

/// <summary>Pacote de serviço com preço fixo — base das propostas (catálogo do dono).</summary>
public class ServicePackage : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    /// <summary>Itens do escopo, um por linha.</summary>
    public string Scope { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int DeliveryDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    protected ServicePackage() { } // EF Core

    public ServicePackage(Guid userId, string name, string shortDescription, string scope,
        decimal price, int deliveryDays, int sortOrder)
    {
        UserId = userId;
        Update(name, shortDescription, scope, price, deliveryDays, sortOrder, true);
    }

    public void Update(string name, string shortDescription, string scope,
        decimal price, int deliveryDays, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome do pacote é obrigatório.");
        if (price <= 0) throw new ArgumentException("Preço do pacote deve ser maior que zero.");
        if (deliveryDays < 0) throw new ArgumentException("Prazo inválido.");
        Name = name.Trim();
        ShortDescription = shortDescription?.Trim() ?? string.Empty;
        Scope = scope?.Trim() ?? string.Empty;
        Price = price;
        DeliveryDays = deliveryDays;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public string ToProposalDescription()
    {
        var itens = Scope.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Select(i => "• " + i);
        var prazo = DeliveryDays > 0 ? $"\n\nPrazo de entrega: {DeliveryDays} dias úteis após aprovação." : "";
        return $"{ShortDescription}\n\n{string.Join('\n', itens)}{prazo}".Trim();
    }
}
