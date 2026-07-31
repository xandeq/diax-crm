using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

public class AssetService : IApplicationService
{
    private readonly IAssetRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AssetService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<AssetResponse>>> GetAllAsync(
        Guid userId,
        AssetClass? assetClass = null,
        AssetOwnership? ownership = null,
        AssetLiquidity? liquidity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching assets for user {UserId}", userId);
            var assets = await _repository.GetAllByUserIdAsync(userId, assetClass, ownership, liquidity, cancellationToken);
            var response = assets.Select(a => MapToResponse(a, includeValuations: false));
            _logger.LogInformation("Successfully retrieved {Count} assets for user {UserId}", assets.Count, userId);
            return Result<IEnumerable<AssetResponse>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve assets for user {UserId}", userId);
            return Result.Failure<IEnumerable<AssetResponse>>(
                new Error("Asset.QueryFailed", "Failed to retrieve assets. Please check server logs for details."));
        }
    }

    public async Task<Result<AssetResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching asset {AssetId} for user {UserId}", id, userId);
            var asset = await _repository.GetByIdAndUserWithValuationsAsync(id, userId, cancellationToken);
            if (asset == null)
            {
                _logger.LogWarning("Asset {AssetId} not found for user {UserId}", id, userId);
                return Result.Failure<AssetResponse>(new Error("Asset.NotFound", "Asset not found"));
            }

            return Result<AssetResponse>.Success(MapToResponse(asset, includeValuations: true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve asset {AssetId} for user {UserId}", id, userId);
            return Result.Failure<AssetResponse>(
                new Error("Asset.QueryFailed", "Failed to retrieve asset. Please check server logs for details."));
        }
    }

    public async Task<Result<AssetResponse>> CreateAsync(CreateAssetRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating asset {AssetName} for user {UserId}", request.Name, userId);
            var asset = Asset.Create(
                userId,
                request.Name,
                request.Class,
                request.Ownership,
                request.Liquidity,
                request.CurrentValue,
                request.ValuationSource,
                request.Currency,
                request.CostBasis,
                request.AcquiredAt,
                request.Notes);

            await _repository.AddAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created asset {AssetId} for user {UserId}", asset.Id, userId);
            return Result<AssetResponse>.Success(MapToResponse(asset, includeValuations: false));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid asset data: {AssetName} for user {UserId}", request.Name, userId);
            return Result.Failure<AssetResponse>(new Error("Asset.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create asset {AssetName} for user {UserId}", request.Name, userId);
            return Result.Failure<AssetResponse>(
                new Error("Asset.CreateFailed", "Failed to create asset. Please check server logs for details."));
        }
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateAssetRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating asset {AssetId} for user {UserId}", id, userId);
            var asset = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (asset == null)
            {
                _logger.LogWarning("Asset {AssetId} not found for update for user {UserId}", id, userId);
                return Result.Failure(new Error("Asset.NotFound", "Asset not found"));
            }

            asset.UpdateDetails(
                request.Name,
                request.Class,
                request.Ownership,
                request.Liquidity,
                request.CurrentValue,
                request.ValuationSource,
                request.Currency,
                request.CostBasis,
                request.AcquiredAt,
                request.Notes);

            await _repository.UpdateAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated asset {AssetId} for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid update data for asset {AssetId} for user {UserId}", id, userId);
            return Result.Failure(new Error("Asset.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update asset {AssetId} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("Asset.UpdateFailed", "Failed to update asset. Please check server logs for details."));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting asset {AssetId} for user {UserId}", id, userId);
            var asset = await _repository.GetByIdAndUserAsync(id, userId, cancellationToken);
            if (asset == null)
            {
                _logger.LogWarning("Asset {AssetId} not found for deletion for user {UserId}", id, userId);
                return Result.Failure(new Error("Asset.NotFound", "Asset not found"));
            }

            await _repository.DeleteAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted asset {AssetId} for user {UserId}", id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete asset {AssetId} for user {UserId}", id, userId);
            return Result.Failure(
                new Error("Asset.DeleteFailed", "Failed to delete asset. Please check server logs for details."));
        }
    }

    /// <summary>
    /// Registra uma avaliação no histórico do ativo.
    /// A avaliação também atualiza o CurrentValue do ativo (última avaliação = valor vigente).
    /// </summary>
    public async Task<Result<AssetValuationResponse>> AddValuationAsync(
        Guid assetId,
        AddValuationRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding valuation to asset {AssetId} for user {UserId}", assetId, userId);
            var asset = await _repository.GetByIdAndUserAsync(assetId, userId, cancellationToken);
            if (asset == null)
            {
                _logger.LogWarning("Asset {AssetId} not found for valuation for user {UserId}", assetId, userId);
                return Result.Failure<AssetValuationResponse>(new Error("Asset.NotFound", "Asset not found"));
            }

            var valuation = asset.AddValuation(
                request.Value,
                request.AsOf ?? DateTime.UtcNow,
                request.Source ?? asset.ValuationSource,
                request.Currency);

            await _repository.AddValuationAsync(valuation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added valuation {ValuationId} to asset {AssetId} for user {UserId}",
                valuation.Id, assetId, userId);
            return Result<AssetValuationResponse>.Success(MapToValuationResponse(valuation));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid valuation data for asset {AssetId} for user {UserId}", assetId, userId);
            return Result.Failure<AssetValuationResponse>(new Error("Asset.ValidationFailed", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add valuation to asset {AssetId} for user {UserId}", assetId, userId);
            return Result.Failure<AssetValuationResponse>(
                new Error("Asset.AddValuationFailed", "Failed to add valuation. Please check server logs for details."));
        }
    }

    private static AssetResponse MapToResponse(Asset asset, bool includeValuations)
    {
        return new AssetResponse(
            asset.Id,
            asset.Name,
            asset.Class,
            asset.Ownership,
            asset.Liquidity,
            asset.Currency,
            asset.CurrentValue,
            asset.CostBasis,
            asset.AcquiredAt,
            asset.ValuationSource,
            asset.Notes,
            asset.CreatedAt,
            asset.UpdatedAt,
            includeValuations
                ? asset.Valuations
                    .OrderByDescending(v => v.AsOf)
                    .Select(MapToValuationResponse)
                    .ToList()
                : null
        );
    }

    private static AssetValuationResponse MapToValuationResponse(AssetValuation valuation)
    {
        return new AssetValuationResponse(
            valuation.Id,
            valuation.AssetId,
            valuation.Value,
            valuation.Currency,
            valuation.AsOf,
            valuation.Source,
            valuation.CreatedAt
        );
    }
}
