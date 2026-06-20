using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Diax.Api.Controllers.V1;
using Diax.Application.EmailMarketing;
using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Diax.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

public class BrevoWebhookHardeningTests
{
    private readonly Mock<IEmailQueueRepository> _queueRepoMock = new();
    private readonly Mock<IEmailCampaignRepository> _campaignRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<BrevoWebhookController>> _loggerMock = new();
    private readonly Mock<IPilotCircuitBreaker> _circuitBreakerMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly BrevoWebhookController _sut;

    public BrevoWebhookHardeningTests()
    {
        var settings = new BrevoSettings { WebhookSecret = "" }; // Permissive mode
        var options = Options.Create(settings);

        _sut = new BrevoWebhookController(
            _queueRepoMock.Object,
            _campaignRepoMock.Object,
            _customerRepoMock.Object,
            _unitOfWorkMock.Object,
            options,
            _loggerMock.Object,
            _circuitBreakerMock.Object,
            _auditLogRepoMock.Object,
            _userRepoMock.Object
        );
    }

    [Fact]
    public async Task HandleWebhook_OpenedEvent_FindsQueueItem_WhenMessageIdHasAngleBrackets()
    {
        // Regressão: Brevo armazena MessageId com <> na API de envio mas omite no webhook.
        // O handler deve casar ambos os formatos — sem isso openCount fica 0 sempre.
        var campaignId = Guid.NewGuid();
        var bareId = "202506191234.abc123@smtp-relay.mailin.fr";

        var payload = new BrevoWebhookPayload
        {
            Event = "opened",
            Email = "lead@empresa.com.br",
            MessageId = bareId, // webhook envia SEM brackets
            Tag = campaignId.ToString()
        };

        // Simula item armazenado COM brackets (comportamento do sender pré-fix)
        var queueItem = new EmailQueueItem(Guid.NewGuid(), "Lead", "lead@empresa.com.br", "Assunto", "Corpo",
            DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        queueItem.MarkSent($"<{bareId}>");

        _queueRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailQueueItem> { queueItem });

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Assunto", "<p>Corpo</p>");
        _campaignRepoMock
            .Setup(c => c.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _campaignRepoMock.Verify(c => c.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, campaign.OpenCount);
        Assert.NotNull(queueItem.OpenedAt);
    }

    [Fact]
    public async Task HandleWebhook_ShouldBeIdempotent_ForDeliveredEvent()
    {
        // Arrange
        var messageId = "msg-delivered-123";
        var payload = new BrevoWebhookPayload
        {
            Event = "delivered",
            Email = "carlos@agencia.com.br",
            MessageId = messageId,
            Tag = Guid.NewGuid().ToString()
        };

        var queueItem = new EmailQueueItem(Guid.NewGuid(), "Carlos", "carlos@agencia.com.br", "Assunto", "Corpo", DateTime.UtcNow, Guid.NewGuid(), null, Guid.Parse(payload.Tag));
        queueItem.MarkDelivered(); // Marca como já entregue para simular a duplicata
        
        _queueRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailQueueItem> { queueItem });

        // Act
        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Garante que não incrementou no banco novamente
        _campaignRepoMock.Verify(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_OpenedEvent_IncrementsCampaign_ViaQueueItemCampaignId_WhenTagIsNotGuid()
    {
        // Regressão: o Brevo às vezes ecoa o tag como array-string (["<guid>"]),
        // que Guid.TryParse rejeita. O contador da campanha deve subir mesmo assim,
        // resolvendo a campanha pelo CampaignId do item (como o fluxo de 'delivered').
        var campaignId = Guid.NewGuid();
        var payload = new BrevoWebhookPayload
        {
            Event = "opened",
            Email = "lead@empresa.com.br",
            MessageId = "msg-open-xyz",
            Tag = "[\"" + campaignId + "\"]" // NÃO é um GUID puro
        };

        var queueItem = new EmailQueueItem(Guid.NewGuid(), "Lead", "lead@empresa.com.br", "Assunto", "Corpo", DateTime.UtcNow, Guid.NewGuid(), null, campaignId);
        _queueRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailQueueItem> { queueItem });

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Assunto", "<p>Corpo</p>");
        _campaignRepoMock
            .Setup(c => c.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        Assert.IsType<OkResult>(result);
        _campaignRepoMock.Verify(c => c.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, campaign.OpenCount); // contador da campanha subiu
        Assert.NotNull(queueItem.OpenedAt);  // abertura registrada no item
    }

    [Fact]
    public async Task HandleWebhook_ShouldBeIdempotent_ForOpenedEvent()
    {
        // Arrange
        var messageId = "msg-opened-123";
        var payload = new BrevoWebhookPayload
        {
            Event = "opened",
            Email = "carlos@agencia.com.br",
            MessageId = messageId,
            Tag = Guid.NewGuid().ToString()
        };

        var queueItem = new EmailQueueItem(Guid.NewGuid(), "Carlos", "carlos@agencia.com.br", "Assunto", "Corpo", DateTime.UtcNow, Guid.NewGuid(), null, Guid.Parse(payload.Tag));
        queueItem.RecordOpen(); // Marca como já aberto

        _queueRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailQueueItem> { queueItem });

        // Act
        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Garante que não buscou a campanha para incrementar a abertura de novo
        _campaignRepoMock.Verify(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_ShouldOptOutAndRecordWebhookFailure_OnHardBounce()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var payload = new BrevoWebhookPayload
        {
            Event = "hard_bounce",
            Email = "invalido@agencia.com.br",
            MessageId = "msg-bounce-123",
            Tag = campaignId.ToString()
        };

        var customer = new Customer("Carlos Invalido", "invalido@agencia.com.br");
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("invalido@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var campaign = new EmailCampaign(Guid.NewGuid(), "Campanha", "Assunto", "<p>Corpo</p>");
        _campaignRepoMock
            .Setup(r => r.GetByIdAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        // Act
        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Deve marcar o cliente com opt-out (suprimir o e-mail morto)
        Assert.True(customer.EmailOptOut);

        // Deve registrar a falha de webhook (abre só após 3), NÃO abrir o breaker no 1º bounce
        _circuitBreakerMock.Verify(c => c.RecordWebhookFailure(), Times.Once);
        _circuitBreakerMock.Verify(c => c.Open(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_ShouldTrackWebhookFailure_OnException()
    {
        // Arrange
        var payload = new BrevoWebhookPayload
        {
            Event = "delivered",
            Email = "carlos@agencia.com.br",
            MessageId = "msg-error-123"
        };

        // Faz com que a busca lance uma exceção para simular falha no webhook
        _queueRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EmailQueueItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database offline"));

        // Act
        var result = await _sut.HandleWebhook(payload, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result); // Deve retornar OK mesmo em erro para evitar retry infinito do Brevo
        
        // Deve registrar a falha de webhook no circuit breaker
        _circuitBreakerMock.Verify(c => c.RecordWebhookFailure(), Times.Once);
    }
}
