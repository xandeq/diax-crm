using Diax.Application.AI.VideoGeneration.Dtos;

namespace Diax.Application.AI.VideoGeneration;

/// <summary>
/// Fila assíncrona de geração de vídeo: enfileira → worker processa → frontend consulta.
/// Evita segurar o request HTTP aberto por minutos (timeout do IIS/proxy).
/// </summary>
public interface IVideoJobService
{
    /// <summary>Enfileira um job e retorna o estado inicial (com posição na fila).</summary>
    Task<VideoJobDto> EnqueueAsync(VideoGenerationRequestDto request, Guid userId, CancellationToken ct = default);

    /// <summary>Estado do job. Retorna null se não existe ou não pertence ao usuário.</summary>
    Task<VideoJobDto?> GetAsync(Guid jobId, Guid userId, CancellationToken ct = default);

    Task<List<VideoJobDto>> ListAsync(Guid userId, int take = 20, CancellationToken ct = default);

    /// <summary>Processa o próximo job da fila (chamado pelo worker). Retorna 1 se processou, 0 se fila vazia.</summary>
    Task<int> ProcessNextAsync(CancellationToken ct = default);

    /// <summary>Marca como Failed jobs presos em Processing (ex.: app reiniciou no meio da geração).</summary>
    Task RecoverStaleJobsAsync(CancellationToken ct = default);
}
