using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public record CreateProposalRequest(
    Guid CustomerId,
    string Title,
    string Description,
    decimal Amount,
    string? PixKey,
    DateTime? ValidUntil);

public record ProposalDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Title,
    string Description,
    decimal Amount,
    ProposalStatus Status,
    string PublicToken,
    string? PixKey,
    DateTime? ValidUntil,
    DateTime? SentAt,
    DateTime? AcceptedAt,
    DateTime? PaidAt,
    int ViewCount,
    DateTime CreatedAt);

/// <summary>Visão pública da proposta (sem dados internos do CRM).</summary>
public record PublicProposalDto(
    string Title,
    string Description,
    decimal Amount,
    string CustomerName,
    ProposalStatus Status,
    bool IsExpired,
    DateTime? ValidUntil,
    DateTime? AcceptedAt,
    /// <summary>PIX copia-e-cola (BR Code) — presente quando há chave PIX configurada.</summary>
    string? PixCopiaECola);

public class ProposalService : IApplicationService
{
    private const string MerchantName = "Alexandre Queiroz";
    private const string MerchantCity = "Vitoria";

    private readonly IProposalRepository _proposalRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(
        IProposalRepository proposalRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProposalService> logger)
    {
        _proposalRepository = proposalRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProposalDto>> CreateAsync(
        CreateProposalRequest request, Guid userId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result.Failure<ProposalDto>(Error.NotFound("Customer", request.CustomerId.ToString()));

        Proposal proposal;
        try
        {
            proposal = new Proposal(
                customer.Id, userId, request.Title, request.Description,
                request.Amount, request.PixKey, request.ValidUntil);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProposalDto>(Error.Validation("Proposal", ex.Message));
        }

        await _proposalRepository.AddAsync(proposal, ct);

        // Enviar proposta caracteriza avanço no funil: Lead/Contacted → Negotiating
        if (customer.Status < Domain.Customers.Enums.CustomerStatus.Negotiating)
            customer.UpdateStatus(Domain.Customers.Enums.CustomerStatus.Negotiating);
        // Sincroniza o valor do negócio no pipeline se ainda não definido
        if (customer.EstimatedValue == null)
            customer.UpdateDealInfo(request.Amount, request.ValidUntil);
        await _customerRepository.UpdateAsync(customer, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Proposal criada: {ProposalId} para {CustomerId} (R$ {Amount})",
            proposal.Id, customer.Id, request.Amount);

        return ToDto(proposal, customer.Name);
    }

    public async Task<List<ProposalDto>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var proposals = await _proposalRepository.GetByUserAsync(userId, 50, ct);
        var customerIds = proposals.Select(p => p.CustomerId).Distinct();
        var customers = (await _customerRepository.GetByIdsAsync(customerIds, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        return proposals
            .Select(p => ToDto(p, customers.GetValueOrDefault(p.CustomerId, "—")))
            .ToList();
    }

    /// <summary>Visão pública por token (AllowAnonymous) — registra a visualização.</summary>
    public async Task<Result<PublicProposalDto>> GetPublicAsync(string token, CancellationToken ct = default)
    {
        var proposal = await FindByTokenAsync(token, ct);
        if (proposal == null)
            return Result.Failure<PublicProposalDto>(Error.NotFound("Proposal", "token"));

        var customer = await _customerRepository.GetByIdAsync(proposal.CustomerId, ct);

        proposal.RegisterView();
        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        string? pix = null;
        if (!string.IsNullOrWhiteSpace(proposal.PixKey) && !proposal.IsExpired
            && proposal.Status is not (ProposalStatus.Paid or ProposalStatus.Cancelled))
        {
            try
            {
                pix = PixBrCode.Generate(
                    proposal.PixKey, proposal.Amount, MerchantName, MerchantCity,
                    proposal.Id.ToString("N")[..25]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gerar PIX BR Code da proposta {ProposalId}", proposal.Id);
            }
        }

        return new PublicProposalDto(
            Title: proposal.Title,
            Description: proposal.Description,
            Amount: proposal.Amount,
            CustomerName: customer?.Name ?? "Cliente",
            Status: proposal.Status,
            IsExpired: proposal.IsExpired,
            ValidUntil: proposal.ValidUntil,
            AcceptedAt: proposal.AcceptedAt,
            PixCopiaECola: pix);
    }

    /// <summary>Aceite pelo link público (anônimo, por token não-enumerável).</summary>
    public async Task<Result<PublicProposalDto>> AcceptPublicAsync(string token, CancellationToken ct = default)
    {
        var proposal = await FindByTokenAsync(token, ct);
        if (proposal == null)
            return Result.Failure<PublicProposalDto>(Error.NotFound("Proposal", "token"));

        try
        {
            proposal.Accept();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PublicProposalDto>(Error.Validation("Proposal", ex.Message));
        }

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Proposal ACEITA pelo cliente: {ProposalId}", proposal.Id);
        return await GetPublicAsync(token, ct);
    }

    /// <summary>Marca como paga (ação do dono) e CONVERTE o lead em cliente no pipeline.</summary>
    public async Task<Result<ProposalDto>> MarkPaidAsync(Guid proposalId, Guid userId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal == null || proposal.UserId != userId)
            return Result.Failure<ProposalDto>(Error.NotFound("Proposal", proposalId.ToString()));

        try
        {
            proposal.MarkPaid();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ProposalDto>(Error.Validation("Proposal", ex.Message));
        }

        var customer = await _customerRepository.GetByIdAsync(proposal.CustomerId, ct);
        if (customer != null)
        {
            customer.ConvertToCustomer(); // fecha o negócio no pipeline
            if (customer.EstimatedValue == null)
                customer.UpdateDealInfo(proposal.Amount, null);
            await _customerRepository.UpdateAsync(customer, ct);
        }

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Proposal PAGA: {ProposalId} (R$ {Amount}) — lead {CustomerId} convertido em cliente",
            proposal.Id, proposal.Amount, proposal.CustomerId);

        return ToDto(proposal, customer?.Name ?? "—");
    }

    public async Task<Result<ProposalDto>> CancelAsync(Guid proposalId, Guid userId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal == null || proposal.UserId != userId)
            return Result.Failure<ProposalDto>(Error.NotFound("Proposal", proposalId.ToString()));

        try
        {
            proposal.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ProposalDto>(Error.Validation("Proposal", ex.Message));
        }

        await _proposalRepository.UpdateAsync(proposal, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var customer = await _customerRepository.GetByIdAsync(proposal.CustomerId, ct);
        return ToDto(proposal, customer?.Name ?? "—");
    }

    private async Task<Proposal?> FindByTokenAsync(string token, CancellationToken ct)
    {
        // Token opaco de 40 chars — valida formato antes de ir ao banco
        if (string.IsNullOrWhiteSpace(token) || token.Length is < 32 or > 64
            || !token.All(char.IsLetterOrDigit))
            return null;
        return await _proposalRepository.GetByPublicTokenAsync(token, ct);
    }

    private static ProposalDto ToDto(Proposal p, string customerName) => new(
        p.Id, p.CustomerId, customerName, p.Title, p.Description, p.Amount,
        p.Status, p.PublicToken, p.PixKey, p.ValidUntil, p.SentAt, p.AcceptedAt,
        p.PaidAt, p.ViewCount, p.CreatedAt);
}
