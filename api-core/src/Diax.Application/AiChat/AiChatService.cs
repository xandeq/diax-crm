using System.Runtime.CompilerServices;
using System.Text;
using Diax.Application.AiChat.Dtos;
using Diax.Domain.AiChat;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AiChat;

public class AiChatService : IAiChatService
{
    // Limites — devem casar com o spec do frontend
    public const int MaxAttachmentSizeBytes = 500 * 1024;       // 500KB por arquivo
    public const int MaxTotalAttachmentsBytes = 2 * 1024 * 1024; // 2MB total por msg
    public const int DefaultMaxTokens = 4096;
    public const string TitleModel = "claude-haiku-4-5";
    public const string DefaultTitle = "Nova conversa";

    /// <summary>Tabela de preços por 1M tokens (input, cacheWrite, cacheRead, output).</summary>
    private static readonly Dictionary<string, (decimal Input, decimal CacheWrite, decimal CacheRead, decimal Output)> Prices
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-sonnet-4-5"] = (3.00m, 3.75m, 0.30m, 15.00m),
            ["claude-haiku-4-5"] = (1.00m, 1.25m, 0.10m, 5.00m),
            ["claude-opus-4-7"] = (15.00m, 18.75m, 1.50m, 75.00m),
        };

    private readonly IAiChatRepository _repository;
    private readonly IAnthropicChatClient _anthropic;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IAiChatRepository repository,
        IAnthropicChatClient anthropic,
        IUnitOfWork uow,
        ILogger<AiChatService> logger)
    {
        _repository = repository;
        _anthropic = anthropic;
        _uow = uow;
        _logger = logger;
    }

    // ===== LIST =====

    public async Task<Result<PagedResult<ConversationListItemDto>>> ListConversationsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var paged = await _repository.GetPagedByUserAsync(userId, page, pageSize, includeArchived, cancellationToken);

        var items = paged.Items
            .Select(c => new ConversationListItemDto(
                c.Id,
                c.Title,
                c.Model,
                c.CreatedAt,
                c.UpdatedAt,
                c.IsArchived,
                c.Messages.Count))
            .ToList();

        var result = new PagedResult<ConversationListItemDto>(items, paged.TotalCount, paged.Page, paged.PageSize);
        return Result<PagedResult<ConversationListItemDto>>.Success(result);
    }

    // ===== GET =====

    public async Task<Result<ConversationDetailDto>> GetConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conv = await _repository.GetWithMessagesAsync(conversationId, userId, cancellationToken);
        if (conv is null)
            return Result.Failure<ConversationDetailDto>(new Error("NotFound", "Conversa não encontrada."));

        return Result<ConversationDetailDto>.Success(ToDetail(conv));
    }

    // ===== CREATE =====

    public async Task<Result<ConversationDetailDto>> CreateConversationAsync(
        Guid userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
            return Result.Failure<ConversationDetailDto>(new Error("Validation", "Modelo é obrigatório."));

        var title = string.IsNullOrWhiteSpace(request.Title) ? DefaultTitle : request.Title!;
        var conv = new AiConversation(userId, title, request.Model, request.SystemPrompt);

        await _repository.AddAsync(conv, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<ConversationDetailDto>.Success(ToDetail(conv));
    }

    // ===== UPDATE =====

    public async Task<Result<ConversationDetailDto>> UpdateConversationAsync(
        Guid conversationId,
        Guid userId,
        UpdateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var conv = await _repository.GetByIdForUserAsync(conversationId, userId, cancellationToken);
        if (conv is null)
            return Result.Failure<ConversationDetailDto>(new Error("NotFound", "Conversa não encontrada."));

        if (!string.IsNullOrWhiteSpace(request.Title))
            conv.SetTitle(request.Title!);

        if (request.SystemPrompt is not null)
            conv.SetSystemPrompt(request.SystemPrompt);

        if (request.IsArchived == true)
            conv.Archive();
        else if (request.IsArchived == false)
            conv.Unarchive();

        await _repository.UpdateAsync(conv, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Recarrega com mensagens para retornar o detail completo
        var refreshed = await _repository.GetWithMessagesAsync(conversationId, userId, cancellationToken);
        return Result<ConversationDetailDto>.Success(ToDetail(refreshed ?? conv));
    }

    // ===== ARCHIVE =====

    public async Task<Result<bool>> ArchiveConversationAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conv = await _repository.GetByIdForUserAsync(conversationId, userId, cancellationToken);
        if (conv is null)
            return Result.Failure<bool>(new Error("NotFound", "Conversa não encontrada."));

        conv.Archive();
        await _repository.UpdateAsync(conv, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    // ===== STREAM CHAT (core) =====

    public async IAsyncEnumerable<ChatStreamChunkDto> StreamChatAsync(
        Guid userId,
        ChatRequestDto request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // ----- Validações -----
        if (string.IsNullOrWhiteSpace(request.Message) && (request.Attachments is null || request.Attachments.Count == 0))
        {
            yield return new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: "Mensagem ou anexo obrigatório.");
            yield break;
        }

        if (request.Attachments is not null)
        {
            var totalSize = 0;
            foreach (var att in request.Attachments)
            {
                if (att.SizeBytes > MaxAttachmentSizeBytes)
                {
                    yield return new ChatStreamChunkDto(
                        ChatStreamChunkType.Error,
                        Error: $"Anexo '{att.FileName}' excede o limite de 500KB.");
                    yield break;
                }
                totalSize += att.SizeBytes;
            }
            if (totalSize > MaxTotalAttachmentsBytes)
            {
                yield return new ChatStreamChunkDto(
                    ChatStreamChunkType.Error,
                    Error: "Anexos excedem o limite total de 2MB.");
                yield break;
            }
        }

        // ----- Carrega ou cria conversa -----
        AiConversation? conv;
        bool conversationIsNew = false;
        if (request.ConversationId.HasValue)
        {
            conv = await _repository.GetWithMessagesAsync(request.ConversationId.Value, userId, cancellationToken);
            if (conv is null)
            {
                yield return new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: "Conversa não encontrada.");
                yield break;
            }
            // Atualiza modelo caso usuário tenha trocado no picker
            if (!string.Equals(conv.Model, request.Model, StringComparison.OrdinalIgnoreCase))
                conv.SetModel(request.Model);
        }
        else
        {
            conv = new AiConversation(userId, DefaultTitle, request.Model, request.SystemPrompt);
            await _repository.AddAsync(conv, cancellationToken);
            conversationIsNew = true;
        }

        // ----- Constrói conteúdo do user message com anexos -----
        // NÃO usamos conv.AddMessage() pois ele chama SetUpdated() em conv, o que marca
        // conv como Modified e causa um UPDATE desnecessário. Em vez disso, criamos
        // AiChatMessage diretamente e adicionamos via repositório — EF Core relationship
        // fix-up adiciona a mensagem a conv._messages automaticamente via FK.
        var userContent = BuildUserContent(request.Message, request.Attachments);
        var userMessage = new AiChatMessage(conv.Id, "user", userContent);
        if (request.Attachments is not null)
        {
            foreach (var att in request.Attachments)
                userMessage.AttachFile(att.FileName, att.ContentType, att.SizeBytes, att.Content);
        }
        await _repository.AddMessageAsync(userMessage, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        yield return new ChatStreamChunkDto(
            ChatStreamChunkType.ConversationStarted,
            ConversationId: conv.Id);

        // ----- Monta histórico para a Anthropic -----
        // conv.Messages via _messages backing field — relationship fix-up já adicionou
        // userMessage aqui quando foi feito AddMessageAsync. Inclui histórico completo
        // de conversas existentes + a nova mensagem do usuário.
        var anthropicMessages = conv.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AnthropicMessage(m.Role, m.Content))
            .ToList();

        var anthropicRequest = new AnthropicMessageRequest(
            Model: conv.Model,
            Messages: anthropicMessages,
            MaxTokens: DefaultMaxTokens,
            System: conv.SystemPrompt,
            EnablePromptCache: true);

        // ----- Stream para o cliente -----
        var assistantBuffer = new StringBuilder();
        AnthropicUsage? finalUsage = null;
        string? stopReason = null;
        bool errored = false;
        bool wasCancelled = false;

        // Cria mensagem placeholder do assistente — será atualizada ao final com usage + cost.
        // Mesmo padrão do userMessage: via repositório direto para evitar UPDATE em conv.
        var assistantMessage = new AiChatMessage(conv.Id, "assistant", string.Empty);
        await _repository.AddMessageAsync(assistantMessage, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        IAsyncEnumerator<AnthropicStreamEvent>? enumerator = null;
        string? startFailureMessage = null;
        try
        {
            enumerator = _anthropic.StreamMessageAsync(anthropicRequest, cancellationToken).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiChatService] Failed to start Anthropic stream");
            startFailureMessage = "Falha ao iniciar streaming com Anthropic.";
        }

        if (startFailureMessage is not null)
        {
            yield return new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: startFailureMessage);
            yield break;
        }

        try
        {
            while (true)
            {
                bool hasNext;
                AnthropicStreamEvent? evt = null;
                string? loopErrorMessage = null;
                bool loopCancelled = false;
                try
                {
                    hasNext = await enumerator!.MoveNextAsync();
                    if (hasNext) evt = enumerator.Current;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[AiChatService] Stream cancelled by client");
                    loopCancelled = true;
                    wasCancelled = true;
                    hasNext = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AiChatService] Anthropic stream error");
                    loopErrorMessage = "Erro durante streaming.";
                    hasNext = false;
                }

                if (loopCancelled) break;
                if (loopErrorMessage is not null)
                {
                    errored = true;
                    yield return new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: loopErrorMessage);
                    break;
                }

                if (!hasNext) break;

                switch (evt)
                {
                    case AnthropicMessageStartEvent start:
                        finalUsage = start.Usage;
                        break;

                    case AnthropicTextDeltaEvent delta:
                        assistantBuffer.Append(delta.Text);
                        yield return new ChatStreamChunkDto(
                            ChatStreamChunkType.ContentDelta,
                            MessageId: assistantMessage.Id,
                            Delta: delta.Text);
                        break;

                    case AnthropicMessageDeltaEvent msgDelta:
                        if (msgDelta.Usage is not null) finalUsage = MergeUsage(finalUsage, msgDelta.Usage);
                        if (!string.IsNullOrEmpty(msgDelta.StopReason)) stopReason = msgDelta.StopReason;
                        break;

                    case AnthropicMessageStopEvent:
                        // Stream completa — sairemos do loop na próxima iteração
                        break;

                    case AnthropicErrorEvent err:
                        _logger.LogWarning("[AiChatService] Anthropic error event: {Type} {Message}", err.ErrorType, err.Message);
                        errored = true;
                        yield return new ChatStreamChunkDto(ChatStreamChunkType.Error, Error: err.Message);
                        break;
                }
            }
        }
        finally
        {
            if (enumerator is not null)
                await enumerator.DisposeAsync();
        }

        // ----- Cancellation guard: remove placeholder para não poluir a conversa -----
        if (wasCancelled)
        {
            // Cliente desconectou antes de receber resposta completa.
            // Remove o placeholder vazio/parcial do assistente — não há dado útil a persistir.
            // Usa CancellationToken.None pois o token original já foi cancelado.
            await _repository.DeleteMessageAsync(assistantMessage, CancellationToken.None);
            await _uow.SaveChangesAsync(CancellationToken.None);
            yield break;
        }

        // ----- Persiste resposta final + usage -----
        // Usamos apenas SaveChangesAsync — EF Core detecta automaticamente as mudanças
        // nas entidades já rastreadas (assistantMessage + conv). NÃO chamamos UpdateAsync(conv)
        // pois DbSet.Update() em grafo com backing-field pode gerar WHERE clause errado
        // levando a DbUpdateConcurrencyException (0 rows affected).
        var assistantText = assistantBuffer.ToString();
        assistantMessage.SetContent(assistantText);

        if (finalUsage is not null)
        {
            var cost = CalculateCost(conv.Model, finalUsage);
            assistantMessage.UpdateUsage(
                finalUsage.InputTokens,
                finalUsage.OutputTokens,
                finalUsage.CacheReadInputTokens,
                finalUsage.CacheCreationInputTokens,
                cost);

            await _uow.SaveChangesAsync(CancellationToken.None);

            yield return new ChatStreamChunkDto(
                ChatStreamChunkType.Usage,
                MessageId: assistantMessage.Id,
                Usage: new UsageDto(
                    finalUsage.InputTokens,
                    finalUsage.OutputTokens,
                    finalUsage.CacheReadInputTokens,
                    finalUsage.CacheCreationInputTokens,
                    cost));
        }
        else
        {
            // Mesmo sem usage, salva o conteúdo
            await _uow.SaveChangesAsync(CancellationToken.None);
        }

        yield return new ChatStreamChunkDto(
            ChatStreamChunkType.MessageStop,
            MessageId: assistantMessage.Id,
            StopReason: stopReason);

        // ----- Gera título via Haiku se a conversa é nova e tem default title -----
        if (!errored && conversationIsNew && conv.Title == DefaultTitle && assistantText.Length > 0)
        {
            string? newTitle = null;
            try
            {
                newTitle = await GenerateTitleAsync(request.Message, assistantText, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AiChatService] Failed to generate conversation title");
            }

            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                conv.SetTitle(newTitle!);
                await _uow.SaveChangesAsync(CancellationToken.None);

                yield return new ChatStreamChunkDto(
                    ChatStreamChunkType.Title,
                    ConversationId: conv.Id,
                    Title: newTitle);
            }
        }
    }

    // ===== MONTHLY USAGE =====

    public async Task<Result<MonthlyUsageDto>> GetMonthlyUsageAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var (input, output, cacheRead, cacheCreation, cost) =
            await _repository.GetMonthlyUsageAsync(userId, monthStart, cancellationToken);

        var dto = new MonthlyUsageDto(
            now.Year,
            now.Month,
            input,
            output,
            cacheRead,
            cacheCreation,
            input + output + cacheRead + cacheCreation,
            cost);

        return Result<MonthlyUsageDto>.Success(dto);
    }

    // ===== HELPERS =====

    private static string BuildUserContent(string message, List<AttachmentDto>? attachments)
    {
        if (attachments is null || attachments.Count == 0)
            return message;

        var sb = new StringBuilder();
        foreach (var att in attachments)
        {
            sb.Append("<attachment filename=\"").Append(att.FileName).Append("\">\n");
            sb.Append(att.Content);
            if (!att.Content.EndsWith("\n")) sb.Append('\n');
            sb.Append("</attachment>\n\n");
        }
        sb.Append(message);
        return sb.ToString();
    }

    public static decimal CalculateCost(string model, AnthropicUsage usage)
    {
        if (!Prices.TryGetValue(model, out var p))
        {
            // Fallback: usa preço do Sonnet 4.5 se modelo desconhecido
            p = Prices["claude-sonnet-4-5"];
        }

        // Preços são por 1M tokens
        const decimal perMillion = 1_000_000m;
        var cost =
            (usage.InputTokens * p.Input / perMillion) +
            (usage.OutputTokens * p.Output / perMillion) +
            (usage.CacheReadInputTokens * p.CacheRead / perMillion) +
            (usage.CacheCreationInputTokens * p.CacheWrite / perMillion);

        return Math.Round(cost, 6);
    }

    private static AnthropicUsage MergeUsage(AnthropicUsage? existing, AnthropicUsage incoming)
    {
        if (existing is null) return incoming;
        return new AnthropicUsage(
            Math.Max(existing.InputTokens, incoming.InputTokens),
            Math.Max(existing.OutputTokens, incoming.OutputTokens),
            Math.Max(existing.CacheReadInputTokens, incoming.CacheReadInputTokens),
            Math.Max(existing.CacheCreationInputTokens, incoming.CacheCreationInputTokens));
    }

    private async Task<string?> GenerateTitleAsync(string userMessage, string assistantReply, CancellationToken ct)
    {
        var prompt = $"""
            Resuma em até 6 palavras o tópico desta conversa.
            Devolva APENAS o título, sem aspas, sem ponto final.

            Usuário disse:
            {Truncate(userMessage, 500)}

            Assistente respondeu:
            {Truncate(assistantReply, 500)}
            """;

        var req = new AnthropicMessageRequest(
            Model: TitleModel,
            Messages: new List<AnthropicMessage> { new("user", prompt) },
            MaxTokens: 32,
            EnablePromptCache: false);

        var result = await _anthropic.CompleteAsync(req, ct);
        var title = result.Text.Trim().Trim('"', '\'').TrimEnd('.');
        if (title.Length > 200) title = title[..200];
        return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";

    private static ConversationDetailDto ToDetail(AiConversation conv)
    {
        var messages = conv.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                m.Id,
                m.Role,
                m.Content,
                m.InputTokens,
                m.OutputTokens,
                m.CacheReadTokens,
                m.CacheCreationTokens,
                m.CostUsd,
                m.CreatedAt,
                m.Attachments
                    .Select(a => new AttachmentMetaDto(a.Id, a.FileName, a.ContentType, a.SizeBytes))
                    .ToList()))
            .ToList();

        return new ConversationDetailDto(
            conv.Id,
            conv.Title,
            conv.Model,
            conv.SystemPrompt,
            conv.CreatedAt,
            conv.UpdatedAt,
            conv.IsArchived,
            messages);
    }
}
