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
