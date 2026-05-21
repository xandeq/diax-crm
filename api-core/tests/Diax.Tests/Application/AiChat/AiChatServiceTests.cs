using Diax.Application.AiChat;
using Diax.Application.AiChat.Dtos;
using Diax.Domain.AiChat;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.AiChat;

// ─────────────────────────────────────────────────────────────────────────────
// Shared builder
// ─────────────────────────────────────────────────────────────────────────────

public abstract class AiChatServiceTestBase
{
    protected readonly Mock<IAiChatRepository> Repo = new();
    protected readonly Mock<IAnthropicChatClient> Anthropic = new();
    protected readonly Mock<IUnitOfWork> Uow = new();

    protected AiChatService Build() => new(
        Repo.Object,
        Anthropic.Object,
        Uow.Object,
        NullLogger<AiChatService>.Instance);

    /// <summary>
    /// Async-enumerable helper — Moq não suporta IAsyncEnumerable nativamente.
    /// </summary>
    protected static async IAsyncEnumerable<AnthropicStreamEvent> FakeStream(
        IEnumerable<AnthropicStreamEvent> events)
    {
        foreach (var e in events)
            yield return e;
        await Task.CompletedTask; // satisfaz o compilador
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TEST 1 — Streaming end-to-end: nova conversa → chunks SSE → persistência
// ─────────────────────────────────────────────────────────────────────────────

public class AiChatServiceStreamTests : AiChatServiceTestBase
{
    [Fact]
    public async Task StreamChatAsync_NewConversation_YieldsExpectedChunksAndPersists()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var userId = Guid.NewGuid();
        var usage = new AnthropicUsage(100, 50, 0, 0);

        // AddAsync devolve a entidade recebida (simula repository in-memory)
        Repo.Setup(r => r.AddAsync(It.IsAny<AiConversation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConversation c, CancellationToken _) => c);

        Uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Stream fake: start → dois deltas de texto → delta de parada → stop
        var fakeEvents = new AnthropicStreamEvent[]
        {
            new AnthropicMessageStartEvent("msg_fake", usage),
            new AnthropicTextDeltaEvent("Olá"),
            new AnthropicTextDeltaEvent(" mundo"),
            new AnthropicMessageDeltaEvent("end_turn", usage),
            new AnthropicMessageStopEvent(),
        };
        Anthropic.Setup(a => a.StreamMessageAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()))
            .Returns(FakeStream(fakeEvents));

        // CompleteAsync para geração de título (chamado após primeira conversa)
        Anthropic.Setup(a => a.CompleteAsync(It.IsAny<AnthropicMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AnthropicCompletionResult("Saudação simples", new AnthropicUsage(10, 5, 0, 0)));

        var request = new ChatRequestDto
        {
            ConversationId = null,
            Message = "Olá!",
            Model = "claude-sonnet-4-5",
            SystemPrompt = null,
        };

        // ── Act ───────────────────────────────────────────────────────────────
        var svc = Build();
        var chunks = new List<ChatStreamChunkDto>();
        await foreach (var chunk in svc.StreamChatAsync(userId, request, CancellationToken.None))
            chunks.Add(chunk);

        // ── Assert — tipos de chunk emitidos ──────────────────────────────────
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.ConversationStarted);
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.ContentDelta && c.Delta == "Olá");
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.ContentDelta && c.Delta == " mundo");
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.Usage);
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.MessageStop && c.StopReason == "end_turn");
        Assert.Contains(chunks, c => c.Type == ChatStreamChunkType.Title);
        Assert.DoesNotContain(chunks, c => c.Type == ChatStreamChunkType.Error);

        // ── Assert — persistência ─────────────────────────────────────────────
        // AddAsync deve ter sido chamado 1x (nova conversa)
        Repo.Verify(r => r.AddAsync(It.IsAny<AiConversation>(), It.IsAny<CancellationToken>()), Times.Once);

        // UpdateAsync deve ter sido chamado para persistir resposta final
        Repo.Verify(r => r.UpdateAsync(It.IsAny<AiConversation>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // SaveChangesAsync deve ter sido chamado múltiplas vezes (msg user, placeholder, final, título)
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TEST 2 — Cálculo de custo com cache hit retorna USD correto
// ─────────────────────────────────────────────────────────────────────────────

public class AiChatServiceCostTests
{
    // Preços claude-sonnet-4-5 (por 1M tokens):
    //   Input:       $3.00   → 1k tokens = $0.003000
    //   Output:      $15.00  → 500 tokens = $0.007500
    //   CacheRead:   $0.30   → 900 tokens = $0.000270
    //   CacheWrite:  $3.75   → 0 tokens   = $0.000000
    //
    // Total esperado: $0.003000 + $0.007500 + $0.000270 = $0.010770

    [Fact]
    public void CalculateCost_SonnetWithCacheHit_ReturnsCorrectUsd()
    {
        var usage = new AnthropicUsage(
            InputTokens: 1_000,
            OutputTokens: 500,
            CacheReadInputTokens: 900,
            CacheCreationInputTokens: 0);

        var cost = AiChatService.CalculateCost("claude-sonnet-4-5", usage);

        Assert.Equal(0.010770m, cost);
    }

    [Fact]
    public void CalculateCost_SonnetZeroUsage_ReturnsZero()
    {
        var usage = new AnthropicUsage(0, 0, 0, 0);
        var cost = AiChatService.CalculateCost("claude-sonnet-4-5", usage);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void CalculateCost_UnknownModel_FallsBackToSonnetPricing()
    {
        // Modelo inexistente deve usar preços do Sonnet como fallback
        var usage = new AnthropicUsage(1_000_000, 0, 0, 0);
        var costUnknown = AiChatService.CalculateCost("claude-unknown-model", usage);
        var costSonnet = AiChatService.CalculateCost("claude-sonnet-4-5", usage);
        Assert.Equal(costSonnet, costUnknown);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TEST 3 — Multi-tenancy: usuário B não acessa conversa do usuário A
// ─────────────────────────────────────────────────────────────────────────────

public class AiChatServiceMultiTenancyTests : AiChatServiceTestBase
{
    [Fact]
    public async Task GetConversationAsync_WrongUser_ReturnsNotFound()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid(); // usuário que tenta acessar conversa alheia
        var convId = Guid.NewGuid();

        // Simula o filtro de multi-tenancy: repository retorna null quando userB
        // tenta acessar uma conversa que pertence a userA.
        Repo.Setup(r => r.GetWithMessagesAsync(convId, userB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConversation?)null);

        // Garante que a conversa EXISTE para userA (não é um 404 de verdade)
        var conv = new AiConversation(userA, "Conversa do A", "claude-sonnet-4-5");
        Repo.Setup(r => r.GetWithMessagesAsync(convId, userA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conv);

        var svc = Build();

        // ── Act ───────────────────────────────────────────────────────────────
        var resultB = await svc.GetConversationAsync(convId, userB, CancellationToken.None);
        var resultA = await svc.GetConversationAsync(convId, userA, CancellationToken.None);

        // ── Assert ────────────────────────────────────────────────────────────
        Assert.False(resultB.IsSuccess, "Usuário B não deve ver a conversa de A");
        Assert.Equal("NotFound", resultB.Error.Code);

        Assert.True(resultA.IsSuccess, "Usuário A deve ver sua própria conversa");
        Assert.Equal("Conversa do A", resultA.Value!.Title);
    }

    [Fact]
    public async Task ArchiveConversationAsync_WrongUser_ReturnsNotFound()
    {
        // Garante que archive também respeita o multi-tenancy
        var userB = Guid.NewGuid();
        var convId = Guid.NewGuid();

        Repo.Setup(r => r.GetByIdForUserAsync(convId, userB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiConversation?)null);

        var result = await Build().ArchiveConversationAsync(convId, userB, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error.Code);
        Repo.Verify(r => r.UpdateAsync(It.IsAny<AiConversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
