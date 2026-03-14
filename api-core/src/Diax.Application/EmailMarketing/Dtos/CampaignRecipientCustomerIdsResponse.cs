namespace Diax.Application.EmailMarketing.Dtos;

public class CampaignRecipientCustomerIdsResponse
{
    public List<string> CustomerIds { get; set; } = new();
    public int Count { get; set; }
}
