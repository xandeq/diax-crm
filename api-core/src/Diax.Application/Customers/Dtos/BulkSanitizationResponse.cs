namespace Diax.Application.Customers.Dtos;

public class BulkSanitizationResponse
{
    public int AnalyzedLeads { get; set; }
    public int CorrectedLeads { get; set; }
    public int RemovedByInvalidEmail { get; set; }
    public int RemovedBySuspiciousDomain { get; set; }
    public int RemovedByDirectoryOrGeneric { get; set; }
    public int DuplicatesConsolidated { get; set; }
    public int ValidLeadsRemaining { get; set; }
}
