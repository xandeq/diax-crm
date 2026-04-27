using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

public class UpdateSegmentRequest
{
    public LeadSegment Segment { get; set; }
}
