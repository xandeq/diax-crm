using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

public class ApifyImportRequest
{
    public string DatasetUrl { get; set; } = string.Empty;
    public LeadSource Source { get; set; } = LeadSource.GoogleMaps;
}
