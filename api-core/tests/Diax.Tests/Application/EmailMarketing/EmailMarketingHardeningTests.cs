using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Diax.Application.Common;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Domain.Snippets;
using Moq;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

public class EmailMarketingHardeningTests
{
    private readonly EmailTemplateEngine _templateEngine = new();

    [Fact]
    public async Task QueueCampaignRecipients_ShouldFail_WhenCircuitBreakerIsOpen()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = new EmailCampaign(userId, "Campanha Válida", "Assunto", "<p>Corpo com <a href='https://diax.com?utm_source=cold'>link</a> e {{unsubscribe_url}}</p>");
        campaign.Schedule(DateTime.UtcNow.AddDays(1));

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var mockCircuitBreaker = new Mock<IPilotCircuitBreaker>();
        mockCircuitBreaker.Setup(c => c.IsOpen).Returns(true);
        mockCircuitBreaker.Setup(c => c.Reason).Returns("Limite de erros excedido");

        var mockAuditRepo = new Mock<IAuditLogRepository>();
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
            mockCircuitBreaker.Object,
            mockAuditRepo.Object
        );

        // Act
        var result = await service.QueueCampaignRecipientsAsync(
            campaign.Id,
            new QueueCampaignRecipientsRequest { CustomerIds = new List<Guid> { Guid.NewGuid() } },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Circuit Breaker", result.Error.Message);
        
        // Verifica se o log PilotRealSendBlocked foi gerado
        mockAuditRepo.Verify(a => a.AddAsync(It.Is<AuditLogEntry>(e => e.NewValues.Contains("PilotRealSendBlocked")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueCampaignRecipients_ShouldSkipDuplicateLeads()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = new EmailCampaign(userId, "Campanha Válida", "Assunto", "<p>Corpo com <a href='https://diax.com?utm_source=cold'>link</a> e {{unsubscribe_url}}</p>");
        campaign.Schedule(DateTime.UtcNow.AddDays(1));

        var customer = new Customer("Carlos", "carlos@agencia.com.br", PersonType.Company, LeadSource.Import);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var mockCustomerRepo = new Mock<ICustomerRepository>();
        mockCustomerRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer });

        // Simula que já existe um item na fila para esse cliente
        var existingQueueItem = new EmailQueueItem(userId, "Carlos", "carlos@agencia.com.br", "Assunto", "Corpo", DateTime.UtcNow, customer.Id, null, campaign.Id);
        var mockQueueRepo = new Mock<IEmailQueueRepository>();
        mockQueueRepo.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailQueueItem> { existingQueueItem });

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
            new Mock<IEmailSuppressionRepository>().Object,
            new Mock<IPilotCircuitBreaker>().Object,
            new Mock<IAuditLogRepository>().Object
        );

        // Act
        var result = await service.QueueCampaignRecipientsAsync(
            campaign.Id,
            new QueueCampaignRecipientsRequest { CustomerIds = new List<Guid> { customer.Id } },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure); // Retorna falha porque nenhum destinatário novo válido foi enfileirado
        Assert.Contains("Nenhum destinatário válido", result.Error.Message);
    }

    [Fact]
    public async Task QueueCampaignRecipients_ShouldFail_WhenCampaignIsDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaign = new EmailCampaign(userId, "Campanha Draft", "Assunto", "<p>Corpo com <a href='https://diax.com?utm_source=cold'>link</a> e {{unsubscribe_url}}</p>");
        
        // Mantém em estado Draft
        Assert.Equal(EmailCampaignStatus.Draft, campaign.Status);

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var mockAuditRepo = new Mock<IAuditLogRepository>();

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
            mockAuditRepo.Object
        );

        // Act
        var result = await service.QueueCampaignRecipientsAsync(
            campaign.Id,
            new QueueCampaignRecipientsRequest { CustomerIds = new List<Guid> { Guid.NewGuid() } },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Rascunho (Draft)", result.Error.Message);
        
        // Verifica se registrou o motivo do bloqueio na trilha de auditoria
        mockAuditRepo.Verify(a => a.AddAsync(It.Is<AuditLogEntry>(e => e.NewValues.Contains("PilotReadinessBlocked")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTestEmail_ShouldNotAlterCampaignMetrics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("admin@diaxcrm.com.br", "hashed_password");
        var campaign = new EmailCampaign(userId, "Campanha Piloto", "Assunto", "<p>Corpo com {{unsubscribe_url}}</p>");

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);

        var mockCampaignRepo = new Mock<IEmailCampaignRepository>();
        mockCampaignRepo.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(campaign);

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var mockEmailSender = new Mock<IEmailSender>();
        mockEmailSender.Setup(s => s.SendAsync(It.IsAny<EmailSendMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailSendResult.Ok("msg-123"));

        var mockAuditRepo = new Mock<IAuditLogRepository>();

        var service = new EmailMarketingService(
            new Mock<IEmailQueueRepository>().Object,
            mockCampaignRepo.Object,
            new Mock<ICustomerRepository>().Object,
            new Mock<ISnippetRepository>().Object,
            _templateEngine,
            mockCurrentUserService.Object,
            new Mock<IUnitOfWork>().Object,
            mockUserRepo.Object,
            mockEmailSender.Object,
            new Mock<IEmailSuppressionRepository>().Object,
            new Mock<IPilotCircuitBreaker>().Object,
            mockAuditRepo.Object
        );

        // Act
        var result = await service.SendTestEmailAsync(campaign.Id, new SendTestEmailRequest(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Métricas da campanha devem permanecer inalteradas (zero)
        Assert.Equal(0, campaign.SentCount);
        Assert.Equal(0, campaign.DeliveredCount);
        
        // Evento de auditoria de SendTest requisitado e completado deve existir
        mockAuditRepo.Verify(a => a.AddAsync(It.Is<AuditLogEntry>(e => e.NewValues.Contains("PilotSendTestRequested")), It.IsAny<CancellationToken>()), Times.Once);
        mockAuditRepo.Verify(a => a.AddAsync(It.Is<AuditLogEntry>(e => e.NewValues.Contains("PilotSendTestCompleted")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
