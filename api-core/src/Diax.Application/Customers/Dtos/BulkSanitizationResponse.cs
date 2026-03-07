namespace Diax.Application.Customers.Dtos;

public class BulkSanitizationResponse
{
    public int AnalyzedLeads { get; set; }
    public int UpdatedLeads { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int InvalidEmailsDetected { get; set; }
}
