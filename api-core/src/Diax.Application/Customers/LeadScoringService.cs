using Diax.Application.Common;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public record LeadScoringSummaryDto(
    int LeadsScored,
    int Hot,
    int Warm,
    int Cold,
    int FollowUpTasksCreated);

/// <summary>
/// Lead scoring 0–100 combinando engajamento de email (opens/clicks), completude
/// de cadastro e recência — atualiza LeadScore/Segment de todos os leads abertos
/// e (opcionalmente) cria tasks de follow-up para leads quentes parados.
/// </summary>
public class LeadScoringService : IApplicationService
{
    // Limiares de segmento (score 0–100)
    public const int HotThreshold = 60;
    public const int WarmThreshold = 30;

    /// <summary>Máximo de tasks de follow-up criadas por execução (não inundar o dia).</summary>
    public const int MaxFollowUpsPerRun = 10;

    /// <summary>Lead quente sem contato há mais que isso vira follow-up.</summary>
    public static readonly TimeSpan FollowUpIdleThreshold = TimeSpan.FromDays(3);

    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailEventRepository _emailEventRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LeadScoringService> _logger;

    public LeadScoringService(
        ICustomerRepository customerRepository,
        IEmailEventRepository emailEventRepository,
        ITaskRepository taskRepository,
        IUnitOfWork unitOfWork,
        ILogger<LeadScoringService> logger)
    {
        _customerRepository = customerRepository;
        _emailEventRepository = emailEventRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Recalcula score/segmento de todos os leads abertos.
    /// Se <paramref name="followUpOwnerUserId"/> for informado, cria tasks de
    /// follow-up (com dedup e cap) para os leads mais quentes parados.
    /// </summary>
    public async Task<LeadScoringSummaryDto> RecomputeAllAsync(
        Guid? followUpOwnerUserId = null,
        CancellationToken ct = default)
    {
        var leads = (await _customerRepository.GetAllLeadsAsync(ct)).ToList();
        var engagement = (await _emailEventRepository.GetEngagementSummaryAsync(ct))
            .ToDictionary(e => e.CustomerId);

        int hot = 0, warm = 0, cold = 0;
        var now = DateTime.UtcNow;

        foreach (var lead in leads)
        {
            engagement.TryGetValue(lead.Id, out var eng);
            var score = CalculateScore(lead, eng, now);
            var segment = SegmentForScore(score);

            if (lead.LeadScore != score || lead.Segment != segment)
                lead.UpdateSegmentation(score, segment);

            switch (segment)
            {
                case LeadSegment.Hot: hot++; break;
                case LeadSegment.Warm: warm++; break;
                default: cold++; break;
            }
        }

        var followUps = 0;
        if (followUpOwnerUserId.HasValue)
            followUps = await CreateFollowUpTasksAsync(leads, followUpOwnerUserId.Value, now, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "LeadScoring: {Total} leads (Hot: {Hot}, Warm: {Warm}, Cold: {Cold}). Follow-ups criados: {FollowUps}",
            leads.Count, hot, warm, cold, followUps);

        return new LeadScoringSummaryDto(leads.Count, hot, warm, cold, followUps);
    }

    /// <summary>
    /// Score 0–100. O bloco de fit (site, telefone/WhatsApp, elegibilidade, DDD local 27/28 —
    /// mercado é Vitória-ES — mais os sinais de qualidade da Phase 7: site próprio vs diretório,
    /// Quality e EmailType) vale no máximo 45. Recalibrado na Phase 8 (IMPT-03 / D-05) para que um
    /// lead de boa qualidade nasça Warm (30) já no import, sem nenhum engajamento; um lead de
    /// sinais fracos permanece Cold. O teto de 45 mantém Hot (60) inalcançável sem engajamento
    /// real — aberturas, cliques ou o avanço de Status para Qualified/Negotiating. Bounce, opt-out
    /// (email ou WhatsApp) e domínio suspeito penalizam.
    /// </summary>
    public static int CalculateScore(Customer lead, CustomerEngagementSummary? engagement, DateTime nowUtc)
    {
        var score = 0;

        // ── Fit de cadastro (máx 45 — recalibrado na Phase 8 / IMPT-03 / decisão D-05) ──
        // O teto subiu de 25 para 45 justamente para que um lead com sinais fortes de qualidade
        // (coletados pela Phase 7) alcance Warm (30) já no import, sem nenhum engajamento.
        // O teto continua ABAIXO de Hot (60): nascer Hot sem engajamento é impossível por
        // construção, e isso é intencional.
        if (!string.IsNullOrWhiteSpace(lead.Website)) score += 5;
        if (!string.IsNullOrWhiteSpace(lead.Phone) || !string.IsNullOrWhiteSpace(lead.WhatsApp)) score += 5;
        if (lead.IsEligibleForCampaigns) score += 5;
        if (HasLocalDdd(lead.WhatsApp) || HasLocalDdd(lead.Phone)) score += 10; // mercado local: Vitória-ES

        // Sinais de qualidade da Phase 7 — são FIT (propriedades do lead), não engajamento.
        if (lead.WebsiteKind == WebsiteKind.OwnSite) score += 10;                 // EXTR-03: domínio próprio > página de diretório
        if (lead.Quality == LeadQuality.High) score += 5;                         // sanitização: cadastro completo e limpo
        if (lead.EmailType == EmailType.PersonalDirect) score += 5;               // contato direto > contato@/rh@
        if (lead.HasSuspiciousDomain) score -= 15;                                // domínio suspeito derruba o fit inteiro

        // ── Engajamento de email (máx 35) ──
        if (engagement != null)
        {
            if (engagement.OpenCount >= 1) score += 5;
            if (engagement.OpenCount >= 3) score += 5;
            if (engagement.ClickCount >= 1) score += 25;

            // ── Recência do engajamento (máx 15) ──
            if (engagement.LastEngagementAt.HasValue)
            {
                var age = nowUtc - engagement.LastEngagementAt.Value;
                if (age <= TimeSpan.FromDays(7)) score += 15;
                else if (age <= TimeSpan.FromDays(30)) score += 8;
            }

            // ── Penalidades ──
            if (engagement.BounceCount >= 1) score -= 30; // email inválido — despriorizar
        }

        // ── Intenção qualificada (clique/resposta/WhatsApp levaram o Status adiante) ──
        if (lead.Status is CustomerStatus.Qualified or CustomerStatus.Negotiating) score += 15;

        if (lead.EmailOptOut) score -= 15; // saiu da lista — só vale por telefone
        if (lead.WhatsAppOptOut) score -= 15; // saiu do WhatsApp também

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Mapeia score → segmento. Fonte ÚNICA dos limiares: usada pelo RecomputeAllAsync (worker das
    /// 06h BRT) e pelo CustomerImportService (IMPT-03), garantindo que os dois caminhos nunca
    /// produzam segmentos diferentes para o mesmo score.
    /// </summary>
    public static LeadSegment SegmentForScore(int score) =>
        score >= HotThreshold ? LeadSegment.Hot
        : score >= WarmThreshold ? LeadSegment.Warm
        : LeadSegment.Cold;

    /// <summary>
    /// DDD 27/28 (Grande Vitória-ES) no telefone informado, após normalizar (só dígitos,
    /// removendo o "55" de código do país quando presente).
    /// </summary>
    private static bool HasLocalDdd(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length >= 12 && digits.StartsWith("55")) digits = digits[2..];

        return digits.StartsWith("27") || digits.StartsWith("28");
    }

    private async Task<int> CreateFollowUpTasksAsync(
        List<Customer> leads, Guid ownerUserId, DateTime now, CancellationToken ct)
    {
        // Quentes e parados: sem contato há 3+ dias (ou nunca contactados)
        var candidates = leads
            .Where(l => l.Segment == LeadSegment.Hot)
            .Where(l => l.LastContactAt == null || now - l.LastContactAt.Value >= FollowUpIdleThreshold)
            .OrderByDescending(l => l.LeadScore ?? 0)
            .ThenByDescending(l => l.EstimatedValue ?? 0)
            .ToList();

        if (candidates.Count == 0) return 0;

        // Dedup: pula quem já tem task aberta
        var withOpenTasks = await _taskRepository.GetCustomerIdsWithOpenTasksAsync(
            candidates.Select(c => c.Id), ct);

        var created = 0;
        foreach (var lead in candidates)
        {
            if (created >= MaxFollowUpsPerRun) break;
            if (withOpenTasks.Contains(lead.Id)) continue;

            var contact = new List<string>();
            if (!string.IsNullOrWhiteSpace(lead.Phone)) contact.Add($"Tel: {lead.Phone}");
            if (!string.IsNullOrWhiteSpace(lead.WhatsApp)) contact.Add($"WhatsApp: {lead.WhatsApp}");
            if (!string.IsNullOrWhiteSpace(lead.Email)) contact.Add($"Email: {lead.Email}");

            var idleDays = lead.LastContactAt.HasValue
                ? (int)(now - lead.LastContactAt.Value).TotalDays
                : (int?)null;

            var description =
                $"Lead quente (score {lead.LeadScore}) " +
                (idleDays.HasValue ? $"parado há {idleDays} dias. " : "nunca contactado. ") +
                string.Join(" · ", contact);

            await _taskRepository.AddAsync(new TaskItem
            {
                Title = $"Follow-up: {(string.IsNullOrWhiteSpace(lead.NormalizedName) ? lead.Name : lead.NormalizedName)}",
                // Description tem máx 256 chars no banco
                Description = description.Length > 256 ? description[..256] : description,
                Priority = TaskItemPriority.High,
                DueDate = now.Date.AddDays(1),
                UserId = ownerUserId,
                CustomerId = lead.Id,
            }, ct);

            created++;
        }

        return created;
    }
}
