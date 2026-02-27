using Diax.Application.Ads.Dtos;
using Diax.Shared.Results;

namespace Diax.Application.Ads;

public interface IFacebookAdsService
{
    Task<Result<AdAccountResponse>> ConnectAccountAsync(Guid userId, ConnectAdAccountRequest request, CancellationToken ct = default);
    Task<Result<AdAccountResponse>> GetConnectedAccountAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AdAccountResponse>> SyncAccountInfoAsync(Guid userId, CancellationToken ct = default);
    Task<Result> DisconnectAccountAsync(Guid userId, CancellationToken ct = default);

    Task<Result<List<FacebookCampaignDto>>> GetCampaignsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<List<FacebookAdSetDto>>> GetAdSetsAsync(Guid userId, string? campaignId, CancellationToken ct = default);
    Task<Result<List<FacebookAdDto>>> GetAdsAsync(Guid userId, string? adSetId, CancellationToken ct = default);
    Task<Result<List<FacebookInsightDto>>> GetInsightsAsync(Guid userId, InsightsRequest request, CancellationToken ct = default);
    Task<Result<AdAccountSummaryResponse>> GetAccountSummaryAsync(Guid userId, CancellationToken ct = default);
}
