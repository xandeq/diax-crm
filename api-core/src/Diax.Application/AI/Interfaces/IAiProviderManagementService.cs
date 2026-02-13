using Diax.Application.AI.Dtos;

namespace Diax.Application.AI;

/// <summary>
/// Serviço para gerenciamento (CRUD) de providers e modelos de IA.
/// Permite inserir novos modelos dinamicamente via UI sem alterar código.
/// </summary>
public interface IAiProviderManagementService
{
    /// <summary>
    /// Adiciona um novo provider. Retorna erro se já existir.
    /// </summary>
    Task<(bool Success, string Message, Guid? Id)> AddProviderAsync(AddAiProviderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo modelo a um provider existente. Retorna erro se já existir.
    /// </summary>
    Task<(bool Success, string Message, Guid? Id)> AddModelAsync(AddAiModelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ativa ou desativa um provider.
    /// </summary>
    Task<(bool Success, string Message)> SetProviderActiveAsync(string providerKey, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ativa ou desativa um modelo.
    /// </summary>
    Task<(bool Success, string Message)> SetModelActiveAsync(string providerKey, string modelKey, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os providers e modelos (incluindo inativos). Uso administrativo.
    /// </summary>
    Task<List<AiProviderDto>> GetAllProvidersAsync(CancellationToken cancellationToken = default);
}
