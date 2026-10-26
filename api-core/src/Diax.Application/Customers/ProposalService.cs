using System.Globalization;
using Diax.Application.Common;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dispatch;
using Diax.Application.Notifications;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public record CreateProposalRequest(
    Guid CustomerId,
    string Title,
    string Description,
    decimal Amount,
    string? PixKey,
    DateTime? ValidUntil,
    Guid? PackageId = null);

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
    DateTime CreatedAt,
    Guid? PackageId = null,
    ProposalOrigin Origin = ProposalOrigin.Manual);

/// <summary>Resultado do rascunho pedido pelo bot: Created=false quando o lead já tinha proposta aberta.</summary>
public record BotDraftResult(ProposalDto Proposal, bool Created);

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
    string? PixCopiaECola,
    /// <summary>Capa gerada por IA (URL relativa /generated-media — absolutizada no controller).</summary>
    string? CoverImageUrl = null,
    /// <summary>wa.me do dono com mensagem pronta (null se Proposals:ContactWhatsApp não estiver configurado).</summary>
    string? ContactWhatsAppUrl = null);

public class ProposalService : IApplicationService
{
    private const string MerchantName = "Alexandre Queiroz";
    private const string MerchantCity = "Vitoria";

    private static readonly CultureInfo PtBr = new("pt-BR");

    private const string DefaultPublicBaseUrl = "https://crm.alexandrequeiroz.com.br";
    private const string FromEmail = "contato@alexandrequeiroz.com.br";
    private const string FromName = "Alexandre Queiroz Marketing Digital";

    private readonly IProposalRepository _proposalRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IServicePackageRepository _servicePackageRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITelegramSender _telegramSender;
    private readonly IEmailDispatchService _emailDispatchService;
    private readonly IProposalCoverImageGenerator _coverImageGenerator;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(
        IProposalRepository proposalRepository,
        ICustomerRepository customerRepository,
        IServicePackageRepository servicePackageRepository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ITelegramSender telegramSender,
        IEmailDispatchService emailDispatchService,
        IProposalCoverImageGenerator coverImageGenerator,
        ILogger<ProposalService> logger)
    {
        _proposalRepository = proposalRepository;
        _customerRepository = customerRepository;
        _servicePackageRepository = servicePackageRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _telegramSender = telegramSender;
        _emailDispatchService = emailDispatchService;
        _coverImageGenerator = coverImageGenerator;
        _logger = logger;
    }

