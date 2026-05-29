using Diax.Application.Agents.Commercial;
using Diax.Application.Agents.Commercial.Dtos;
using Diax.Application.AiChat;
using Diax.Domain.Agents;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Diax.Tests.Application.Agents;

public class CommercialAgentServiceTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IAnthropicChatClient> _anthropic = new();

    private CommercialAgentService Build() => new(
        _customers.Object,
        _anthropic.Object,
        NullLogger<CommercialAgentService>.Instance);

    private static AnthropicCompletionResult Completion(string text)
        => new(text, new AnthropicUsage(100, 50, 0, 0));

    private static Customer Lead(string name, int score, LeadSegment segment)
    {
        var c = new Customer(name, $"{name}@ex.com");
        c.UpdateSegmentation(score, segment);
        return c;
    }

    [Fact]
    public async Task ChatAsync_EmptyMessage_ReturnsValidationFailure()
    {
        var svc = Build();

        var result = await svc.ChatAsync(Guid.NewGuid(), new CommercialAgentRequest(Message: "  "));

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
        _anthropic.Verify(a => a.CompleteAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatAsync_WithLeadIds_LoadsContext_AndReturnsReply()
    {
        var lead = Lead("Acme", 90, LeadSegment.Hot);
        _customers
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { lead });

        AnthropicMessageRequest? captured = null;
        _anthropic
            .Setup(a => a.CompleteAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AnthropicMessageRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(Completion("Priorize a Acme — lead Hot."));

        var svc = Build();
        var result = await svc.ChatAsync(
            Guid.NewGuid(),
            new CommercialAgentRequest(Message: "Quem priorizar?", LeadIds: new List<Guid> { lead.Id }));

        Assert.True(result.IsSuccess);
        Assert.Equal(AgentType.Commercial, result.Value.Agent);
        Assert.Equal("Priorize a Acme — lead Hot.", result.Value.Reply);
        Assert.Equal(1, result.Value.LeadsInContext);
        Assert.Equal(CommercialAgentService.DefaultModel, result.Value.Model);

        // System prompt deve conter o lead anexado; última mensagem é a do usuário
        Assert.NotNull(captured);
        Assert.Contains("Acme", captured!.System);
        Assert.Equal("user", captured.Messages[^1].Role);
        Assert.Equal("Quem priorizar?", captured.Messages[^1].Content);
    }

    [Fact]
    public async Task ChatAsync_NoLeadIds_QueriesBySegment()
    {
        _customers
            .Setup(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CustomerStatus?>(),
                It.IsAny<LeadSource?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(), It.IsAny<PersonType?>(), It.IsAny<LeadSegment?>(), It.IsAny<bool?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { Lead("Beta", 70, LeadSegment.Warm) }.AsEnumerable(), 1));

        _anthropic
            .Setup(a => a.CompleteAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Completion("ok"));

        var svc = Build();
        var result = await svc.ChatAsync(
            Guid.NewGuid(),
            new CommercialAgentRequest(Message: "Analise os warm", Segment: "Warm"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.LeadsInContext);
        _customers.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatAsync_LlmThrows_ReturnsLlmError()
    {
        _customers
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Customer>());
        _anthropic
            .Setup(a => a.CompleteAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("502"));

        var svc = Build();
        var result = await svc.ChatAsync(
            Guid.NewGuid(),
            new CommercialAgentRequest(Message: "oi", LeadIds: new List<Guid> { Guid.NewGuid() }));

        Assert.True(result.IsFailure);
        Assert.Equal("Agent.LlmError", result.Error.Code);
    }
}
