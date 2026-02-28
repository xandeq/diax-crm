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

    // ===== CAMPAIGNS - WRITE =====

    Task<Result<CampaignWriteResponse>> CreateCampaignAsync(Guid userId, CreateCampaignRequest request, CancellationToken ct = default);
    Task<Result<CampaignWriteResponse>> UpdateCampaignStatusAsync(Guid userId, string campaignId, UpdateCampaignStatusRequest request, CancellationToken ct = default);
    Task<Result<CampaignWriteResponse>> UpdateCampaignBudgetAsync(Guid userId, string campaignId, UpdateCampaignBudgetRequest request, CancellationToken ct = default);

    // ===== AD SETS - WRITE =====

    Task<Result<AdSetWriteResponse>> CreateAdSetAsync(Guid userId, CreateAdSetRequest request, CancellationToken ct = default);
    Task<Result<AdSetWriteResponse>> UpdateAdSetStatusAsync(Guid userId, string adSetId, UpdateAdSetStatusRequest request, CancellationToken ct = default);
    Task<Result<AdSetWriteResponse>> UpdateAdSetBudgetAsync(Guid userId, string adSetId, UpdateAdSetBudgetRequest request, CancellationToken ct = default);

    // ===== ADS - WRITE =====

    Task<Result<AdWriteResponse>> CreateAdAsync(Guid userId, CreateAdRequest request, CancellationToken ct = default);
    Task<Result<AdWriteResponse>> UpdateAdStatusAsync(Guid userId, string adId, UpdateAdStatusRequest request, CancellationToken ct = default);

    // ===== AD CREATIVES =====

    Task<Result<List<AdCreativeResponse>>> GetCreativesAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AdCreativeResponse>> CreateCreativeAsync(Guid userId, CreateAdCreativeRequest request, CancellationToken ct = default);
}
