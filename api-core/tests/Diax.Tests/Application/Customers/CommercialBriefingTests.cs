using Diax.Application.Customers;
using Diax.Domain.Customers;
using Diax.Domain.Tasks;

namespace Diax.Tests.Application.Customers;

public class CommercialBriefingFormatTests
{
    private static readonly DateTime Today = new(2026, 7, 3);

    private static CommercialBriefingData Empty() => new(
        Today, new List<Meeting>(), new List<TaskItem>(), new List<Customer>(),
        new List<Proposal>(), 12345m, 2500m, 1);

    [Fact]
    public void FormatBriefing_Empty_ShowsPipelineAndMotivation()
    {
        var text = CommercialBriefingService.FormatBriefing(Empty());
        Assert.Contains("Briefing Comercial", text);
        Assert.Contains("Pipeline", text);
        Assert.Contains("R$", text);
        Assert.Contains("Dia livre", text);
        Assert.DoesNotContain("Reuniões de hoje", text);
    }

    [Fact]
    public void FormatBriefing_WithMeetingsAndFollowUps_ListsThem()
    {
        var meeting = new Meeting(Guid.NewGuid(),
            new DateTime(2026, 7, 3, 17, 0, 0, DateTimeKind.Utc), // 14:00 BRT
            "Firjan", "x@firjan.com.br", "21999990000", null);
        var task = new TaskItem
        {
            Title = "Follow-up: Clinica Baviera",
            DueDate = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
        };

        var d = Empty() with
        {
            MeetingsToday = new List<Meeting> { meeting },
            FollowUpsDue = new List<TaskItem> { task },
        };
        var text = CommercialBriefingService.FormatBriefing(d);

        Assert.Contains("Reuniões de hoje (1)", text);
        Assert.Contains("14:00 — Firjan", text);
        Assert.Contains("Follow-ups para hoje (1)", text);
        Assert.Contains("Clinica Baviera", text);
        Assert.DoesNotContain("Dia livre", text);
    }

    [Fact]
    public void FormatBriefing_EscapesHtml_InNames()
    {
        var lead = new Customer("Empresa <Fantasia> & Cia", "a@b.com");
        lead.UpdateSegmentation(80, Diax.Domain.Customers.Enums.LeadSegment.Hot);
        var d = Empty() with { HotLeads = new List<Customer> { lead } };

        var text = CommercialBriefingService.FormatBriefing(d);
        Assert.Contains("&lt;Fantasia&gt; &amp; Cia", text);
        Assert.DoesNotContain("<Fantasia>", text);
    }

    [Fact]
    public void FormatBriefing_Proposals_ShowAcceptedFlagAndTotal()
    {
        var p1 = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "Site A", "d", 3000m, null, null);
        p1.RegisterView();
        p1.Accept();
        var p2 = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "Site B", "d", 2000m, null, null);

        var d = Empty() with { PendingProposals = new List<Proposal> { p1, p2 } };
        var text = CommercialBriefingService.FormatBriefing(d);

        Assert.Contains("Propostas na mesa (2", text);
        Assert.Contains("aceita, aguarda pagamento", text);
        Assert.Contains("Site A", text);
    }
}
