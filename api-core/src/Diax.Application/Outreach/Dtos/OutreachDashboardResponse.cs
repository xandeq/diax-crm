namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de resposta para o dashboard do outreach.
/// Contém métricas de leads, emails e status dos módulos.
/// </summary>
public class OutreachDashboardResponse
{
    public int TotalLeads { get; set; }
    public int HotLeads { get; set; }
    public int WarmLeads { get; set; }
    public int ColdLeads { get; set; }
    public int UnsegmentedLeads { get; set; }
    public int EmailsSentToday { get; set; }
    public int EmailsSentThisWeek { get; set; }
    public int EmailsSentThisMonth { get; set; }
    public int PendingInQueue { get; set; }
    public int FailedInQueue { get; set; }
    public bool ImportEnabled { get; set; }
    public bool SegmentationEnabled { get; set; }
    public bool SendEnabled { get; set; }
}
