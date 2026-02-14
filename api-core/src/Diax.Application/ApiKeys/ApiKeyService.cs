using Diax.Application.ApiKeys.Dtos;
using Diax.Application.Common;
using Diax.Domain.ApiKeys;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.ApiKeys;

/// <summary>
/// Serviço de aplicação para gerenciamento de API Keys.
/// </summary>
public class ApiKeyService : IApplicationService
{
    private readonly IApiKeyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        IApiKeyRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ApiKeyService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Cria uma nova API Key.
    /// </summary>
    public async Task<Result<CreateApiKeyResponse>> CreateAsync(
        CreateApiKeyRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Criando nova API Key: {Name}", request.Name);

            var (apiKey, plainKey) = ApiKey.Create(
                request.Name,
                userId,
                request.ValidityDays,
                request.Scope);

            await _repository.AddAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("API Key criada com sucesso: {Id}", apiKey.Id);

            return Result.Success(CreateApiKeyResponse.FromEntityWithKey(apiKey, plainKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar API Key: {Name}", request.Name);
            return Result.Failure<CreateApiKeyResponse>(
                new Error("ApiKey.CreateFailed", "Falha ao criar API Key."));
        }
    }

    /// <summary>
    /// Obtém todas as API Keys (sem expor as chaves plaintext).
    /// </summary>
    public async Task<Result<IEnumerable<ApiKeyResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKeys = await _repository.GetAllAsync(cancellationToken);
            var responses = apiKeys.Select(ApiKeyResponse.FromEntity);

            return Result.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter API Keys");
            return Result.Failure<IEnumerable<ApiKeyResponse>>(
                new Error("ApiKey.GetAllFailed", "Falha ao obter API Keys."));
        }
    }

    /// <summary>
    /// Obtém uma API Key por ID.
    /// </summary>
    public async Task<Result<ApiKeyResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _repository.GetByIdAsync(id, cancellationToken);
            if (apiKey == null)
            {
                return Result.Failure<ApiKeyResponse>(
                    Error.NotFound("ApiKey", id));
            }

            return Result.Success(ApiKeyResponse.FromEntity(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter API Key: {Id}", id);
            return Result.Failure<ApiKeyResponse>(
                new Error("ApiKey.GetByIdFailed", "Falha ao obter API Key."));
        }
    }

    /// <summary>
    /// Desabilita uma API Key.
    /// </summary>
    public async Task<Result<ApiKeyResponse>> DisableAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _repository.GetByIdAsync(id, cancellationToken);
            if (apiKey == null)
            {
                return Result.Failure<ApiKeyResponse>(
                    Error.NotFound("ApiKey", id));
            }

            apiKey.Disable();
            await _repository.UpdateAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("API Key desabilitada: {Id}", id);

            return Result.Success(ApiKeyResponse.FromEntity(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desabilitar API Key: {Id}", id);
            return Result.Failure<ApiKeyResponse>(
                new Error("ApiKey.DisableFailed", "Falha ao desabilitar API Key."));
        }
    }

    /// <summary>
    /// Habilita uma API Key.
    /// </summary>
    public async Task<Result<ApiKeyResponse>> EnableAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _repository.GetByIdAsync(id, cancellationToken);
            if (apiKey == null)
            {
                return Result.Failure<ApiKeyResponse>(
                    Error.NotFound("ApiKey", id));
            }

            apiKey.Enable();
            await _repository.UpdateAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("API Key habilitada: {Id}", id);

            return Result.Success(ApiKeyResponse.FromEntity(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao habilitar API Key: {Id}", id);
            return Result.Failure<ApiKeyResponse>(
                new Error("ApiKey.EnableFailed", "Falha ao habilitar API Key."));
        }
    }

    /// <summary>
    /// Deleta uma API Key permanentemente.
    /// </summary>
    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = await _repository.GetByIdAsync(id, cancellationToken);
            if (apiKey == null)
            {
                return Result.Failure(
                    Error.NotFound("ApiKey", id));
            }

            await _repository.DeleteAsync(apiKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("API Key deletada: {Id}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar API Key: {Id}", id);
            return Result.Failure(
                new Error("ApiKey.DeleteFailed", "Falha ao deletar API Key."));
        }
    }
}
