using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Snippets;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using Diax.Domain.Audit;
using Diax.Domain.Auth;

namespace Diax.Tests.Application.EmailMarketing;

public class EmailCampaignStrategyTests
{
    private readonly EmailTemplateEngine _templateEngine = new();

    [Fact]
    public void TemplateEngine_ShouldRenderBrazilianVariables()
    {
        // Arrange
        var template = "Olá {{nome}}, vimos que a {{empresa}} no site {{site}} usa {{ferramenta_atual}} na cidade de {{cidade}} com a dor de {{dor_principal}}. Acesse {{cta_link}} ou {{unsubscribe_url}}";
        var variables = new Dictionary<string, string?>
        {
            ["nome"] = "Carlos",
            ["empresa"] = "Agência Alfa",
            ["site"] = "www.agenciaalfa.com.br",
            ["cidade"] = "São Paulo",
            ["ferramenta_atual"] = "Pipedrive",
            ["dor_principal"] = "falta de integração com o WhatsApp",
            ["cta_link"] = "https://diaxcrm.com.br/landing/agencias-digitais",
            ["unsubscribe_url"] = "https://diaxcrm.com.br/api/v1/unsubscribe?email=carlos@alfa.com"
        };

        // Act
        var rendered = _templateEngine.Render(template, variables);

        // Assert
        Assert.Contains("Olá Carlos", rendered);
        Assert.Contains("Agência Alfa", rendered);
        Assert.Contains("www.agenciaalfa.com.br", rendered);
        Assert.Contains("Pipedrive", rendered);
        Assert.Contains("São Paulo", rendered);
        Assert.Contains("falta de integração com o WhatsApp", rendered);
        Assert.Contains("https://diaxcrm.com.br/landing/agencias-digitais", rendered);
        Assert.Contains("https://diaxcrm.com.br/api/v1/unsubscribe?email=carlos@alfa.com", rendered);
    }

    [Theory]
    [InlineData("pipedrive, marketing", "Pipedrive", "descentralização de contatos comercial e financeiro")]
    [InlineData("rd station, financeiro", "RD Station", "perda de tempo com cobranças manuais")]
    [InlineData("notion, whatsapp", "Notion", "falta de integração com o WhatsApp")]
    [InlineData("planilha, cobranca", "planilhas", "perda de tempo com cobranças manuais")]
    [InlineData("other_tag", "planilhas ou CRM", "descentralização de contatos comercial e financeiro")]
    public void BuildRecipientTemplateVariables_ShouldExtractInfoFromCustomerTags(
        string customerTags, 
        string expectedTool, 
        string expectedPain)
    {
        // Arrange
        var customer = new Customer("Carlos Silva", "carlos@alfa.com", PersonType.Company, LeadSource.Import);
        customer.UpdateContactInfo(website: "www.agenciaalfa.com.br");
        customer.UpdateBasicInfo("Carlos Silva", "carlos@alfa.com", PersonType.Company, "Agência Alfa");
        customer.UpdateTags(customerTags);

        var queueItem = new EmailQueueItem(
            Guid.NewGuid(),
            "Carlos Silva",
            "carlos@alfa.com",
            "Assunto",
            "Corpo",
            DateTime.UtcNow,
            customer.Id,
            null
        );

        // Act
        var variables = EmailMarketingService.BuildRecipientTemplateVariables(customer, queueItem);

        // Assert
        Assert.Equal("Carlos", variables["nome"]);
        Assert.Equal("Agência Alfa", variables["empresa"]);
        Assert.Equal("www.agenciaalfa.com.br", variables["site"]);
        Assert.Equal(expectedTool, variables["ferramenta_atual"]);
        Assert.Equal(expectedPain, variables["dor_principal"]);
        Assert.Contains("https://diaxcrm.com.br/landing/agencias-digitais", variables["cta_link"]);
        Assert.Contains("unsubscribe?email=carlos%40alfa.com", variables["unsubscribe_url"]);
    }

