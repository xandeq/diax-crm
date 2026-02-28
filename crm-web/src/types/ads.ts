// ===== Account =====

export interface AdAccountResponse {
  id: string;
  adAccountId: string;
  accountName: string;
  currency: string;
  timezone: string;
  accountStatus: string;
  isActive: boolean;
  lastSyncAt: string | null;
  createdAt: string;
}

export interface ConnectAdAccountRequest {
  adAccountId: string;
  accessToken: string;
}

// ===== Campaigns =====

export interface FacebookCampaign {
  id: string;
  name: string;
  status: string;
  objective: string;
  budgetRemaining: string | null;
  dailyBudget: string | null;
  lifetimeBudget: string | null;
  startTime: string | null;
  stopTime: string | null;
  createdTime: string;
  updatedTime: string;
}

// ===== Ad Sets =====

export interface FacebookAdSet {
  id: string;
  campaignId: string;
  name: string;
  status: string;
  dailyBudget: string | null;
  lifetimeBudget: string | null;
  billingEvent: string;
  optimizationGoal: string;
  startTime: string | null;
  endTime: string | null;
  createdTime: string;
  updatedTime: string;
}

// ===== Ads =====

export interface FacebookAd {
  id: string;
  adSetId: string;
  campaignId: string;
  name: string;
  status: string;
  creativeId: string | null;
  createdTime: string;
  updatedTime: string;
}

// ===== Insights =====

export interface FacebookAction {
  actionType: string;
  value: string;
}

export interface FacebookInsight {
  campaignId: string;
  campaignName: string | null;
  adSetId: string | null;
  adSetName: string | null;
  adId: string | null;
  adName: string | null;
  dateStart: string;
  dateStop: string;
  impressions: string;
  clicks: string;
  spend: string;
  reach: string;
  ctr: string | null;
  cpc: string | null;
  cpm: string | null;
  cpp: string | null;
  frequency: string | null;
  actions: FacebookAction[];
  conversions: FacebookAction[];
}

// ===== Summary =====

export interface AdAccountSummary {
  account: AdAccountResponse;
  totalCampaigns: number;
  activeCampaigns: number;
  totalSpend: number;
  totalImpressions: number;
  totalClicks: number;
  averageCtr: number;
}

// ===== Campaign Write =====

export interface CreateCampaignRequest {
  name: string;
  objective: string;
  status: 'ACTIVE' | 'PAUSED';
  dailyBudget?: string;
  lifetimeBudget?: string;
  startTime?: string;
  stopTime?: string;
}

export interface UpdateCampaignStatusRequest {
  status: 'ACTIVE' | 'PAUSED';
}

export interface UpdateCampaignBudgetRequest {
  dailyBudget?: string;
  lifetimeBudget?: string;
  bidStrategy?: string;
}

export interface CampaignWriteResponse {
  id: string;
  status: string;
  name: string;
}

// ===== Ad Set Write =====

export interface TargetingRequest {
  countries?: string[];
  regions?: string[];
  ageMin?: number;
  ageMax?: number;
  genders?: number[];
  interestIds?: number[];
  behaviorIds?: number[];
  devicePlatforms?: string[];
  facebookPositions?: string[];
  instagramPositions?: string[];
  customAudienceIds?: string[];
}

export interface CreateAdSetRequest {
  campaignId: string;
  name: string;
  status: 'ACTIVE' | 'PAUSED';
  optimizationGoal: string;
  billingEvent: string;
  bidStrategy: string;
  dailyBudget?: string;
  lifetimeBudget?: string;
  bidAmount?: string;
  targeting: TargetingRequest;
  startTime?: string;
  endTime?: string;
}

export interface UpdateAdSetStatusRequest {
  status: 'ACTIVE' | 'PAUSED';
}

export interface UpdateAdSetBudgetRequest {
  dailyBudget?: string;
  lifetimeBudget?: string;
  bidAmount?: string;
  bidStrategy?: string;
}

export interface AdSetWriteResponse {
  id: string;
  status: string;
  name: string;
}

// ===== Ad Write =====

export interface CreateAdRequest {
  adSetId: string;
  name: string;
  status: 'ACTIVE' | 'PAUSED';
  creativeId: string;
}

export interface UpdateAdStatusRequest {
  status: 'ACTIVE' | 'PAUSED';
}

export interface AdWriteResponse {
  id: string;
  status: string;
  name: string;
}

// ===== Ad Creative =====

export interface CarouselElementRequest {
  title: string;
  description?: string;
  imageHash: string;
  linkUrl: string;
  callToActionType: string;
}

export interface CreateAdCreativeRequest {
  name: string;
  objectType: 'IMAGE' | 'VIDEO' | 'CAROUSEL';
  pageId: string;
  body?: string;
  title?: string;
  callToActionType?: string;
  linkUrl?: string;
  imageHash?: string;
  videoId?: string;
  carouselElements?: CarouselElementRequest[];
}

export interface AdCreativeResponse {
  id: string;
  name: string;
  objectType: string;
  status: string;
  thumbnailUrl?: string;
  body?: string;
  title?: string;
  callToActionType?: string;
  linkUrl?: string;
  createdTime: string;
}

// ===== Status colors =====

export const STATUS_COLORS: Record<string, string> = {
  ACTIVE: 'text-green-700 bg-green-50 border-green-200',
  PAUSED: 'text-yellow-700 bg-yellow-50 border-yellow-200',
  DELETED: 'text-red-700 bg-red-50 border-red-200',
  ARCHIVED: 'text-slate-700 bg-slate-50 border-slate-200',
  PENDING_REVIEW: 'text-blue-700 bg-blue-50 border-blue-200',
  DISAPPROVED: 'text-red-700 bg-red-50 border-red-200',
  PREAPPROVED: 'text-teal-700 bg-teal-50 border-teal-200',
  PENDING_BILLING_INFO: 'text-orange-700 bg-orange-50 border-orange-200',
  CAMPAIGN_PAUSED: 'text-yellow-700 bg-yellow-50 border-yellow-200',
  ADSET_PAUSED: 'text-yellow-700 bg-yellow-50 border-yellow-200',
  UNKNOWN: 'text-slate-700 bg-slate-50 border-slate-200',
};
