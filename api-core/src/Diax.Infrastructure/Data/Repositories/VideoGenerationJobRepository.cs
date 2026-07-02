using Diax.Domain.AI;
using Microsoft.EntityFrameworkCore;

namespace Diax.Infrastructure.Data.Repositories;

public class VideoGenerationJobRepository : Repository<VideoGenerationJob>, IVideoGenerationJobRepository
{
    public VideoGenerationJobRepository(DiaxDbContext context) : base(context) { }

    public async Task<VideoGenerationJob?> GetNextQueuedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.Status == VideoGenerationJobStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<VideoGenerationJob>> GetByUserIdAsync(
        Guid userId, int take, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountQueuedAheadAsync(DateTime createdAt, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(x => x.Status == VideoGenerationJobStatus.Queued && x.CreatedAt < createdAt, cancellationToken);
    }

    public async Task<List<VideoGenerationJob>> GetStaleProcessingAsync(
        TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        return await DbSet
            .Where(x => x.Status == VideoGenerationJobStatus.Processing
                        && x.StartedAt != null
                        && x.StartedAt < cutoff)
            .ToListAsync(cancellationToken);
    }
}
