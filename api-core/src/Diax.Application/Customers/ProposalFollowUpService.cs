using System.Globalization;
using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

/// <summary>
/// Cria 1 tarefa por proposta por etapa (não vista / vista sem aceite / vencida / aceita sem pagamento).
/// Idempotente via Proposal.LastFollowUpStage: a mesma etapa nunca gera tarefa duas vezes.
/// </summary>
public class ProposalFollowUpService : IApplicationService
{
    public static readonly TimeSpan Wait = TimeSpan.FromHours(48);
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IProposalRepository _proposals;
    private readonly ICustomerRepository _customers;
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProposalFollowUpService> _logger;

    public ProposalFollowUpService(
        IProposalRepository proposals,
        ICustomerRepository customers,
        ITaskRepository tasks,
        IUnitOfWork uow,
        ILogger<ProposalFollowUpService> logger)
    {
        _proposals = proposals;
        _customers = customers;
        _tasks = tasks;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>
    /// 0 = nada a fazer; 1 = não aberta após 48h; 2 = vista sem aceite após 48h; 3 = vencida;
    /// 4 = aceita há 48h e ainda sem pagamento (dinheiro na mesa: reenviar o PIX).
    /// </summary>
    public static int StageFor(ProposalStatus status, DateTime? sentAt, int viewCount, bool isExpired, DateTime now,
        DateTime? acceptedAt = null)
    {
        if (status == ProposalStatus.Accepted)
            return acceptedAt != null && now - acceptedAt.Value >= Wait ? 4 : 0;
        if (status != ProposalStatus.Sent || sentAt == null) return 0;
        if (isExpired) return 3;
        if (now - sentAt.Value < Wait) return 0;
        return viewCount == 0 ? 1 : 2;
    }

    public static string TaskTitle(int stage, string customerName) => stage switch
    {
        1 => $"Proposta não aberta: {customerName}",
        2 => $"Proposta vista sem aceite: {customerName}",
        3 => $"Proposta vencida: {customerName}",
        4 => $"Proposta aceita sem pagamento: {customerName}",
        _ => $"Proposta: {customerName}",
    };

    private static string TaskHint(int stage) => stage switch
    {
        1 => "Reenviar o link ou chamar no WhatsApp.",
        2 => "Cliente abriu a proposta. Fazer follow-up e tirar dúvidas.",
        3 => "Renovar em /propostas (botão Renovar: +7 dias no mesmo link) ou encerrar o negócio.",
        4 => "Cliente aceitou e não pagou. Reenviar o código PIX e combinar a forma de pagamento.",
        _ => "",
    };

    public async Task<int> RunAsync(Guid ownerUserId, DateTime now, CancellationToken ct = default)
    {
        var created = 0;
        foreach (var p in await _proposals.GetAwaitingClientAsync(ownerUserId, ct))
        {
            var stage = StageFor(p.Status, p.SentAt, p.ViewCount, p.IsExpired, now, p.AcceptedAt);
            if (stage == 0 || !p.RegisterFollowUpStage(stage)) continue;

            var customer = await _customers.GetByIdAsync(p.CustomerId, ct);
            var name = customer?.Name ?? p.Title;
            var desc = $"{p.Title} · {p.Amount.ToString("C", PtBr)} · {p.ViewCount} visualização(ões). {TaskHint(stage)}";
            await _tasks.AddAsync(new TaskItem
            {
                Title = TaskTitle(stage, name),
                Description = desc.Length > 256 ? desc[..256] : desc,
                Priority = stage is 2 or 4 ? TaskItemPriority.Urgent : TaskItemPriority.High,
                DueDate = now.Date.AddDays(1),
                UserId = ownerUserId,
                CustomerId = p.CustomerId,
            }, ct);
            await _proposals.UpdateAsync(p, ct);
            created++;
        }

        if (created > 0) await _uow.SaveChangesAsync(ct);
        _logger.LogInformation("[ProposalFollowUp] {Created} tarefa(s) criada(s)", created);
        return created;
    }
}
