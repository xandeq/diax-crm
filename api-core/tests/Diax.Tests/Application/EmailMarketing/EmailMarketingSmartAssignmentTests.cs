using System.Linq.Expressions;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Pro.Dtos;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Snippets;
using Moq;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// QueueWithSmartAssignmentAsync — o caminho usado pelas campanhas reais.
/// Cobre P0-2 (opt-out/supressão eram IGNORADOS aqui), a regressão do PR #31
/// (RegisterEmailSent) e o parse estrito de provider (fim do default silencioso Brevo).
/// </summary>
public class EmailMarketingSmartAssignmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IEmailQueueRepository> _queueRepo = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressionRepo = new();
    private readonly Mock<IPilotCircuitBreaker> _pilotBreaker = new();
    private readonly List<EmailQueueItem> _queued = [];

    private EmailMarketingService BuildService(params string[] disabledProviders)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(s => s.UserId).Returns(_userId);

        _pilotBreaker.Setup(b => b.IsOpen).Returns(false);

        _queueRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _queueRepo
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EmailQueueItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<EmailQueueItem>, CancellationToken>((items, _) => _queued.AddRange(items))
            .Returns(Task.CompletedTask);

        return new EmailMarketingService(
            _queueRepo.Object,
            _campaignRepo.Object,
            _customerRepo.Object,
            new Mock<ISnippetRepository>().Object,
            new Mock<IEmailTemplateEngine>().Object,
            currentUser.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IEmailSender>().Object,
            _suppressionRepo.Object,
            _pilotBreaker.Object,
            new Mock<IAuditLogRepository>().Object,
            EmailTestDefaults.LinkBuilder(),
            EmailTestDefaults.ProviderPolicy(disabledProviders)
        );
    }

    private EmailCampaign SetupReadyCampaign()
    {
        // Corpo com unsubscribe e sem href — passa os readiness gates; Scheduled ≠ Draft.
        var campaign = new EmailCampaign(_userId, "Campanha", "Assunto",
            "Olá {{nome}} — para sair: {{unsubscribe_url}}");
        campaign.Schedule(DateTime.UtcNow.AddHours(1));

        _campaignRepo
            .Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        return campaign;
    }

    private Customer SetupCustomer(string name, string email, bool optOut = false, bool suppressed = false)
    {
        var customer = new Customer(name, email, PersonType.Company, LeadSource.Import);
        if (optOut)
        {
            customer.OptOutEmail();
        }

        _suppressionRepo
            .Setup(r => r.IsSuppressedAsync(_userId, email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suppressed);

        return customer;
    }

    private void SetupCustomerLookup(params Customer[] customers)
    {
        _customerRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers.ToList());
    }

    [Fact]
    public async Task QueueWithSmartAssignment_SkipsOptOutAndSuppressed()
    {
        // Regressão P0-2: o caminho legado checava opt-out/supressão, este NÃO —
        // quem se descadastrou voltava a receber campanha.
        var campaign = SetupReadyCampaign();
        var ok = SetupCustomer("Ok", "ok@a.com");
        var optedOut = SetupCustomer("OptOut", "optout@a.com", optOut: true);
        var suppressed = SetupCustomer("Suprimido", "sup@a.com", suppressed: true);
        SetupCustomerLookup(ok, optedOut, suppressed);

        var service = BuildService();
        var result = await service.QueueWithSmartAssignmentAsync(new QueueWithAssignmentRequest
        {
            CampaignId = campaign.Id,
            Leads =
            [
                new AssignedLeadQueueDto { CustomerId = ok.Id, AssignedProvider = "brevo" },
                new AssignedLeadQueueDto { CustomerId = optedOut.Id, AssignedProvider = "brevo" },
                new AssignedLeadQueueDto { CustomerId = suppressed.Id, AssignedProvider = "brevo" },
            ]
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.QueuedCount);
        Assert.Equal(2, result.Value.SkippedCount);
        Assert.Contains(result.Value.SkippedRecipients, s => s.Contains("opt-out"));
        Assert.Contains(result.Value.SkippedRecipients, s => s.Contains("suprimido"));
        Assert.Single(_queued);
        Assert.Equal("ok@a.com", _queued[0].RecipientEmail);
    }

    [Fact]
    public async Task QueueWithSmartAssignment_CallsRegisterEmailSent_OnlyForQueuedCustomers()
    {
        // Regressão do PR #31: sem RegisterEmailSent, EmailSentCount ficava 0 e o
        // dedup cross-day não funcionava.
        var campaign = SetupReadyCampaign();
        var queuedCustomer = SetupCustomer("Ok", "ok@a.com");
        var skippedCustomer = SetupCustomer("OptOut", "optout@a.com", optOut: true);
        SetupCustomerLookup(queuedCustomer, skippedCustomer);

        var service = BuildService();
        await service.QueueWithSmartAssignmentAsync(new QueueWithAssignmentRequest
        {
            CampaignId = campaign.Id,
            Leads =
            [
                new AssignedLeadQueueDto { CustomerId = queuedCustomer.Id, AssignedProvider = "sendgrid" },
                new AssignedLeadQueueDto { CustomerId = skippedCustomer.Id, AssignedProvider = "sendgrid" },
            ]
        });

        Assert.Equal(1, queuedCustomer.EmailSentCount);
        Assert.NotNull(queuedCustomer.LastEmailSentAt);
        Assert.Equal(0, skippedCustomer.EmailSentCount);
    }

    [Fact]
    public async Task QueueWithSmartAssignment_UnknownProviderName_IsSkipped_NotSilentlyBrevo()
    {
        // Antes: typo no nome do provider caía silenciosamente em Brevo e
        // desequilibrava os limites diários.
        var campaign = SetupReadyCampaign();
        var customer = SetupCustomer("Typo", "typo@a.com");
        SetupCustomerLookup(customer);

        var service = BuildService();
        var result = await service.QueueWithSmartAssignmentAsync(new QueueWithAssignmentRequest
        {
            CampaignId = campaign.Id,
            Leads = [new AssignedLeadQueueDto { CustomerId = customer.Id, AssignedProvider = "sendgrind" }]
        });

        Assert.True(result.IsFailure); // nenhum destinatário válido restou
        Assert.Empty(_queued);
    }

    [Fact]
    public async Task QueueWithSmartAssignment_DisabledProvider_IsSkipped()
    {
        var campaign = SetupReadyCampaign();
        var customer = SetupCustomer("Dead", "dead@a.com");
        SetupCustomerLookup(customer);

        var service = BuildService("mailersend");
        var result = await service.QueueWithSmartAssignmentAsync(new QueueWithAssignmentRequest
        {
            CampaignId = campaign.Id,
            Leads = [new AssignedLeadQueueDto { CustomerId = customer.Id, AssignedProvider = "mailersend" }]
        });

        Assert.True(result.IsFailure);
        Assert.Empty(_queued);
    }

    [Fact]
    public async Task QueueWithSmartAssignment_EmptyProvider_FallsBackToFirstEnabled()
    {
        var campaign = SetupReadyCampaign();
        var customer = SetupCustomer("Sem provider", "np@a.com");
        SetupCustomerLookup(customer);

        var service = BuildService("brevo"); // primeiro habilitado passa a ser mailjet
        var result = await service.QueueWithSmartAssignmentAsync(new QueueWithAssignmentRequest
        {
            CampaignId = campaign.Id,
            Leads = [new AssignedLeadQueueDto { CustomerId = customer.Id, AssignedProvider = "" }]
        });

        Assert.True(result.IsSuccess);
        Assert.Single(_queued);
        Assert.Equal(EmailProvider.Mailjet, _queued[0].AssignedProvider);
    }
}

