namespace Diax.Application.EmailMarketing.Dtos;

public class QueueBulkEmailResponse
{
    public int RequestedCount { get; set; }
    public int QueuedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> SkippedRecipients { get; set; } = [];
}
