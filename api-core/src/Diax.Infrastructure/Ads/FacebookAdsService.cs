using Diax.Application.Ads;
using Diax.Application.Ads.Dtos;
using Diax.Domain.Ads;
using Diax.Domain.Ads.Repositories;
using Diax.Domain.Common;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Ads;

public class FacebookAdsService : IFacebookAdsService
{
    private readonly IFacebookAdAccountRepository _repository;
    private readonly FacebookGraphApiClient _apiClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FacebookAdsService> _logger;

    public FacebookAdsService(
        IFacebookAdAccountRepository repository,
        FacebookGraphApiClient apiClient,
        IUnitOfWork unitOfWork,
        ILogger<FacebookAdsService> logger)
    {
        _repository = repository;
        _apiClient = apiClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AdAccountResponse>> ConnectAccountAsync(
        Guid userId, ConnectAdAccountRequest request, CancellationToken ct = default)
    {
        var adAccountId = request.AdAccountId.StartsWith("act_")
            ? request.AdAccountId
            : $"act_{request.AdAccountId}";

        // Verify the token and get account info from Facebook
        var info = await _apiClient.GetAccountInfoAsync(adAccountId, request.AccessToken, ct);
        if (info == null)
            return Result.Failure<AdAccountResponse>(Error.Validation("Ads.InvalidToken",
                "Não foi possível verificar o token de acesso. Verifique o Ad Account ID e o token."));

        // Check if user already has a connected account
        var existing = await _repository.GetByUserIdAsync(userId, ct);
        FacebookAdAccount account;

        if (existing != null)
        {
            existing.UpdateToken(request.AccessToken);
            existing.UpdateAccountInfo(
                info.Name ?? existing.AccountName,
                info.Currency ?? existing.Currency,
                info.TimezoneName ?? existing.Timezone,
                MapAccountStatus(info.AccountStatus));
            _repository.Update(existing);
            account = existing;
        }
        else
        {
            account = FacebookAdAccount.Create(
                userId,
                adAccountId,
                request.AccessToken,
                info.Name ?? adAccountId,
                info.Currency ?? "",
                info.TimezoneName ?? "",
                MapAccountStatus(info.AccountStatus));

            await _repository.AddAsync(account, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} connected Facebook Ad Account {AdAccountId}", userId, adAccountId);

        return Result.Success(MapToResponse(account));
    }

    public async Task<Result<AdAccountResponse>> GetConnectedAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await _repository.GetByUserIdAsync(userId, ct);
        if (account == null)
            return Result.Failure<AdAccountResponse>(Error.NotFound("FacebookAdAccount", userId));

        return Result.Success(MapToResponse(account));
    }

    public async Task<Result<AdAccountResponse>> SyncAccountInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await _repository.GetByUserIdAsync(userId, ct);
        if (account == null)
            return Result.Failure<AdAccountResponse>(Error.NotFound("FacebookAdAccount", userId));

        var info = await _apiClient.GetAccountInfoAsync(account.AdAccountId, account.AccessToken, ct);
        if (info == null)
            return Result.Failure<AdAccountResponse>(Error.Validation("Ads.SyncFailed",
                "Falha ao sincronizar com o Facebook. Verifique se o token ainda é válido."));

        account.UpdateAccountInfo(
            info.Name ?? account.AccountName,
            info.Currency ?? account.Currency,
            info.TimezoneName ?? account.Timezone,
            MapAccountStatus(info.AccountStatus));

        _repository.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(MapToResponse(account));
    }

    public async Task<Result> DisconnectAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var account = await _repository.GetByUserIdAsync(userId, ct);
        if (account == null)
            return Result.Failure(Error.NotFound("FacebookAdAccount", userId));

