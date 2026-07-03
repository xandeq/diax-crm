using System.Globalization;
using System.Text;
using Diax.Application.Common;
using Diax.Application.Notifications;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Tasks;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public record CommercialBriefingData(
    DateTime DateBrt,
    List<Meeting> MeetingsToday,
    List<TaskItem> FollowUpsDue,
    List<Customer> HotLeads,
    List<Proposal> PendingProposals,
    decimal WeightedForecast,
    decimal WonLast30DaysValue,
    int WonLast30DaysCount);

/// <summary>
/// Briefing comercial matinal: reuniões de hoje + follow-ups vencendo + leads
/// quentes + propostas pendentes + previsão do pipeline — entregue no Telegram.
/// </summary>
public class CommercialBriefingService : IApplicationService
{
    private static readonly TimeSpan BrtOffset = TimeSpan.FromHours(-3);
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly IMeetingRepository _meetingRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly PipelineService _pipelineService;
    private readonly ITelegramSender _telegramSender;
    private readonly ILogger<CommercialBriefingService> _logger;

    public CommercialBriefingService(
        IMeetingRepository meetingRepository,
        ITaskRepository taskRepository,
        ICustomerRepository customerRepository,
        IProposalRepository proposalRepository,
        PipelineService pipelineService,
        ITelegramSender telegramSender,
        ILogger<CommercialBriefingService> logger)
    {
        _meetingRepository = meetingRepository;
        _taskRepository = taskRepository;
        _customerRepository = customerRepository;
        _proposalRepository = proposalRepository;
        _pipelineService = pipelineService;
        _telegramSender = telegramSender;
        _logger = logger;
    }

    public async Task<CommercialBriefingData> ComposeAsync(Guid userId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var todayBrt = (nowUtc + BrtOffset).Date;
        var dayStartUtc = DateTime.SpecifyKind(todayBrt - BrtOffset, DateTimeKind.Utc);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var meetings = await _meetingRepository.GetConfirmedInRangeAsync(userId, dayStartUtc, dayEndUtc, ct);

        var allTasks = await _taskRepository.GetByUserAsync(userId, includeArchived: false, ct);
        var followUps = allTasks
            .Where(t => t.Status is TaskItemStatus.Todo or TaskItemStatus.InProgress)
            .Where(t => t.DueDate.HasValue && t.DueDate.Value < dayEndUtc)
            .OrderBy(t => t.DueDate)
            .Take(8)
            .ToList();

        var hotLeads = (await _customerRepository.GetAllLeadsAsync(ct))
            .Where(l => l.Segment == LeadSegment.Hot)
            .OrderByDescending(l => l.LeadScore ?? 0)
            .Take(5)
            .ToList();

        var proposals = (await _proposalRepository.GetByUserAsync(userId, 50, ct))
            .Where(p => p.Status is ProposalStatus.Sent or ProposalStatus.Accepted)
            .OrderByDescending(p => p.Status)
            .ThenByDescending(p => p.Amount)
            .ToList();

        var board = await _pipelineService.GetBoardAsync(ct);

        return new CommercialBriefingData(
            todayBrt, meetings, followUps, hotLeads, proposals,
            board.WeightedForecast, board.WonLast30DaysValue, board.WonLast30DaysCount);
    }

