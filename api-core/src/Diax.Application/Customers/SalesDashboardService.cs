using Diax.Application.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.Tasks;

namespace Diax.Application.Customers;

public record FunnelStageDto(string Stage, string Label, int Count, decimal? ConversionToNext);

public record MonthlyRevenueDto(string Month, decimal Total, int Count);

public record ProposalSummaryDto(string Status, int Count, decimal Total);

public record SalesDashboardDto(
    List<FunnelStageDto> Funnel,
    List<MonthlyRevenueDto> MonthlyRevenue,
    List<ProposalSummaryDto> Proposals,
    decimal ProposalAcceptanceRate,   // aceitas+pagas / (enviadas+aceitas+pagas), 0-1
    int HotLeads,
    int WarmLeads,
    int ColdLeads,
    int UpcomingMeetings,
    int OpenFollowUps,
    decimal WeightedForecast,
    decimal WonLast30DaysValue,
    int WonLast30DaysCount);

/// <summary>
/// Dashboard comercial: funil com conversões, receita mensal (propostas pagas),
/// resumo de propostas, segmentos e compromissos — uma chamada, só leitura.
/// </summary>
public class SalesDashboardService : IApplicationService
{
    private static readonly (CustomerStatus Status, string Label)[] FunnelStages =
    {
        (CustomerStatus.Lead, "Leads"),
        (CustomerStatus.Contacted, "Contactados"),
        (CustomerStatus.Qualified, "Qualificados"),
        (CustomerStatus.Negotiating, "Negociando"),
        (CustomerStatus.Customer, "Clientes"),
    };

    private readonly ICustomerRepository _customerRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly IMeetingRepository _meetingRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly PipelineService _pipelineService;

    public SalesDashboardService(
        ICustomerRepository customerRepository,
        IProposalRepository proposalRepository,
        IMeetingRepository meetingRepository,
        ITaskRepository taskRepository,
        PipelineService pipelineService)
    {
        _customerRepository = customerRepository;
        _proposalRepository = proposalRepository;
        _meetingRepository = meetingRepository;
        _taskRepository = taskRepository;
        _pipelineService = pipelineService;
    }

    public async Task<SalesDashboardDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var statusCounts = await _customerRepository.GetStatusCountsAsync(ct);
        var segmentCounts = await _customerRepository.GetSegmentCountsAsync(ct);
        var proposalSummary = await _proposalRepository.GetStatusSummaryAsync(userId, ct);
        var paid = await _proposalRepository.GetPaidSinceAsync(userId, now.AddMonths(-6), ct);
        var meetings = await _meetingRepository.GetUpcomingAsync(userId, 50, ct);
        var tasks = await _taskRepository.GetByUserAsync(userId, includeArchived: false, ct);
        var board = await _pipelineService.GetBoardAsync(ct);

        return new SalesDashboardDto(
            Funnel: BuildFunnel(statusCounts),
            MonthlyRevenue: BucketByMonth(paid, now, months: 6),
            Proposals: proposalSummary
                .OrderBy(p => p.Status)
                .Select(p => new ProposalSummaryDto(p.Status.ToString(), p.Count, p.Total))
                .ToList(),
            ProposalAcceptanceRate: AcceptanceRate(proposalSummary),
            HotLeads: segmentCounts.GetValueOrDefault(LeadSegment.Hot),
            WarmLeads: segmentCounts.GetValueOrDefault(LeadSegment.Warm),
            ColdLeads: segmentCounts.GetValueOrDefault(LeadSegment.Cold),
            UpcomingMeetings: meetings.Count,
            OpenFollowUps: tasks.Count(t =>
                t.Status is TaskItemStatus.Todo or TaskItemStatus.InProgress && t.CustomerId != null),
            WeightedForecast: board.WeightedForecast,
            WonLast30DaysValue: board.WonLast30DaysValue,
            WonLast30DaysCount: board.WonLast30DaysCount);
    }

    /// <summary>Funil com taxa de conversão para o próximo estágio — função pura testável.</summary>
    public static List<FunnelStageDto> BuildFunnel(Dictionary<CustomerStatus, int> counts)
    {
        var result = new List<FunnelStageDto>();
        for (var i = 0; i < FunnelStages.Length; i++)
        {
            var (status, label) = FunnelStages[i];
            var count = counts.GetValueOrDefault(status);

            decimal? conversion = null;
            if (i < FunnelStages.Length - 1)
            {
                // Conversão = quem está ADIANTE no funil / (estágio atual + adiante).
                // Aproximação em snapshot: mede a proporção que já avançou.
                var ahead = FunnelStages.Skip(i + 1).Sum(s => counts.GetValueOrDefault(s.Status));
                var basis = count + ahead;
                conversion = basis > 0 ? Math.Round((decimal)ahead / basis, 4) : null;
            }

            result.Add(new FunnelStageDto(status.ToString(), label, count, conversion));
        }
        return result;
    }

    /// <summary>Agrupa propostas pagas por mês (yyyy-MM), preenchendo meses vazios — pura.</summary>
    public static List<MonthlyRevenueDto> BucketByMonth(List<Proposal> paid, DateTime nowUtc, int months)
    {
        var result = new List<MonthlyRevenueDto>();
        var current = new DateTime(nowUtc.Year, nowUtc.Month, 1);
        for (var i = months - 1; i >= 0; i--)
        {
            var month = current.AddMonths(-i);
            var inMonth = paid.Where(p =>
                p.PaidAt.HasValue
                && p.PaidAt.Value.Year == month.Year
                && p.PaidAt.Value.Month == month.Month).ToList();
            result.Add(new MonthlyRevenueDto(
                month.ToString("yyyy-MM"),
                inMonth.Sum(p => p.Amount),
                inMonth.Count));
        }
        return result;
    }

    /// <summary>Taxa de aceite: (aceitas + pagas) / (enviadas + aceitas + pagas) — pura.</summary>
    public static decimal AcceptanceRate(List<(ProposalStatus Status, int Count, decimal Total)> summary)
    {
        int Of(ProposalStatus s) => summary.Where(x => x.Status == s).Sum(x => x.Count);
        var accepted = Of(ProposalStatus.Accepted) + Of(ProposalStatus.Paid);
        var basis = Of(ProposalStatus.Sent) + accepted;
        return basis > 0 ? Math.Round((decimal)accepted / basis, 4) : 0m;
    }
}
