using Diax.Domain.AiChat;
using Diax.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class AiChatRepository : Repository<AiConversation>, IAiChatRepository
{
    public AiChatRepository(DiaxDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<AiConversation>> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.UserId == userId);

        if (!includeArchived)
            query = query.Where(c => !c.IsArchived);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Messages)
            .ToListAsync(cancellationToken);

        return new PagedResult<AiConversation>(items, totalCount, page, pageSize);
    }

    public async Task<AiConversation?> GetWithMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .Include(c => c.Messages)
                .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AiConversation?> GetByIdForUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Id == conversationId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddMessageAsync(AiChatMessage message, CancellationToken cancellationToken = default)
    {
        // Adiciona diretamente ao DbSet para evitar marcar AiConversation como Modified.
        // EF Core relationship fix-up via ConversationId FK popula automaticamente conv._messages.
        await Context.Set<AiChatMessage>().AddAsync(message, cancellationToken);
    }

    public async Task<(int InputTokens, int OutputTokens, int CacheReadTokens, int CacheCreationTokens, decimal CostUsd)>
        GetMonthlyUsageAsync(
            Guid userId,
            DateTime monthStartUtc,
            CancellationToken cancellationToken = default)
    {
        // Join conversa -> mensagens, filtra por dono e mês corrente
        var query =
            from c in DbSet.Where(c => c.UserId == userId)
            from m in c.Messages
            where m.CreatedAt >= monthStartUtc && m.Role == "assistant"
            select m;

        if (!await query.AnyAsync(cancellationToken))
            return (0, 0, 0, 0, 0m);

        var agg = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Input = g.Sum(m => m.InputTokens),
                Output = g.Sum(m => m.OutputTokens),
                CacheRead = g.Sum(m => m.CacheReadTokens),
                CacheCreate = g.Sum(m => m.CacheCreationTokens),
                Cost = g.Sum(m => m.CostUsd)
            })
            .FirstAsync(cancellationToken);

        return (agg.Input, agg.Output, agg.CacheRead, agg.CacheCreate, agg.Cost);
    }
}
