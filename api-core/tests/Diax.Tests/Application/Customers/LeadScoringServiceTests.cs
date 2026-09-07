using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

public class LeadScoringServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IEmailEventRepository> _events = new();
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private LeadScoringService CreateService()
    {
        _tasks.Setup(t => t.GetCustomerIdsWithOpenTasksAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        return new(_customers.Object, _events.Object, _tasks.Object, _uow.Object,
            NullLogger<LeadScoringService>.Instance);
    }

    private static Customer MakeLead(string name, string? phone = null, string? website = null)
    {
        var c = new Customer(name, $"{name.ToLower().Replace(" ", ".")}@t.com");
        c.UpdateContactInfo(phone: phone, website: website);
        return c;
    }

    private static CustomerEngagementSummary Eng(Guid id, int opens = 0, int clicks = 0,
        int bounces = 0, DateTime? last = null) => new(id, opens, clicks, bounces, last);

    private static Customer MakePhase7Lead(
        string name,
        string? phone,
        string? website,
        WebsiteKind kind,
        LeadQuality? quality,
        EmailType? emailType,
        bool suspicious,
        bool eligible)
    {
        var c = new Customer(name, $"{name.ToLower().Replace(" ", ".")}@t.com");
        c.UpdateContactInfo(phone: phone, website: website);
        c.SetWebsiteKind(kind);
        c.UpdateClassification(quality, emailType, suspicious, eligible);
        return c;
    }

    // ── CalculateScore (função pura) ─────────────────────────────────────────

    [Fact]
    public void CalculateScore_ColdLead_NoDataNoEngagement_IsZeroish()
    {
        var lead = new Customer("Vazio");
        Assert.True(LeadScoringService.CalculateScore(lead, null, Now) < LeadScoringService.WarmThreshold);
    }

    [Fact]
    public void CalculateScore_ClickerIsHot_EvenWithThinProfile()
    {
        var lead = new Customer("Clicador", "c@t.com");
        var eng = Eng(lead.Id, opens: 2, clicks: 1, last: Now.AddDays(-2));
        // opens≥1 (+5) + click≥1 (+25) + recência ≤7d (+15) = 45... + fit 0 → ainda Hot
        var score = LeadScoringService.CalculateScore(lead, eng, Now);
        Assert.Equal(45, score);
    }

    [Fact]
    public void CalculateScore_FullProfileAndEngagement_IsHot()
    {
        var lead = MakeLead("Completo", phone: "27999990000", website: "https://x.com");
        lead.UpdateClassification(null, null, false, isEligibleForCampaigns: true);
        var eng = Eng(lead.Id, opens: 4, clicks: 2, last: Now.AddDays(-1));

        var score = LeadScoringService.CalculateScore(lead, eng, Now);
        // fit 25 (site 5 + tel 5 + elegível 5 + DDD ES 10) + opens 10 + click 25 + recência 15 = 75
        Assert.Equal(75, score);
    }

    [Fact]
    public void CalculateScore_BouncePenalizesHard()
    {
        var lead = MakeLead("Bounce", phone: "27999990000", website: "https://x.com");
        var withBounce = LeadScoringService.CalculateScore(lead, Eng(lead.Id, bounces: 1), Now);
        var without = LeadScoringService.CalculateScore(lead, null, Now);
        Assert.Equal(Math.Max(0, without - 30), withBounce); // -30, com clamp em 0
        Assert.True(withBounce < without);
    }

    [Fact]
    public void CalculateScore_OldEngagement_GetsSmallerRecencyBonus()
    {
        var lead = new Customer("Antigo");
        var recent = LeadScoringService.CalculateScore(lead, Eng(lead.Id, opens: 1, last: Now.AddDays(-3)), Now);
        var old = LeadScoringService.CalculateScore(lead, Eng(lead.Id, opens: 1, last: Now.AddDays(-20)), Now);
        var ancient = LeadScoringService.CalculateScore(lead, Eng(lead.Id, opens: 1, last: Now.AddDays(-90)), Now);
        Assert.True(recent > old);
        Assert.True(old > ancient);
    }

    [Fact]
    public void CalculateScore_CompleteRegistration_NoEngagement_DDD11_IsCold()
    {
        var lead = MakeLead("Cadastro Completo SP", phone: "11999990000", website: "https://x.com");
        lead.UpdateClassification(null, null, false, isEligibleForCampaigns: true);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.True(score < LeadScoringService.WarmThreshold);
    }

    [Fact]
    public void CalculateScore_CompleteRegistration_NoEngagement_DDD27_StillCold()
    {
        var lead = MakeLead("Cadastro Completo ES", phone: "27999990000", website: "https://x.com");
        lead.UpdateClassification(null, null, false, isEligibleForCampaigns: true);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        // fit sozinho (máx 25: site+tel+elegível+DDD local) nunca chega a Warm (30)
        Assert.True(score < LeadScoringService.WarmThreshold);
    }

    [Fact]
    public void CalculateScore_CompleteRegistration_DDD27_PlusOneOpen_ReachesWarm()
    {
        var lead = MakeLead("Cadastro Completo ES Aberto", phone: "27999990000", website: "https://x.com");
        lead.UpdateClassification(null, null, false, isEligibleForCampaigns: true);
        var eng = Eng(lead.Id, opens: 1);

        var score = LeadScoringService.CalculateScore(lead, eng, Now);

        Assert.True(score >= LeadScoringService.WarmThreshold);
    }

    [Fact]
    public void CalculateScore_ClickWithinWeek_EsPhoneAndWebsite_IsHot()
    {
        var lead = MakeLead("Quente ES", phone: "27999990000", website: "https://x.com");
        var eng = Eng(lead.Id, clicks: 1, last: Now.AddDays(-2));

        var score = LeadScoringService.CalculateScore(lead, eng, Now);

        Assert.True(score >= LeadScoringService.HotThreshold);
    }

    [Fact]
    public void CalculateScore_QualifiedStatus_WithoutEmailEvents_ReachesWarm()
    {
        var lead = MakeLead("Qualificado Sem Email", phone: "27999990000");
        lead.UpdateStatus(CustomerStatus.Qualified);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.True(score >= LeadScoringService.WarmThreshold);
    }

    [Fact]
    public void CalculateScore_Bounce_DropsOtherwiseHotLead_BelowHotThreshold()
    {
        var lead = MakeLead("Quente Com Bounce", phone: "27999990000", website: "https://x.com");
        var eng = Eng(lead.Id, clicks: 1, bounces: 1, last: Now.AddDays(-2));

        var score = LeadScoringService.CalculateScore(lead, eng, Now);

        Assert.True(score < LeadScoringService.HotThreshold);
    }

    [Fact]
    public void CalculateScore_PhoneWithCountryCodeAndFormatting_StillDetectsEsDdd()
    {
        var esLead = MakeLead("ES Formatado", phone: "+55 (28) 99999-0000");
        var spLead = MakeLead("SP Formatado", phone: "(11) 99999-0000");

        var esScore = LeadScoringService.CalculateScore(esLead, null, Now);
        var spScore = LeadScoringService.CalculateScore(spLead, null, Now);

        Assert.Equal(spScore + 10, esScore); // só o bônus de DDD local diferencia os dois
    }

    [Fact]
    public void CalculateScore_StrongPhase7Signals_ReachesWarmWithoutEngagement()
    {
        // site 5 + telefone 5 + elegível 5 + DDD 27 → 10 + OwnSite 10 + High 5 + PersonalDirect 5 = 45
        var lead = MakePhase7Lead("Clinica Forte", "27999990000", "https://clinicaodontovix.com.br",
            WebsiteKind.OwnSite, LeadQuality.High, EmailType.PersonalDirect,
            suspicious: false, eligible: true);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.Equal(45, score);
        Assert.Equal(LeadSegment.Warm, LeadScoringService.SegmentForScore(score));
    }

    [Fact]
    public void CalculateScore_WeakPhase7Signals_StaysColdWithoutEngagement()
    {
        // site 5 + telefone 5 + elegível 5 + DDD 27 → 10 + Directory 0 + Medium 0 + Generic 0 = 25
        var lead = MakePhase7Lead("Lead Diretorio", "27999990000",
            "https://www.econodata.com.br/empresa/123",
            WebsiteKind.Directory, LeadQuality.Medium, EmailType.GenericCorporate,
            suspicious: false, eligible: true);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.Equal(25, score);
        Assert.Equal(LeadSegment.Cold, LeadScoringService.SegmentForScore(score));
    }

    [Fact]
    public void CalculateScore_FitCeiling_NeverReachesHotWithoutEngagement()
    {
        var lead = MakePhase7Lead("Teto Do Fit", "27999990000", "https://clinicaodontovix.com.br",
            WebsiteKind.OwnSite, LeadQuality.High, EmailType.PersonalDirect,
            suspicious: false, eligible: true);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.Equal(45, score); // teto do bloco de fit
        Assert.True(score < LeadScoringService.HotThreshold);
        Assert.NotEqual(LeadSegment.Hot, LeadScoringService.SegmentForScore(score));
    }

    [Fact]
    public void CalculateScore_SuspiciousDomain_DropsStrongLeadBackToCold()
    {
        // suspeito ⇒ não elegível (invariante do LeadSanitizationService):
        // 5 + 5 + 0 + 10 + 10 + 5 + 5 − 15 = 25
        var lead = MakePhase7Lead("Dominio Suspeito", "27999990000", "https://clinicaodontovix.com.br",
            WebsiteKind.OwnSite, LeadQuality.High, EmailType.PersonalDirect,
            suspicious: true, eligible: false);

        var score = LeadScoringService.CalculateScore(lead, null, Now);

        Assert.Equal(25, score);
        Assert.Equal(LeadSegment.Cold, LeadScoringService.SegmentForScore(score));
    }

    [Theory]
    [InlineData(0, LeadSegment.Cold)]
    [InlineData(29, LeadSegment.Cold)]
    [InlineData(30, LeadSegment.Warm)]
    [InlineData(59, LeadSegment.Warm)]
    [InlineData(60, LeadSegment.Hot)]
    [InlineData(100, LeadSegment.Hot)]
    public void SegmentForScore_MapsThresholdsExactly(int score, LeadSegment expected)
        => Assert.Equal(expected, LeadScoringService.SegmentForScore(score));

    // ── RecomputeAllAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RecomputeAllAsync_SegmentsLeads_AndPersists()
    {
        var hotLead = MakeLead("Quente", phone: "27999990000", website: "https://x.com");
        hotLead.UpdateClassification(null, null, false, true);
        var coldLead = new Customer("Frio");

        _customers.Setup(r => r.GetAllLeadsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { hotLead, coldLead });
        _events.Setup(r => r.GetEngagementSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerEngagementSummary>
            {
                Eng(hotLead.Id, opens: 3, clicks: 1, last: DateTime.UtcNow.AddDays(-1)),
            });

        var summary = await CreateService().RecomputeAllAsync();

        Assert.Equal(2, summary.LeadsScored);
        Assert.Equal(1, summary.Hot);
        Assert.Equal(1, summary.Cold);
        Assert.Equal(LeadSegment.Hot, hotLead.Segment);
        Assert.Equal(LeadSegment.Cold, coldLead.Segment);
        Assert.Equal(0, summary.FollowUpTasksCreated); // sem owner → sem tasks
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecomputeAllAsync_CreatesFollowUps_ForHotIdleLeads_WithDedup()
    {
        var owner = Guid.NewGuid();
        var hotIdle = MakeLead("Quente Parado", phone: "27999990000", website: "https://x.com");
        hotIdle.UpdateClassification(null, null, false, true);
        var hotWithTask = MakeLead("Quente Com Task", phone: "27888880000", website: "https://y.com");
        hotWithTask.UpdateClassification(null, null, false, true);
        var hotRecent = MakeLead("Quente Recente", phone: "27777770000", website: "https://z.com");
        hotRecent.UpdateClassification(null, null, false, true);
        hotRecent.RegisterContact(); // contactado agora → não vira follow-up

        var leads = new List<Customer> { hotIdle, hotWithTask, hotRecent };
        _customers.Setup(r => r.GetAllLeadsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(leads);
        _events.Setup(r => r.GetEngagementSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leads.Select(l => Eng(l.Id, opens: 3, clicks: 1, last: DateTime.UtcNow.AddDays(-1))).ToList());

        var service = new LeadScoringService(_customers.Object, _events.Object, _tasks.Object, _uow.Object,
            NullLogger<LeadScoringService>.Instance);
        _tasks.Setup(t => t.GetCustomerIdsWithOpenTasksAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { hotWithTask.Id }); // já tem task aberta

        var created = new List<TaskItem>();
        _tasks.Setup(t => t.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((t, _) => created.Add(t))
            .ReturnsAsync((TaskItem t, CancellationToken _) => t);

        var summary = await service.RecomputeAllAsync(owner);

        Assert.Equal(1, summary.FollowUpTasksCreated); // só o quente parado sem task
        var task = Assert.Single(created);
        Assert.Equal(hotIdle.Id, task.CustomerId);
        Assert.Equal(owner, task.UserId);
        Assert.Equal(TaskItemPriority.High, task.Priority);
        Assert.Contains("Quente Parado", task.Title);
    }

    [Fact]
    public async Task RecomputeAllAsync_CapsFollowUps()
    {
        var owner = Guid.NewGuid();
        var leads = Enumerable.Range(1, LeadScoringService.MaxFollowUpsPerRun + 5)
            .Select(i =>
            {
                var l = MakeLead($"Quente {i}", phone: $"2799999{i:D4}", website: "https://x.com");
                l.UpdateClassification(null, null, false, true);
                return l;
            }).ToList();

        _customers.Setup(r => r.GetAllLeadsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(leads);
        _events.Setup(r => r.GetEngagementSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leads.Select(l => Eng(l.Id, opens: 3, clicks: 1, last: DateTime.UtcNow.AddDays(-1))).ToList());

        var summary = await CreateService().RecomputeAllAsync(owner);

        Assert.Equal(LeadScoringService.MaxFollowUpsPerRun, summary.FollowUpTasksCreated);
    }
}