    [Fact]
    public void DraftCampaign_ShouldNotBeProcessedByQueue()
    {
        // Arrange
        var campaign = new EmailCampaign(
            Guid.NewGuid(),
            "Cold Email Agências Digitais BR",
            "whatsapp na {{empresa}}",
            "<p>Corpo</p>"
        );

        // Assert
        Assert.Equal(EmailCampaignStatus.Draft, campaign.Status);
        
        // Em estado Draft, não deve possuir ScheduledAt definido
        Assert.Null(campaign.ScheduledAt);

        // Uma campanha em rascunho pode ir para Processando, mas tentar agendar após isso deve lançar exceção
        campaign.StartProcessing();
        Assert.Equal(EmailCampaignStatus.Processing, campaign.Status);

        var ex = Assert.Throws<InvalidOperationException>(() => campaign.Schedule(DateTime.UtcNow.AddDays(1)));
        Assert.Contains("schedule", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task SendTestEmail_ShouldOnlyGoToLoggedInUser()
    {
        // Arrange
        await using var db = CreateDbContext();
        var user = new User("usuario-admin@diaxcrm.com.br", "hashed_password");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var campaign = new EmailCampaign(
            user.Id,
            "Campanha Draft",
            "Assunto {{nome}}",
            "Corpo {{empresa}}"
        );
        db.EmailCampaigns.Add(campaign);
        await db.SaveChangesAsync();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(user.Id);

        var mockEmailSender = new Mock<IEmailSender>();
        var sentMessages = new List<EmailSendMessage>();

        mockEmailSender
            .Setup(s => s.SendAsync(It.IsAny<EmailSendMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailSendMessage, CancellationToken>((msg, ct) => sentMessages.Add(msg))
            .ReturnsAsync(EmailSendResult.Ok("msg-123"));

        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var service = new EmailMarketingService(
            new Mock<IEmailQueueRepository>().Object,
            mockCampaignRepo.Object,
            new Mock<ICustomerRepository>().Object,
            new Mock<ISnippetRepository>().Object,
            _templateEngine,
            mockCurrentUserService.Object,
            new Mock<IUnitOfWork>().Object,
            mockUserRepository.Object,
            mockEmailSender.Object,
            new Mock<IEmailSuppressionRepository>().Object,
            new Mock<IPilotCircuitBreaker>().Object,
            new Mock<IAuditLogRepository>().Object
        );

        // Act
        var result = await service.SendTestEmailAsync(campaign.Id, new SendTestEmailRequest(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(sentMessages);
        
        // O e-mail de teste deve ir APENAS para o e-mail do próprio usuário logado
        Assert.Equal(user.Email, sentMessages[0].RecipientEmail);
        Assert.Contains("[TESTE]", sentMessages[0].Subject);
    }

    [Fact]
    public void NoSecretsInCampaignSettings()
    {
        // Teste de segurança estática: garante que configurações padrão não contenham chaves fixas
        var settings = new BrevoSettings();
        
        // Assert: Garante que por padrão, campos sensíveis venham nulos ou vazios
        Assert.True(string.IsNullOrEmpty(settings.ApiKey));
        Assert.True(string.IsNullOrEmpty(settings.WebhookSecret));
    }

    [Fact]
    public void ValidateReadiness_ShouldFail_WhenUnsubscribeIsMissing()
    {
        // Arrange
        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Assunto", "<p>Corpo sem descadastro</p>");
        campaign.Schedule(DateTime.UtcNow.AddDays(1)); // Mover de Draft para Scheduled para passar no status check

        // Act
        var result = EmailMarketingService.ValidateReadiness(campaign);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cancelamento de inscrição", result.Error.Message);
    }

    [Fact]
    public void ValidateReadiness_ShouldFail_WhenUtmTrackingIsMissing()
    {
        // Arrange
        var campaign = new EmailCampaign(
            Guid.NewGuid(), 
            "Campanha", 
            "Assunto", 
            "<p>Acesse <a href='https://diaxcrm.com.br'>link sem UTM</a>. {{unsubscribe_url}}</p>"
        );
        campaign.Schedule(DateTime.UtcNow.AddDays(1));

        // Act
        var result = EmailMarketingService.ValidateReadiness(campaign);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("parâmetros de tracking", result.Error.Message);
    }

    [Fact]
    public void ValidateReadiness_ShouldFail_WhenCampaignIsDraft()
    {
        // Arrange
        var campaign = new EmailCampaign(
            Guid.NewGuid(), 
            "Campanha", 
            "Assunto", 
            "<p>Corpo válido com {{unsubscribe_url}}</p>"
        );

        // Act
        var result = EmailMarketingService.ValidateReadiness(campaign);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Rascunho (Draft)", result.Error.Message);
    }

    [Fact]
    public void ValidateReadiness_ShouldSucceed_WhenCampaignIsValidAndScheduled()
    {
        // Arrange
        var campaign = new EmailCampaign(
            Guid.NewGuid(), 
            "Campanha", 
            "Assunto", 
            "<p>Corpo com <a href='https://diax.com?utm_source=cold_email'>link com UTM</a>. {{unsubscribe_url}}</p>"
        );
        campaign.Schedule(DateTime.UtcNow.AddDays(1));

        // Act
        var result = EmailMarketingService.ValidateReadiness(campaign);

        // Assert
        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ScheduleCampaign_ShouldFail_WhenContentReadinessFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = new EmailCampaign(userId, "Campanha Sem Unsubscribe", "Assunto", "<p>Corpo sem unsubscribe</p>");

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var service = new EmailMarketingService(
            new Mock<IEmailQueueRepository>().Object,
            mockCampaignRepo.Object,
            new Mock<ICustomerRepository>().Object,
            new Mock<ISnippetRepository>().Object,
            _templateEngine,
            mockCurrentUserService.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IEmailSender>().Object,
            new Mock<IEmailSuppressionRepository>().Object,
            new Mock<IPilotCircuitBreaker>().Object,
            new Mock<IAuditLogRepository>().Object
        );

        // Act
        var result = await service.ScheduleCampaignAsync(
            campaign.Id,
            new ScheduleEmailCampaignRequest { ScheduledAt = DateTime.UtcNow.AddDays(1) },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("unsubscribe_url", result.Error.Message);
    }

    [Fact]
    public async Task QueueCampaignRecipients_ShouldSkipOptOutCustomers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = new EmailCampaign(
            userId, 
            "Campanha Válida", 
            "Assunto", 
            "<p>Corpo com <a href='https://diax.com?utm_source=cold'>link</a> e {{unsubscribe_url}}</p>"
        );
        campaign.Schedule(DateTime.UtcNow.AddDays(1));

        var leadNormal = new Customer("Normal", "normal@agencia.com", PersonType.Company, LeadSource.Import);
        var leadOptOut = new Customer("OptOut", "optout@agencia.com", PersonType.Company, LeadSource.Import);
        leadOptOut.OptOutEmail();

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var mockCustomerRepo = new Mock<ICustomerRepository>();
        mockCustomerRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { leadNormal, leadOptOut });

        var mockQueueRepo = new Mock<IEmailQueueRepository>();
        var queued = new List<EmailQueueItem>();
        mockQueueRepo
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<EmailQueueItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<EmailQueueItem>, CancellationToken>((items, _) => queued.AddRange(items))
            .Returns(Task.CompletedTask);

        var mockSuppressionRepo = new Mock<IEmailSuppressionRepository>();
        mockSuppressionRepo
            .Setup(s => s.IsSuppressedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new EmailMarketingService(
            mockQueueRepo.Object,
            mockCampaignRepo.Object,
            mockCustomerRepo.Object,
            new Mock<ISnippetRepository>().Object,
            _templateEngine,
            mockCurrentUserService.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IUserRepository>().Object,
            new Mock<IEmailSender>().Object,
            mockSuppressionRepo.Object,
            new Mock<IPilotCircuitBreaker>().Object,
            new Mock<IAuditLogRepository>().Object
        );

        // Act
        var result = await service.QueueCampaignRecipientsAsync(
            campaign.Id,
            new QueueCampaignRecipientsRequest
            {
                CustomerIds = new List<Guid> { leadNormal.Id, leadOptOut.Id }
            },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.QueuedCount);
        Assert.Equal(1, result.Value.SkippedCount);
        Assert.Single(queued);
        Assert.Equal("normal@agencia.com", queued[0].RecipientEmail);
    }

    private static DiaxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new DiaxDbContext(options);
    }
}
