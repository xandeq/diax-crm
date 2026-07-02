using Diax.Domain.Common;

namespace Diax.Domain.AI;

public interface IVideoGenerationJobRepository : IRepository<VideoGenerationJob>
{
    /// <summary>Job mais antigo com status Queued (FIFO), ou null se a fila está vazia.</summary>
    Task<VideoGenerationJob?> GetNextQueuedAsync(CancellationToken cancellationToken = default);

    Task<List<VideoGenerationJob>> GetByUserIdAsync(Guid userId, int take, CancellationToken cancellationToken = default);

    /// <summary>Quantos jobs Queued foram criados antes deste (posição na fila).</summary>
    Task<int> CountQueuedAheadAsync(DateTime createdAt, CancellationToken cancellationToken = default);

    /// <summary>Jobs presos em Processing (ex.: app reiniciou no meio) mais antigos que o limite.</summary>
    Task<List<VideoGenerationJob>> GetStaleProcessingAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