    /// <summary>Notificação fire-safe: falha no Telegram nunca quebra o fluxo público.</summary>
    private async Task NotifyAsync(string html, CancellationToken ct)
    {
        try
        {
            await _telegramSender.SendAsync(html, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao notificar Telegram (ignorada)");
        }
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public Task<Result<ProposalDto>> CreateAsync(
        CreateProposalRequest request, Guid userId, CancellationToken ct = default)
        => CreateCoreAsync(request, userId, ProposalOrigin.Manual, null, ct);

    /// <summary>
    /// Rascunho pedido pelo bot de WhatsApp: nunca envia nada ao cliente. Cria a proposta em
    /// Rascunho (Origin = Bot) a partir de um pacote ativo do dono e uma tarefa urgente para o
    /// dono revisar e enviar. Idempotente: se o lead já tem proposta aberta, devolve essa.
    /// </summary>
    public async Task<Result<BotDraftResult>> CreateBotDraftAsync(
        Guid customerId, Guid packageId, string? summary, Guid ownerUserId, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer == null)
            return Result.Failure<BotDraftResult>(Error.NotFound("Customer", customerId.ToString()));

        var open = (await _proposalRepository.GetByCustomerAsync(customerId, ct)).FirstOrDefault(p => p.IsOpen);
        if (open != null)
            return new BotDraftResult(ToDto(open, customer.Name), false);

        var package = await _servicePackageRepository.GetByIdAsync(packageId, ct);
        if (package == null || !package.IsActive || package.UserId != ownerUserId)
            return Result.Failure<BotDraftResult>(Error.Validation("ServicePackage", "Pacote inexistente ou inativo."));

        var desc = string.IsNullOrWhiteSpace(summary) ? $"Pacote {package.Name}" : summary.Trim();
        var task = new TaskItem
        {
            Title = $"Revisar e enviar proposta: {customer.Name}",
            Description = desc.Length > 256 ? desc[..256] : desc, // coluna description é nvarchar(256)
            Priority = TaskItemPriority.Urgent,
            DueDate = DateTime.UtcNow.Date.AddHours(21),
            UserId = ownerUserId,
            CustomerId = customerId,
        };

        var created = await CreateCoreAsync(
            new CreateProposalRequest(customerId, "", "", 0m, null, DateTime.UtcNow.AddDays(7), package.Id),
            ownerUserId, ProposalOrigin.Bot, task, ct);
        return created.IsSuccess
            ? new BotDraftResult(created.Value, true)
            : Result.Failure<BotDraftResult>(created.Error);
    }

    private async Task<Result<ProposalDto>> CreateCoreAsync(
        CreateProposalRequest request, Guid userId, ProposalOrigin origin, TaskItem? followUpTask, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result.Failure<ProposalDto>(Error.NotFound("Customer", request.CustomerId.ToString()));

        // Pacote do catálogo: preenche só o que o dono deixou vazio (ajuste manual prevalece).
        ServicePackage? package = null;
        if (request.PackageId is { } packageId)
        {
            package = await _servicePackageRepository.GetByIdAsync(packageId, ct);
            if (package == null || package.UserId != userId)
                return Result.Failure<ProposalDto>(Error.NotFound("ServicePackage", packageId.ToString()));
        }

        var title = string.IsNullOrWhiteSpace(request.Title) && package != null ? package.Name : request.Title;
        var description = string.IsNullOrWhiteSpace(request.Description) && package != null ? package.ToProposalDescription() : request.Description;
        var amount = request.Amount <= 0 && package != null ? package.Price : request.Amount;

        Proposal proposal;
        try
        {
            proposal = new Proposal(
                customer.Id, userId, title, description,
                amount, request.PixKey, request.ValidUntil);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<ProposalDto>(Error.Validation("Proposal", ex.Message));
        }

        if (package != null)
            proposal.LinkPackage(package.Id, origin);
        else
            proposal.SetOrigin(origin);

        await _proposalRepository.AddAsync(proposal, ct);

        // Enviar proposta caracteriza avanço no funil: Lead/Contacted → Negotiating
        if (customer.Status < Domain.Customers.Enums.CustomerStatus.Negotiating)
            customer.UpdateStatus(Domain.Customers.Enums.CustomerStatus.Negotiating);
        // Sincroniza o valor do negócio no pipeline se ainda não definido
        if (customer.EstimatedValue == null)
            customer.UpdateDealInfo(amount, request.ValidUntil);
        await _customerRepository.UpdateAsync(customer, ct);

        if (followUpTask != null)
            await _taskRepository.AddAsync(followUpTask, ct); // mesma transação da proposta

        await _unitOfWork.SaveChangesAsync(ct);

        // Capa por IA em background (grátis via Pollinations) — nunca bloqueia nem falha a criação
        _coverImageGenerator.QueueGeneration(proposal.Id, proposal.Title);

        _logger.LogInformation(
            "Proposal criada: {ProposalId} para {CustomerId} (R$ {Amount})",
            proposal.Id, customer.Id, amount);

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

        // 🔔 Primeira visualização = momento quente — avisa o dono na hora
        if (proposal.ViewCount == 1 && proposal.Status is not (ProposalStatus.Paid or ProposalStatus.Cancelled))
        {
            await NotifyAsync(
                $"👀 <b>Proposta visualizada agora!</b>\n" +
                $"{Esc(proposal.Title)}\n" +
                $"Cliente: {Esc(customer?.Name ?? "—")} · Valor: {proposal.Amount.ToString("C0", PtBr)}\n" +
                $"<i>Momento quente para fazer contato.</i>", ct);
        }

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
            PixCopiaECola: pix,
            CoverImageUrl: proposal.CoverImageUrl);
    }

