using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public enum ProposalStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Paid = 3,
    Cancelled = 4,
}

public enum ProposalOrigin
{
    Manual = 0,
    Bot = 1,
}

/// <summary>
/// Proposta comercial vinculada a um lead/cliente do pipeline.
/// Tem um link público (token) onde o cliente lê, aceita e paga via PIX.
/// Quando marcada como paga, o lead é convertido em cliente.
/// </summary>
public class Proposal : AuditableEntity, IUserOwnedEntity
{
    public Guid CustomerId { get; private set; }
    public Guid UserId { get; set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>Escopo/descrição do serviço (texto livre, suporta markdown simples).</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Valor total da proposta em R$.</summary>
    public decimal Amount { get; private set; }

    public ProposalStatus Status { get; private set; } = ProposalStatus.Draft;

    /// <summary>Token opaco do link público (não enumerável).</summary>
    public string PublicToken { get; private set; } = string.Empty;

    /// <summary>Chave PIX do emissor (CPF/CNPJ/email/telefone/aleatória) usada no BR Code.</summary>
    public string? PixKey { get; private set; }

    public DateTime? ValidUntil { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    /// <summary>Quantas vezes o link público foi aberto.</summary>
    public int ViewCount { get; private set; }

    /// <summary>Capa gerada por IA para a página pública (URL durável em /generated-media).</summary>
    public string? CoverImageUrl { get; private set; }

    public void SetCoverImage(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            CoverImageUrl = url.Length <= 500 ? url : url[..500];
    }

    /// <summary>Pacote do catálogo que originou a proposta (opcional).</summary>
    public Guid? PackageId { get; private set; }

    public ProposalOrigin Origin { get; private set; } = ProposalOrigin.Manual;

    /// <summary>Maior etapa de follow-up já gerada (0 = nenhuma). Ver ProposalFollowUpService.</summary>
    public int LastFollowUpStage { get; private set; }

    public bool IsOpen => Status is ProposalStatus.Draft or ProposalStatus.Sent or ProposalStatus.Accepted;

    public void LinkPackage(Guid packageId, ProposalOrigin origin)
    {
        PackageId = packageId;
        Origin = origin;
    }

    public void SetOrigin(ProposalOrigin origin) => Origin = origin;

    /// <summary>Retorna true se a etapa é nova (avança); false se já foi gerada.</summary>
    public bool RegisterFollowUpStage(int stage)
    {
        if (stage <= LastFollowUpStage) return false;
        LastFollowUpStage = stage;
        return true;
    }

    protected Proposal() { } // EF Core

    public Proposal(
        Guid customerId,
        Guid userId,
        string title,
        string description,
        decimal amount,
        string? pixKey,
        DateTime? validUntil)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Título da proposta é obrigatório.");
        if (amount <= 0)
            throw new ArgumentException("Valor da proposta deve ser maior que zero.");
        if (amount > 100_000_000)
            throw new ArgumentException("Valor da proposta acima do limite permitido.");

        CustomerId = customerId;
        UserId = userId;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        Amount = amount;
        PixKey = string.IsNullOrWhiteSpace(pixKey) ? null : pixKey.Trim();
        ValidUntil = validUntil;
        Status = ProposalStatus.Draft;
        PublicToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..8]; // 40 chars
    }

    public bool IsExpired => ValidUntil.HasValue && DateTime.UtcNow > ValidUntil.Value;

    public void MarkSent()
    {
        if (Status == ProposalStatus.Draft)
        {
            Status = ProposalStatus.Sent;
            SentAt = DateTime.UtcNow;
        }
    }

    public void RegisterView()
    {
        ViewCount++;
        // Abrir o link já caracteriza envio efetivo
        if (Status == ProposalStatus.Draft)
            MarkSent();
    }

    public void Accept()
    {
        if (Status is ProposalStatus.Paid or ProposalStatus.Cancelled)
            throw new InvalidOperationException($"Proposta em status '{Status}' não pode ser aceita.");
        if (IsExpired)
            throw new InvalidOperationException("Proposta expirada — gere uma nova proposta.");

        if (Status != ProposalStatus.Accepted)
        {
            Status = ProposalStatus.Accepted;
            AcceptedAt = DateTime.UtcNow;
        }
    }

    public void MarkPaid()
    {
        if (Status == ProposalStatus.Cancelled)
            throw new InvalidOperationException("Proposta cancelada não pode ser marcada como paga.");
        if (Status != ProposalStatus.Paid)
        {
            Status = ProposalStatus.Paid;
            PaidAt = DateTime.UtcNow;
            AcceptedAt ??= PaidAt;
        }
    }

    public void Cancel()
    {
        if (Status == ProposalStatus.Paid)
            throw new InvalidOperationException("Proposta paga não pode ser cancelada.");
        Status = ProposalStatus.Cancelled;
    }

    /// <summary>
    /// Nova validade na MESMA proposta (o link que o cliente já recebeu volta a valer, com PIX).
    /// Se o aviso de "vencida" (etapa 3) já saiu, volta para a etapa 2 para avisar de novo no próximo vencimento.
    /// </summary>
    public void Renew(DateTime validUntil)
    {
        if (Status is ProposalStatus.Paid or ProposalStatus.Cancelled)
            throw new InvalidOperationException($"Proposta em status '{Status}' não pode ser renovada.");
        if (validUntil <= DateTime.UtcNow)
            throw new InvalidOperationException("A nova validade precisa ser no futuro.");
        ValidUntil = validUntil;
        if (LastFollowUpStage == 3) LastFollowUpStage = 2;
    }
}
