using System.Globalization;
using System.Text.Json;
using Diax.Application.Common;
using Diax.Application.Finance.Patrimonio.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Finance.Assets;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Finance.Patrimonio;

/// <summary>
/// Avaliação automática de veículos pela tabela FIPE (F4). Fonte: API Parallelum v2
/// (gratuita, sem chave). Catálogo (marcas/modelos/anos) cacheado por 24h; preço por 6h —
/// a FIPE só muda uma vez por mês, então o refresh lazy diário quase sempre bate no cache.
/// O vínculo fica no próprio Asset (códigos do drill-down) e cada reconsulta com preço
/// diferente vira uma AssetValuation com Source = Fipe (histórico de depreciação de graça).
/// </summary>
public class FipeService : IApplicationService
{
    private const string BaseUrl = "https://fipe.parallelum.com.br/api/v2";

    private static readonly string[] VehicleTypes = { "cars", "motorcycles", "trucks" };
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan PriceTtl = TimeSpan.FromHours(6);

    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FipeService> _logger;

    public FipeService(
        IAssetRepository assetRepository,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<FipeService> logger)
    {
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    // ===== Catálogo (drill-down marca → modelo → ano) =====

    public Task<Result<IReadOnlyList<FipeItemResponse>>> GetBrandsAsync(
        string vehicleType, CancellationToken cancellationToken = default)
        => GetCatalogAsync(vehicleType, $"{vehicleType}/brands", cancellationToken);

    public Task<Result<IReadOnlyList<FipeItemResponse>>> GetModelsAsync(
        string vehicleType, string brandCode, CancellationToken cancellationToken = default)
        => GetCatalogAsync(vehicleType, $"{vehicleType}/brands/{brandCode}/models", cancellationToken,
            brandCode);

    public Task<Result<IReadOnlyList<FipeItemResponse>>> GetYearsAsync(
        string vehicleType, string brandCode, string modelCode, CancellationToken cancellationToken = default)
        => GetCatalogAsync(vehicleType, $"{vehicleType}/brands/{brandCode}/models/{modelCode}/years",
            cancellationToken, brandCode, modelCode);

    public async Task<Result<FipePriceResponse>> GetPriceAsync(
        string vehicleType, string brandCode, string modelCode, string yearCode,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(vehicleType, brandCode, modelCode, yearCode);
        if (validation is not null)
            return Result.Failure<FipePriceResponse>(validation);

        var path = $"{vehicleType}/brands/{brandCode}/models/{modelCode}/years/{yearCode}";
        var cacheKey = $"patrimonio:fipe:price:{path}";
        if (_cache.TryGetValue(cacheKey, out FipePriceResponse? cached) && cached is not null)
            return Result.Success(cached);

        try
        {
            var json = await CreateClient().GetStringAsync($"{BaseUrl}/{path}", cancellationToken);
            var price = ParsePriceJson(json);
            if (price is null)
                return Result.Failure<FipePriceResponse>(
                    new Error("Fipe.ParseFailed", "FIPE returned an unreadable price payload."));

            _cache.Set(cacheKey, price, PriceTtl);
            return Result.Success(price);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FIPE price lookup failed for {Path}", path);
            return Result.Failure<FipePriceResponse>(
                new Error("Fipe.Unavailable", "FIPE service is unavailable right now."));
        }
    }

    // ===== Vínculo + refresh =====

    /// <summary>
    /// Vincula um ativo Veiculo aos códigos FIPE e já registra a primeira avaliação automática.
    /// </summary>
    public async Task<Result<LinkFipeResponse>> LinkAsync(
        Guid assetId, LinkFipeRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var asset = await _assetRepository.GetByIdAndUserAsync(assetId, userId, cancellationToken);
        if (asset is null)
            return Result.Failure<LinkFipeResponse>(new Error("Asset.NotFound", "Asset not found"));

        var priceResult = await GetPriceAsync(
            request.VehicleType, request.BrandCode, request.ModelCode, request.YearCode, cancellationToken);
        if (!priceResult.IsSuccess)
            return Result.Failure<LinkFipeResponse>(priceResult.Error!);

        try
        {
            asset.LinkFipe(request.VehicleType, request.BrandCode, request.ModelCode, request.YearCode);
            var valuation = asset.AddValuation(priceResult.Value.Price, DateTime.UtcNow, AssetValuationSource.Fipe);

            await _assetRepository.AddValuationAsync(valuation, cancellationToken);
            await _assetRepository.UpdateAsync(asset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Linked asset {AssetId} to FIPE {Model} = {Value} ({Reference})",
                assetId, priceResult.Value.Model, priceResult.Value.Price, priceResult.Value.ReferenceMonth);

            return Result<LinkFipeResponse>.Success(new LinkFipeResponse(
                assetId, priceResult.Value.Price, priceResult.Value.ReferenceMonth, priceResult.Value.Model));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<LinkFipeResponse>(new Error("Asset.ValidationFailed", ex.Message));
        }
    }

    /// <summary>
    /// Remove o vínculo FIPE do ativo (o valor volta a ser mantido manualmente).
    /// </summary>
    public async Task<Result> UnlinkAsync(Guid assetId, Guid userId, CancellationToken cancellationToken = default)
    {
        var asset = await _assetRepository.GetByIdAndUserAsync(assetId, userId, cancellationToken);
        if (asset is null)
            return Result.Failure(new Error("Asset.NotFound", "Asset not found"));

        asset.UnlinkFipe();
        await _assetRepository.UpdateAsync(asset, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Reavalia todos os veículos vinculados do usuário (chamado lazy pelo frontend na 1ª visita).
    /// Só grava valuation quando o preço FIPE mudou — na prática, uma vez por mês por veículo.
    /// Best-effort: falha de um veículo não derruba os demais.
    /// </summary>
    public async Task<Result<FipeRefreshResponse>> RefreshAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var vehicles = await _assetRepository.GetAllByUserIdAsync(
            userId, AssetClass.Veiculo, null, null, cancellationToken);
        var linked = vehicles.Where(a => a.HasFipeLink).ToList();

        var updated = 0;
        foreach (var asset in linked)
        {
            var priceResult = await GetPriceAsync(
                asset.FipeVehicleType!, asset.FipeBrandCode!, asset.FipeModelCode!, asset.FipeYearCode!,
                cancellationToken);
            if (!priceResult.IsSuccess)
            {
                _logger.LogWarning("FIPE refresh skipped asset {AssetId}: {Code}",
                    asset.Id, priceResult.Error?.Code);
                continue;
            }

            if (priceResult.Value.Price == asset.CurrentValue)
                continue;

            var valuation = asset.AddValuation(priceResult.Value.Price, DateTime.UtcNow, AssetValuationSource.Fipe);
            await _assetRepository.AddValuationAsync(valuation, cancellationToken);
            await _assetRepository.UpdateAsync(asset, cancellationToken);
            updated++;
        }

        if (updated > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<FipeRefreshResponse>.Success(new FipeRefreshResponse(linked.Count, updated));
    }

    // ===== Internals =====

    private async Task<Result<IReadOnlyList<FipeItemResponse>>> GetCatalogAsync(
        string vehicleType, string path, CancellationToken cancellationToken, params string[] codes)
    {
        var validation = Validate(vehicleType, codes);
        if (validation is not null)
            return Result.Failure<IReadOnlyList<FipeItemResponse>>(validation);

        var cacheKey = $"patrimonio:fipe:catalog:{path}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<FipeItemResponse>? cached) && cached is not null)
            return Result.Success(cached);

        try
        {
            var json = await CreateClient().GetStringAsync($"{BaseUrl}/{path}", cancellationToken);
            var items = ParseCatalog(json);

            _cache.Set(cacheKey, items, CatalogTtl);
            return Result.Success(items);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FIPE catalog lookup failed for {Path}", path);
            return Result.Failure<IReadOnlyList<FipeItemResponse>>(
                new Error("Fipe.Unavailable", "FIPE service is unavailable right now."));
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        return client;
    }

    /// <summary>
    /// Valida tipo de veículo (whitelist) e códigos (dígitos e hífen, ex: "2023-5") —
    /// os códigos entram na URL da FIPE, então nada além disso passa.
    /// </summary>
    private static Error? Validate(string vehicleType, params string[] codes)
    {
        if (!VehicleTypes.Contains(vehicleType, StringComparer.OrdinalIgnoreCase))
            return new Error("Fipe.InvalidVehicleType",
                "Vehicle type must be one of: cars, motorcycles, trucks.");

        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 20 ||
                !code.All(c => char.IsAsciiDigit(c) || c == '-'))
                return new Error("Fipe.InvalidCode", "FIPE codes must contain only digits and dashes.");
        }

        return null;
    }

    /// <summary>Catálogo Parallelum: [{"code":"56","name":"Toyota"}, ...]. Item ilegível é ignorado.</summary>
    internal static IReadOnlyList<FipeItemResponse> ParseCatalog(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<FipeItemResponse>();

        var items = new List<FipeItemResponse>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var code = ReadString(el, "code");
            var name = ReadString(el, "name");
            if (code is not null && name is not null)
                items.Add(new FipeItemResponse(code, name));
        }

        return items;
    }

    /// <summary>
    /// Preço Parallelum: {"price":"R$ 135.836,00","brand":"Toyota","model":"...","modelYear":2023,
    /// "fuel":"Flex","codeFipe":"002203-9","referenceMonth":"agosto de 2026"}. Null se o preço não parsear.
    /// </summary>
    internal static FipePriceResponse? ParsePriceJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var price = ParsePrice(ReadString(root, "price"));
        if (price is null)
            return null;

        var modelYear = root.TryGetProperty("modelYear", out var yearEl) && yearEl.TryGetInt32(out var year)
            ? year
            : 0;

        return new FipePriceResponse(
            Price: price.Value,
            Brand: ReadString(root, "brand") ?? string.Empty,
            Model: ReadString(root, "model") ?? string.Empty,
            ModelYear: modelYear,
            Fuel: ReadString(root, "fuel"),
            CodeFipe: ReadString(root, "codeFipe"),
            ReferenceMonth: ReadString(root, "referenceMonth"));
    }

    /// <summary>Converte "R$ 135.836,00" (pt-BR) em decimal. Null se ilegível ou não-positivo.</summary>
    internal static decimal? ParsePrice(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = raw.Replace("R$", string.Empty).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Currency, new CultureInfo("pt-BR"), out var value) && value > 0
            ? value
            : null;
    }

    private static string? ReadString(JsonElement el, string property)
        => el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
