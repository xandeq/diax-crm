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