    /// <summary>Aceite pelo link público (anônimo, por token não-enumerável).</summary>
    public async Task<Result<PublicProposalDto>> AcceptPublicAsync(string token, CancellationToken ct = default)
    {
        var proposal = await FindByTokenAsync(token, ct);
        if (proposal == null)
            return Result.Failure<PublicProposalDto>(Error.NotFound("Proposal", "token"));

        var statusBefore = proposal.Status;
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

        if (statusBefore == ProposalStatus.Accepted)
            return await GetPublicAsync(token, ct); // clique repetido — sem nova notificação

        _logger.LogInformation("Proposal ACEITA pelo cliente: {ProposalId}", proposal.Id);

        var acceptedCustomer = await _customerRepository.GetByIdAsync(proposal.CustomerId, ct);
        var phone = !string.IsNullOrWhiteSpace(acceptedCustomer?.Phone)
            ? acceptedCustomer!.Phone : acceptedCustomer?.WhatsApp;
        var contactParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(phone)) contactParts.Add(WhatsAppLink.AsHtml(phone));
        if (!string.IsNullOrWhiteSpace(acceptedCustomer?.Email)) contactParts.Add(Esc(acceptedCustomer!.Email!));
        var contact = string.Join(" · ", contactParts);
        await NotifyAsync(
            $"✅ <b>PROPOSTA ACEITA!</b> 🎉\n" +
            $"{Esc(proposal.Title)}\n" +
            $"Cliente: {Esc(acceptedCustomer?.Name ?? "—")} · Valor: <b>{proposal.Amount.ToString("C0", PtBr)}</b>\n" +
            (contact.Length > 0 ? $"Contato: {contact}\n" : "") +
            $"<i>Aguardando pagamento — marque como paga em crm.alexandrequeiroz.com.br/propostas quando o PIX cair.</i>", ct);

