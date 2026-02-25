namespace Diax.Application.Outreach.Dtos;

/// <summary>
/// DTO de resposta com resultado da segmentação de leads.
/// </summary>
public class SegmentationResultResponse
{
    public int TotalProcessed { get; set; }
    public int HotCount { get; set; }
    public int WarmCount { get; set; }
    public int ColdCount { get; set; }
}