    /// <summary>Formata o briefing em HTML do Telegram — função pura testável.</summary>
    public static string FormatBriefing(CommercialBriefingData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<b>☀️ Briefing Comercial — {d.DateBrt.ToString("dddd, dd/MM", PtBr)}</b>");
        sb.AppendLine();

        // Reuniões
        if (d.MeetingsToday.Count > 0)
        {
            sb.AppendLine($"<b>📅 Reuniões de hoje ({d.MeetingsToday.Count})</b>");
            foreach (var m in d.MeetingsToday.OrderBy(m => m.ScheduledAt))
            {
                var brt = m.ScheduledAt + BrtOffset;
                sb.AppendLine($"• {brt:HH:mm} — {Esc(m.ContactName)}" +
                    (m.ContactPhone != null ? $" ({Esc(m.ContactPhone)})" : ""));
            }
            sb.AppendLine();
        }

        // Follow-ups
        if (d.FollowUpsDue.Count > 0)
        {
            sb.AppendLine($"<b>📞 Follow-ups para hoje ({d.FollowUpsDue.Count})</b>");
            foreach (var t in d.FollowUpsDue.Take(5))
                sb.AppendLine($"• {Esc(Truncate(t.Title, 60))}");
            if (d.FollowUpsDue.Count > 5)
                sb.AppendLine($"  <i>… e mais {d.FollowUpsDue.Count - 5} na lista de Tarefas</i>");
            sb.AppendLine();
        }

        // Leads quentes
        if (d.HotLeads.Count > 0)
        {
            sb.AppendLine($"<b>🔥 Leads quentes (top {d.HotLeads.Count})</b>");
            foreach (var l in d.HotLeads)
            {
                var contact = !string.IsNullOrWhiteSpace(l.Phone) ? l.Phone
                    : !string.IsNullOrWhiteSpace(l.WhatsApp) ? l.WhatsApp : l.Email;
                sb.AppendLine($"• {Esc(Truncate(l.Name, 40))} (score {l.LeadScore}) — {Esc(contact ?? "-")}");
            }
            sb.AppendLine();
        }

        // Propostas pendentes
        if (d.PendingProposals.Count > 0)
        {
            var total = d.PendingProposals.Sum(p => p.Amount);
            sb.AppendLine($"<b>📄 Propostas na mesa ({d.PendingProposals.Count} — {Brl(total)})</b>");
            foreach (var p in d.PendingProposals.Take(5))
            {
                var flag = p.Status == ProposalStatus.Accepted ? "✅ aceita, aguarda pagamento" :
                    p.ViewCount > 0 ? $"👀 vista {p.ViewCount}x" : "enviada";
                sb.AppendLine($"• {Esc(Truncate(p.Title, 45))} — {Brl(p.Amount)} ({flag})");
            }
            sb.AppendLine();
        }

        // Pipeline
        sb.AppendLine("<b>💰 Pipeline</b>");
        sb.AppendLine($"• Previsão ponderada: <b>{Brl(d.WeightedForecast)}</b>");
        sb.AppendLine($"• Fechado (30d): {Brl(d.WonLast30DaysValue)} ({d.WonLast30DaysCount} negócio{(d.WonLast30DaysCount == 1 ? "" : "s")})");

        if (d.MeetingsToday.Count == 0 && d.FollowUpsDue.Count == 0 && d.PendingProposals.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("<i>Dia livre de compromissos — bom dia para prospectar os leads quentes! 🚀</i>");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>Compõe e envia o briefing ao Telegram. Retorna o texto enviado.</summary>
    public async Task<Result<string>> SendAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_telegramSender.IsConfigured)
            return Result.Failure<string>(Error.Validation(
                "Telegram", "Telegram não configurado (Telegram:BotToken/ChatId)."));

        var data = await ComposeAsync(userId, ct);
        var text = FormatBriefing(data);

        var sent = await _telegramSender.SendAsync(text, ct);
        if (!sent)
            return Result.Failure<string>(Error.Validation(
                "Telegram", "Falha ao enviar a mensagem ao Telegram — ver logs."));

        _logger.LogInformation(
            "Briefing comercial enviado: {Meetings} reuniões, {FollowUps} follow-ups, {Hot} hot, {Proposals} propostas",
            data.MeetingsToday.Count, data.FollowUpsDue.Count, data.HotLeads.Count, data.PendingProposals.Count);

        return text;
    }

    private static string Esc(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static string Brl(decimal v) => v.ToString("C0", PtBr);
}
