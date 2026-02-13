using Diax.Application.AI.Dtos;
using Diax.Domain.AI;
using Microsoft.Extensions.Logging;

namespace Diax.Application.AI;

/// <summary>
/// Gerenciamento de providers e modelos de IA.
/// Permite inserir/ativar/desativar modelos dinamicamente.
/// Invalida o cache do validator após alterações.
/// </summary>
public class AiProviderManagementService : IAiProviderManagementService
{
    private readonly IAiProviderRepository _providerRepository;
    private readonly IAiModelRepository _modelRepository;
    private readonly IAiModelValidator _validator;
    private readonly ILogger<AiProviderManagementService> _logger;

    public AiProviderManagementService(
        IAiProviderRepository providerRepository,
        IAiModelRepository modelRepository,
        IAiModelValidator validator,
        ILogger<AiProviderManagementService> logger)
    {
        _providerRepository = providerRepository ?? throw new ArgumentNullException(nameof(providerRepository));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AiProviderDto>> GetAllProvidersAsync(CancellationToken cancellationToken = default)
    {
        var allProviders = await _providerRepository.GetAllIncludedAsync(cancellationToken);

        var result = allProviders.Select(p => new AiProviderDto(
            p.Id,
            p.Key,
            p.Name,
            p.IsEnabled,
            p.SupportsListModels,
            p.BaseUrl,
            p.Models.Select(m => new AiModelDto(
                m.Id,
                m.ModelKey,
                m.DisplayName,
                m.IsEnabled,
                m.IsDiscovered,
                m.InputCostHint,
                m.OutputCostHint,
                m.MaxTokensHint
            )).OrderBy(m => m.DisplayName).ToList()
        )).OrderBy(p => p.Name).ToList();

        return result;
    }

    public async Task<(bool Success, string Message, Guid? Id)> AddProviderAsync(
        AddAiProviderRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _providerRepository.GetByKeyAsync(request.Key, cancellationToken);
        if (existing != null)
        {
            _logger.LogWarning("[AiProviderMgmt] Tentativa de inserir provider duplicado: '{Key}'.", request.Key);
            return (false, $"Provider '{request.Key}' já existe.", null);
        }

        var provider = new AiProvider(
            request.Key,
            request.Name,
            request.SupportsListModels,
            request.BaseUrl
        );

        if (request.IsEnabled)
        {
            provider.Enable();
        }

        await _providerRepository.AddAsync(provider, cancellationToken);
        _validator.InvalidateCache();

        _logger.LogInformation("[AiProviderMgmt] Provider '{Name}' criado com ID {Id}.", request.Name, provider.Id);
        return (true, $"Provider '{request.Name}' criado com sucesso.", provider.Id);
    }

    public async Task<(bool Success, string Message, Guid? Id)> AddModelAsync(
        AddAiModelRequest request, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByKeyAsync(request.ProviderKey, cancellationToken);
        if (provider == null)
        {
            _logger.LogWarning("[AiProviderMgmt] Tentativa de inserir modelo para provider inexistente: '{Provider}'.", request.ProviderKey);
            return (false, $"Provider '{request.ProviderKey}' não encontrado.", null);
        }

        var existingModels = await _modelRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
        if (existingModels.Any(m => m.ModelKey.Equals(request.ModelKey, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("[AiProviderMgmt] Tentativa de inserir modelo duplicado: '{ModelKey}' no provider '{Provider}'.",
                request.ModelKey, request.ProviderKey);
            return (false, $"Modelo '{request.ModelKey}' já existe no provider '{request.ProviderKey}'.", null);
        }

        var model = new AiModel(
            provider.Id,
            request.ModelKey,
            request.DisplayName,
            request.IsDiscovered
        );

        model.UpdateDetails(
            request.DisplayName,
            request.InputCostHint,
            request.OutputCostHint,
            request.MaxTokensHint,
            null
        );

        if (request.IsEnabled)
        {
            model.Enable();
        }

        await _modelRepository.AddAsync(model, cancellationToken);
        _validator.InvalidateCache();

        _logger.LogInformation("[AiProviderMgmt] Modelo '{ModelKey}' criado no provider '{Provider}'. ID: {Id}.",
            request.ModelKey, request.ProviderKey, model.Id);
        return (true, $"Modelo '{request.ModelKey}' criado com sucesso.", model.Id);
    }

    public async Task<(bool Success, string Message)> SetProviderActiveAsync(
        string providerKey, bool isActive, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByKeyAsync(providerKey, cancellationToken);
        if (provider == null)
        {
            return (false, $"Provider '{providerKey}' não encontrado.");
        }

        if (isActive)
            provider.Enable();
        else
            provider.Disable();

        await _providerRepository.UpdateAsync(provider, cancellationToken);
        _validator.InvalidateCache();

        var status = isActive ? "ativado" : "desativado";
        _logger.LogInformation("[AiProviderMgmt] Provider '{Key}' {Status}.", providerKey, status);
        return (true, $"Provider '{providerKey}' {status} com sucesso.");
    }

    public async Task<(bool Success, string Message)> SetModelActiveAsync(
        string providerKey, string modelKey, bool isActive, CancellationToken cancellationToken = default)
    {
        var provider = await _providerRepository.GetByKeyAsync(providerKey, cancellationToken);
        if (provider == null)
        {
            return (false, $"Provider '{providerKey}' não encontrado.");
        }

        var models = await _modelRepository.GetByProviderIdAsync(provider.Id, cancellationToken);
        var model = models.FirstOrDefault(m => m.ModelKey.Equals(modelKey, StringComparison.OrdinalIgnoreCase));

        if (model == null)
        {
            return (false, $"Modelo '{modelKey}' não encontrado no provider '{providerKey}'.");
        }

        if (isActive)
            model.Enable();
        else
            model.Disable();

        await _modelRepository.UpdateAsync(model, cancellationToken);
        _validator.InvalidateCache();

        var status = model.IsEnabled ? "ativado" : "desativado";
        _logger.LogInformation("[AiProviderMgmt] Modelo '{ModelKey}' do provider '{Provider}' {Status}.",
            modelKey, providerKey, status);
        return (true, $"Modelo '{modelKey}' {status} com sucesso.");
    }
}