        _repository.Remove(account);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} disconnected Facebook Ad Account", userId);

        return Result.Success();
    }

    public async Task<Result<List<FacebookCampaignDto>>> GetCampaignsAsync(Guid userId, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<List<FacebookCampaignDto>>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);
        var campaigns = await _apiClient.GetCampaignsAsync(account!.AdAccountId, account.AccessToken, ct);
        return Result.Success(campaigns);
    }

    public async Task<Result<List<FacebookAdSetDto>>> GetAdSetsAsync(Guid userId, string? campaignId, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<List<FacebookAdSetDto>>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);
        var adSets = await _apiClient.GetAdSetsAsync(account!.AdAccountId, account.AccessToken, campaignId, ct);
        return Result.Success(adSets);
    }

    public async Task<Result<List<FacebookAdDto>>> GetAdsAsync(Guid userId, string? adSetId, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<List<FacebookAdDto>>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);
        var ads = await _apiClient.GetAdsAsync(account!.AdAccountId, account.AccessToken, adSetId, ct);
        return Result.Success(ads);
    }

    public async Task<Result<List<FacebookInsightDto>>> GetInsightsAsync(
        Guid userId, InsightsRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<List<FacebookInsightDto>>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);
        var insights = await _apiClient.GetInsightsAsync(account!.AdAccountId, account.AccessToken, request, ct);
        return Result.Success(insights);
    }

    public async Task<Result<AdAccountSummaryResponse>> GetAccountSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdAccountSummaryResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        var campaigns = await _apiClient.GetCampaignsAsync(account!.AdAccountId, account.AccessToken, ct);
        var insights = await _apiClient.GetInsightsAsync(
            account.AdAccountId,
            account.AccessToken,
            new InsightsRequest("last_30d", Level: "campaign"),
            ct);

        var totalSpend = insights.Sum(i => decimal.TryParse(i.Spend, out var s) ? s : 0);
        var totalImpressions = insights.Sum(i => decimal.TryParse(i.Impressions, out var imp) ? imp : 0);
        var totalClicks = insights.Sum(i => decimal.TryParse(i.Clicks, out var c) ? c : 0);
        var avgCtr = totalImpressions > 0 ? (double)(totalClicks / totalImpressions) * 100 : 0;

        return Result.Success(new AdAccountSummaryResponse(
            accountResult.Value,
            campaigns.Count,
            campaigns.Count(c => c.Status == "ACTIVE"),
            totalSpend,
            totalImpressions,
            totalClicks,
            Math.Round(avgCtr, 2)));
    }

    // ===== CAMPAIGNS - WRITE =====

    public async Task<Result<CampaignWriteResponse>> CreateCampaignAsync(
        Guid userId, CreateCampaignRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<CampaignWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.NameRequired", "O nome da campanha é obrigatório."));

        if (request.DailyBudget != null && request.LifetimeBudget != null)
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.BudgetConflict", "Defina apenas um tipo de orçamento."));

        if (request.LifetimeBudget != null && request.StopTime == null)
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.StopTimeRequired", "Orçamento vitalício requer data de término."));

        if (request.Status != "ACTIVE" && request.Status != "PAUSED")
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.InvalidStatus", "Status deve ser ACTIVE ou PAUSED."));

        var campaignId = await _apiClient.CreateCampaignAsync(
            account!.AdAccountId, account.AccessToken, request, ct);

        if (string.IsNullOrEmpty(campaignId))
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.CreateFailed",
                    "Falha ao criar campanha no Facebook. Verifique os parâmetros e tente novamente."));

        _logger.LogInformation("User {UserId} created campaign {CampaignId}", userId, campaignId);
        return Result.Success(new CampaignWriteResponse(campaignId, request.Status, request.Name));
    }

    public async Task<Result<CampaignWriteResponse>> UpdateCampaignStatusAsync(
        Guid userId, string campaignId, UpdateCampaignStatusRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<CampaignWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (request.Status != "ACTIVE" && request.Status != "PAUSED")
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.InvalidStatus", "Status deve ser ACTIVE ou PAUSED."));

        var success = await _apiClient.UpdateCampaignStatusAsync(
            campaignId, account!.AccessToken, request.Status, ct);

        if (!success)
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.UpdateFailed",
                    "Falha ao atualizar status da campanha. Verifique se a campanha existe."));

        _logger.LogInformation("User {UserId} updated campaign {CampaignId} status to {Status}", userId, campaignId, request.Status);
        return Result.Success(new CampaignWriteResponse(campaignId, request.Status, ""));
    }

    public async Task<Result<CampaignWriteResponse>> UpdateCampaignBudgetAsync(
        Guid userId, string campaignId, UpdateCampaignBudgetRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<CampaignWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (request.DailyBudget != null && request.LifetimeBudget != null)
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.BudgetConflict", "Defina apenas um tipo de orçamento."));

        var success = await _apiClient.UpdateCampaignBudgetAsync(
            campaignId, account!.AccessToken, request, ct);

        if (!success)
            return Result.Failure<CampaignWriteResponse>(
                Error.Validation("Ads.Campaign.BudgetUpdateFailed",
                    "Falha ao atualizar orçamento da campanha. Verifique os valores."));

        _logger.LogInformation("User {UserId} updated campaign {CampaignId} budget", userId, campaignId);
        return Result.Success(new CampaignWriteResponse(campaignId, "", ""));
    }

    // ===== AD SETS - WRITE =====

    public async Task<Result<AdSetWriteResponse>> CreateAdSetAsync(
        Guid userId, CreateAdSetRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdSetWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.NameRequired", "O nome do ad set é obrigatório."));

        if (request.DailyBudget != null && request.LifetimeBudget != null)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.BudgetConflict", "Defina apenas um tipo de orçamento."));

        if (request.LifetimeBudget != null && request.EndTime == null)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.EndTimeRequired", "Orçamento vitalício requer data de término."));

        if ((request.BidStrategy == "COST_CAP" || request.BidStrategy == "BID_CAP") && string.IsNullOrEmpty(request.BidAmount))
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.BidAmountRequired", "Valor do lance é obrigatório para esta estratégia."));

        if (request.Targeting?.Countries?.Count == 0 && request.Targeting?.Regions?.Count == 0)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.TargetingRequired", "Selecione pelo menos um país ou região."));

        var adSetId = await _apiClient.CreateAdSetAsync(
            account!.AdAccountId, account.AccessToken, request, ct);

        if (string.IsNullOrEmpty(adSetId))
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.CreateFailed",
                    "Falha ao criar ad set no Facebook. Verifique os parâmetros e tente novamente."));

        _logger.LogInformation("User {UserId} created ad set {AdSetId}", userId, adSetId);
        return Result.Success(new AdSetWriteResponse(adSetId, request.Status, request.Name));
    }

    public async Task<Result<AdSetWriteResponse>> UpdateAdSetStatusAsync(
        Guid userId, string adSetId, UpdateAdSetStatusRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdSetWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (request.Status != "ACTIVE" && request.Status != "PAUSED")
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.InvalidStatus", "Status deve ser ACTIVE ou PAUSED."));

        var success = await _apiClient.UpdateAdSetStatusAsync(
            adSetId, account!.AccessToken, request.Status, ct);

        if (!success)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.UpdateFailed",
                    "Falha ao atualizar status do ad set. Verifique se o ad set existe."));

        _logger.LogInformation("User {UserId} updated ad set {AdSetId} status to {Status}", userId, adSetId, request.Status);
        return Result.Success(new AdSetWriteResponse(adSetId, request.Status, ""));
    }

    public async Task<Result<AdSetWriteResponse>> UpdateAdSetBudgetAsync(
        Guid userId, string adSetId, UpdateAdSetBudgetRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdSetWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (request.DailyBudget != null && request.LifetimeBudget != null)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.BudgetConflict", "Defina apenas um tipo de orçamento."));

        if ((request.BidStrategy == "COST_CAP" || request.BidStrategy == "BID_CAP") && string.IsNullOrEmpty(request.BidAmount))
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.BidAmountRequired", "Valor do lance é obrigatório para esta estratégia."));

        var success = await _apiClient.UpdateAdSetBudgetAsync(
            adSetId, account!.AccessToken, request, ct);

        if (!success)
            return Result.Failure<AdSetWriteResponse>(
                Error.Validation("Ads.AdSet.BudgetUpdateFailed",
                    "Falha ao atualizar orçamento do ad set. Verifique os valores."));

        _logger.LogInformation("User {UserId} updated ad set {AdSetId} budget", userId, adSetId);
        return Result.Success(new AdSetWriteResponse(adSetId, "", ""));
    }

    // ===== ADS - WRITE =====

    public async Task<Result<AdWriteResponse>> CreateAdAsync(
        Guid userId, CreateAdRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.NameRequired", "O nome do anúncio é obrigatório."));

        if (string.IsNullOrWhiteSpace(request.CreativeId))
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.CreativeRequired", "Selecione um criativo para o anúncio."));

        if (request.Status != "ACTIVE" && request.Status != "PAUSED")
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.InvalidStatus", "Status deve ser ACTIVE ou PAUSED."));

        var adId = await _apiClient.CreateAdAsync(
            request.AdSetId, account!.AccessToken, request, ct);

        if (string.IsNullOrEmpty(adId))
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.CreateFailed",
                    "Falha ao criar anúncio no Facebook. Verifique os parâmetros e tente novamente."));

        _logger.LogInformation("User {UserId} created ad {AdId}", userId, adId);
        return Result.Success(new AdWriteResponse(adId, request.Status, request.Name));
    }

    public async Task<Result<AdWriteResponse>> UpdateAdStatusAsync(
        Guid userId, string adId, UpdateAdStatusRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdWriteResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (request.Status != "ACTIVE" && request.Status != "PAUSED")
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.InvalidStatus", "Status deve ser ACTIVE ou PAUSED."));

        var success = await _apiClient.UpdateAdStatusAsync(
            adId, account!.AccessToken, request.Status, ct);

        if (!success)
            return Result.Failure<AdWriteResponse>(
                Error.Validation("Ads.Ad.UpdateFailed",
                    "Falha ao atualizar status do anúncio. Verifique se o anúncio existe."));

        _logger.LogInformation("User {UserId} updated ad {AdId} status to {Status}", userId, adId, request.Status);
        return Result.Success(new AdWriteResponse(adId, request.Status, ""));
    }

    // ===== AD CREATIVES =====

    public async Task<Result<List<AdCreativeResponse>>> GetCreativesAsync(Guid userId, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<List<AdCreativeResponse>>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);
        var creatives = await _apiClient.GetCreativesAsync(account!.AdAccountId, account.AccessToken, ct);
        return Result.Success(creatives);
    }

    public async Task<Result<AdCreativeResponse>> CreateCreativeAsync(
        Guid userId, CreateAdCreativeRequest request, CancellationToken ct = default)
    {
        var accountResult = await GetConnectedAccountAsync(userId, ct);
        if (accountResult.IsFailure) return Result.Failure<AdCreativeResponse>(accountResult.Error);

        var account = await _repository.GetByUserIdAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<AdCreativeResponse>(
                Error.Validation("Ads.Creative.NameRequired", "O nome do criativo é obrigatório."));

        if (request.ObjectType != "IMAGE" && request.ObjectType != "VIDEO" && request.ObjectType != "CAROUSEL")
            return Result.Failure<AdCreativeResponse>(
                Error.Validation("Ads.Creative.InvalidType", "Tipo de criativo deve ser IMAGE, VIDEO ou CAROUSEL."));

        if (request.ObjectType == "IMAGE" && string.IsNullOrEmpty(request.ImageHash))
            return Result.Failure<AdCreativeResponse>(
                Error.Validation("Ads.Creative.ImageHashRequired", "Hash da imagem é obrigatório para criativos de imagem."));

        if (request.ObjectType == "VIDEO" && string.IsNullOrEmpty(request.VideoId))
            return Result.Failure<AdCreativeResponse>(
                Error.Validation("Ads.Creative.VideoIdRequired", "ID do vídeo é obrigatório para criativos de vídeo."));

        var creativeId = await _apiClient.CreateCreativeAsync(
            account!.AdAccountId, account.AccessToken, request, ct);

        if (string.IsNullOrEmpty(creativeId))
            return Result.Failure<AdCreativeResponse>(
                Error.Validation("Ads.Creative.CreateFailed",
                    "Falha ao criar criativo no Facebook. Verifique os parâmetros e tente novamente."));

        _logger.LogInformation("User {UserId} created creative {CreativeId}", userId, creativeId);
        return Result.Success(new AdCreativeResponse(
            creativeId, request.Name, request.ObjectType, "ACTIVE", null,
            request.Body, request.Title, request.CallToActionType, request.LinkUrl, DateTime.UtcNow));
    }

    private static AdAccountResponse MapToResponse(FacebookAdAccount account) => new(
        account.Id,
        account.AdAccountId,
        account.AccountName,
        account.Currency,
        account.Timezone,
        account.AccountStatus,
        account.IsActive,
        account.LastSyncAt,
        account.CreatedAt);

    private static string MapAccountStatus(int status) => status switch
    {
        1 => "ACTIVE",
        2 => "DISABLED",
        3 => "UNSETTLED",
        7 => "PENDING_RISK_REVIEW",
        8 => "PENDING_SETTLEMENT",
        9 => "IN_GRACE_PERIOD",
        100 => "PENDING_CLOSURE",
        101 => "CLOSED",
        201 => "ANY_ACTIVE",
        202 => "ANY_CLOSED",
        _ => "UNKNOWN"
    };
}
