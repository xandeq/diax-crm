namespace Diax.Application.Ads.Dtos;

// ===== Account =====

public record ConnectAdAccountRequest(
    string AdAccountId,
    string AccessToken);

public record AdAccountResponse(
    Guid Id,
    string AdAccountId,
    string AccountName,
    string Currency,
    string Timezone,
    string AccountStatus,
    bool IsActive,
    DateTime? LastSyncAt,
    DateTime CreatedAt);

// ===== Campaigns =====

public record FacebookCampaignDto(
    string Id,
    string Name,
    string Status,
    string Objective,
    string? BudgetRemaining,
    string? DailyBudget,
    string? LifetimeBudget,
    DateTime? StartTime,
    DateTime? StopTime,
    DateTime CreatedTime,
    DateTime UpdatedTime);

// ===== Ad Sets =====

public record FacebookAdSetDto(
    string Id,
    string CampaignId,
    string Name,
    string Status,
    string? DailyBudget,
    string? LifetimeBudget,
    string BillingEvent,
    string OptimizationGoal,
    DateTime? StartTime,
    DateTime? EndTime,
    DateTime CreatedTime,
    DateTime UpdatedTime);

// ===== Ads =====

public record FacebookAdDto(
    string Id,
    string AdSetId,
    string CampaignId,
    string Name,
    string Status,
    string? CreativeId,
    DateTime CreatedTime,
    DateTime UpdatedTime);

// ===== Insights =====

public record InsightsRequest(
    string? DatePreset = "last_30d",
    string? Since = null,
    string? Until = null,
    string Level = "campaign");

public record FacebookInsightDto(
    string CampaignId,
    string? CampaignName,
    string? AdSetId,
    string? AdSetName,
    string? AdId,
    string? AdName,
    string DateStart,
    string DateStop,
    string Impressions,
    string Clicks,
    string Spend,
    string Reach,
    string? Ctr,
    string? Cpc,
    string? Cpm,
    string? Cpp,
    string? Frequency,
    List<FacebookActionDto> Actions,
    List<FacebookActionDto> Conversions);

public record FacebookActionDto(
    string ActionType,
    string Value);

// ===== Summary =====

public record AdAccountSummaryResponse(
    AdAccountResponse Account,
    int TotalCampaigns,
    int ActiveCampaigns,
    decimal TotalSpend,
    decimal TotalImpressions,
    decimal TotalClicks,
    double AverageCtr);

// ===== Campaign Write =====

public record CreateCampaignRequest(
    string Name,
    string Objective,
    string Status,
    string? DailyBudget,
    string? LifetimeBudget,
    DateTime? StartTime,
    DateTime? StopTime);

public record UpdateCampaignStatusRequest(string Status);

public record UpdateCampaignBudgetRequest(
    string? DailyBudget,
    string? LifetimeBudget,
    string? BidStrategy);

public record CampaignWriteResponse(
    string Id,
    string Status,
    string Name);

// ===== Ad Set Write =====

public record TargetingRequest(
    List<string>? Countries,
    List<string>? Regions,
    int? AgeMin,
    int? AgeMax,
    List<int>? Genders,
    List<long>? InterestIds,
    List<long>? BehaviorIds,
    List<string>? DevicePlatforms,
    List<string>? FacebookPositions,
    List<string>? InstagramPositions,
    List<string>? CustomAudienceIds);

public record CreateAdSetRequest(
    string CampaignId,
    string Name,
    string Status,
    string OptimizationGoal,
    string BillingEvent,
    string BidStrategy,
    string? DailyBudget,
    string? LifetimeBudget,
    string? BidAmount,
    TargetingRequest Targeting,
    DateTime? StartTime,
    DateTime? EndTime);

public record UpdateAdSetStatusRequest(string Status);

public record UpdateAdSetBudgetRequest(
    string? DailyBudget,
    string? LifetimeBudget,
    string? BidAmount,
    string? BidStrategy);

public record AdSetWriteResponse(
    string Id,
    string Status,
    string Name);

// ===== Ad Write =====

public record CreateAdRequest(
    string AdSetId,
    string Name,
    string Status,
    string CreativeId);

public record UpdateAdStatusRequest(string Status);

public record AdWriteResponse(
    string Id,
    string Status,
    string Name);

// ===== Ad Creative =====

public record CreateAdCreativeRequest(
    string Name,
    string ObjectType,
    string PageId,
    string? Body,
    string? Title,
    string? CallToActionType,
    string? LinkUrl,
    string? ImageHash,
    string? VideoId,
    List<CarouselElementRequest>? CarouselElements);

public record CarouselElementRequest(
    string Title,
    string? Description,
    string ImageHash,
    string LinkUrl,
    string CallToActionType);

public record AdCreativeResponse(
    string Id,
    string Name,
    string ObjectType,
    string Status,
    string? ThumbnailUrl,
    string? Body,
    string? Title,
    string? CallToActionType,
    string? LinkUrl,
    DateTime CreatedTime);

// ===== Insights Enhanced =====

public record UpdatedInsightsRequest(
    string? DatePreset = "last_30d",
    string? Since = null,
    string? Until = null,
    string Level = "campaign",
    string? CampaignId = null,
    string? AdSetId = null,
    string? Breakdown = null);