/// <summary>
/// Round-trip do link de unsubscribe: o token gerado nos emails DEVE ser aceito pelo
/// EmailUnsubscribeController (mesma chave, mesmo algoritmo). Regressão do P0-1.
/// </summary>
public class UnsubscribeLinkBuilderTests
{
    [Fact]
    public void BuildUnsubscribeUrl_PointsToPublicHost_WithHmacToken()
    {
        var builder = EmailTestDefaults.LinkBuilder();
        var userId = Guid.NewGuid();

        var url = builder.BuildUnsubscribeUrl(userId, "lead@example.com");

        Assert.StartsWith($"{EmailTestDefaults.PublicBaseUrl}/unsubscribe?token=", url);
        Assert.DoesNotContain("diaxcrm.com.br", url);   // domínio morto — nunca mais
        Assert.DoesNotContain("?email=", url);          // formato antigo sem token
    }

    [Fact]
    public void GeneratedToken_MatchesControllerValidation()
    {
        var userId = Guid.NewGuid();
        const string email = "lead@example.com";

        var fromBuilder = UnsubscribeLinkBuilder.ComputeToken(EmailTestDefaults.SigningKey, userId.ToString(), email);
        var fromController = Diax.Api.Controllers.V1.EmailUnsubscribeController.ComputeToken(
            EmailTestDefaults.SigningKey, userId.ToString(), email);

        Assert.Equal(fromBuilder, fromController);

        var url = EmailTestDefaults.LinkBuilder().BuildUnsubscribeUrl(userId, email);
        Assert.Contains(Uri.EscapeDataString(fromBuilder), url);
    }

    [Fact]
    public void Token_IsBase64Url_NoPaddingOrUnsafeChars()
    {
        var token = UnsubscribeLinkBuilder.ComputeToken("key", Guid.NewGuid().ToString(), "a@b.com");

        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }
}