        return await GetPublicAsync(token, ct);
    }

    /// <summary>Marca como paga (ação do dono) e CONVERTE o lead em cliente no pipeline.</summary>
    public async Task<Result<ProposalDto>> MarkPaidAsync(Guid proposalId, Guid userId, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal == null || proposal.UserId != userId)
            return Result.Failure<ProposalDto>(Error.NotFound("Proposal", proposalId.ToString()));

        var wasPaid = proposal.Status == ProposalStatus.Paid;
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

        // Pago = começa o trabalho: tarefa de kickoff para o dono não deixar o cliente esperando.
        // Só na transição (marcar paga de novo não duplica).
        if (!wasPaid)
        {
            var desc = $"{proposal.Title} pago ({proposal.Amount.ToString("C", PtBr)}). " +
                       "Combinar o início: briefing, acessos, materiais e prazo.";
            await _taskRepository.AddAsync(new TaskItem
            {
                Title = $"Iniciar projeto: {customer?.Name ?? proposal.Title}",
                Description = desc.Length > 256 ? desc[..256] : desc,
                Priority = TaskItemPriority.High,
                DueDate = DateTime.UtcNow.Date.AddDays(1),
                UserId = userId,
                CustomerId = proposal.CustomerId,
            }, ct);
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

    /// <summary>
    /// Envia a proposta por email ao cliente (pipeline com fallback multi-provider).
    /// Idempotente por dia (reenvio no mesmo dia é replay, não duplica).
    /// </summary>
    public async Task<Result<string>> SendByEmailAsync(
        Guid proposalId, Guid userId, string? publicBaseUrl = null, CancellationToken ct = default)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, ct);
        if (proposal == null || proposal.UserId != userId)
            return Result.Failure<string>(Error.NotFound("Proposal", proposalId.ToString()));

        if (proposal.Status is ProposalStatus.Paid or ProposalStatus.Cancelled)
            return Result.Failure<string>(Error.Validation("Proposal", "Proposta paga/cancelada não pode ser reenviada."));
        if (proposal.IsExpired)
            return Result.Failure<string>(Error.Validation("Proposal", "Proposta expirada — crie uma nova antes de enviar."));

        var customer = await _customerRepository.GetByIdAsync(proposal.CustomerId, ct);
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
            return Result.Failure<string>(Error.Validation("Customer", "O lead não tem email cadastrado."));
        if (customer.EmailOptOut)
            return Result.Failure<string>(Error.Validation("Customer", "O lead optou por não receber emails (opt-out)."));

        var baseUrl = string.IsNullOrWhiteSpace(publicBaseUrl) ? DefaultPublicBaseUrl : publicBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/proposta?t={proposal.PublicToken}";
        var html = BuildProposalEmailHtml(proposal, customer.Name, link);

        var result = await _emailDispatchService.DispatchAsync(new EmailDispatchRequest(
            Message: new EmailMessage
            {
                From = new EmailAddress(FromEmail, FromName),
                To = new[] { new EmailAddress(customer.Email!, customer.Name) },
                ReplyTo = new EmailAddress(FromEmail, FromName),
                Subject = $"Proposta: {proposal.Title}",
                Html = html,
                Tags = new[] { "proposal" },
            },
            IdempotencyKey: $"proposal-email-{proposal.Id:N}-{DateTime.UtcNow:yyyyMMdd}",
            ProviderHint: null,
            RequestId: Guid.NewGuid().ToString("N"),
            AllowUnaligned: false), ct);

        if (!result.Success)
            return Result.Failure<string>(Error.Validation(
                "Email", $"Falha ao enviar por todos os providers ({result.Status})."));

        proposal.MarkSent();
        customer.RegisterContact();
        await _proposalRepository.UpdateAsync(proposal, ct);
        await _customerRepository.UpdateAsync(customer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Proposta {ProposalId} enviada por email para {Email} via {Provider} ({Status})",
            proposal.Id, customer.Email, result.ProviderUsed, result.Status);

        return result.Status == EmailDispatchStatus.Duplicate
            ? $"Já enviada hoje (replay) via {result.ProviderUsed ?? "provider"}"
            : $"Enviada via {result.ProviderUsed ?? "provider"}";
    }

    /// <summary>Template HTML do email da proposta — função pura testável.</summary>
    public static string BuildProposalEmailHtml(Proposal proposal, string customerName, string publicLink)
    {
        var brl = proposal.Amount.ToString("C2", PtBr);
        var validade = proposal.ValidUntil.HasValue
            ? $"<p style='color:#888;font-size:13px'>Proposta válida até <strong>{proposal.ValidUntil.Value:dd/MM/yyyy}</strong>.</p>"
            : "";

        return $@"<!DOCTYPE html>
<html><body style='margin:0;padding:0;background:#f4f4f7;font-family:Arial,Helvetica,sans-serif'>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f7;padding:32px 16px'>
<tr><td align='center'>
<table role='presentation' width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden'>
  <tr><td style='background:#5b21b6;padding:20px 32px'>
    <p style='margin:0;color:#ffffff;font-size:15px;font-weight:bold'>Alexandre Queiroz · Marketing Digital</p>
  </td></tr>
  <tr><td style='padding:32px'>
    <p style='font-size:15px;color:#333'>Olá, <strong>{Esc(customerName)}</strong>!</p>
    <p style='font-size:15px;color:#333;line-height:1.6'>
      Preparei uma proposta personalizada para você:
    </p>
    <p style='font-size:18px;color:#111;font-weight:bold;margin:20px 0 6px'>{Esc(proposal.Title)}</p>
    <p style='font-size:24px;color:#059669;font-weight:bold;margin:0 0 20px'>{brl}</p>
    <p style='font-size:14px;color:#555;line-height:1.6'>
      No link abaixo você encontra o escopo completo, pode <strong>aceitar online</strong> e,
      se preferir, já <strong>pagar via PIX</strong> em segundos.
    </p>
    <table role='presentation' cellpadding='0' cellspacing='0' style='margin:24px auto'>
      <tr><td style='background:#5b21b6;border-radius:10px'>
        <a href='{publicLink}' style='display:inline-block;padding:14px 36px;color:#ffffff;font-size:15px;font-weight:bold;text-decoration:none'>
          Ver proposta completa
        </a>
      </td></tr>
    </table>
    {validade}
    <p style='font-size:13px;color:#888;line-height:1.5'>
      Qualquer dúvida, é só responder este email ou chamar no WhatsApp.
    </p>
  </td></tr>
  <tr><td style='background:#fafafa;padding:16px 32px'>
    <p style='margin:0;font-size:12px;color:#999'>Alexandre Queiroz · alexandrequeiroz.com.br</p>
  </td></tr>
</table>
</td></tr>
</table>
</body></html>";
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
        p.PaidAt, p.ViewCount, p.CreatedAt, p.PackageId, p.Origin);
}
