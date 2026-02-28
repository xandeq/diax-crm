import { apiFetch } from './api';
import type {
  AdAccountResponse,
  AdAccountSummary,
  AdCreativeResponse,
  AdSetWriteResponse,
  AdWriteResponse,
  CampaignWriteResponse,
  ConnectAdAccountRequest,
  CreateAdCreativeRequest,
  CreateAdRequest,
  CreateAdSetRequest,
  CreateCampaignRequest,
  FacebookAd,
  FacebookAdSet,
  FacebookCampaign,
  FacebookInsight,
  UpdateAdSetBudgetRequest,
  UpdateAdSetStatusRequest,
  UpdateAdStatusRequest,
  UpdateCampaignBudgetRequest,
  UpdateCampaignStatusRequest,
} from '@/types/ads';

const BASE = '/ads';

// ===== Account =====

export async function connectAdAccount(data: ConnectAdAccountRequest): Promise<AdAccountResponse> {
  return apiFetch<AdAccountResponse>(`${BASE}/connect`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function getAdAccount(): Promise<AdAccountResponse> {
  return apiFetch<AdAccountResponse>(`${BASE}/account`);
}

export async function syncAdAccount(): Promise<AdAccountResponse> {
  return apiFetch<AdAccountResponse>(`${BASE}/account/sync`, { method: 'POST' });
}

export async function disconnectAdAccount(): Promise<void> {
  return apiFetch<void>(`${BASE}/account`, { method: 'DELETE' });
}

// ===== Summary =====

export async function getAdAccountSummary(): Promise<AdAccountSummary> {
  return apiFetch<AdAccountSummary>(`${BASE}/summary`);
}

// ===== Campaigns =====

export async function getCampaigns(): Promise<FacebookCampaign[]> {
  return apiFetch<FacebookCampaign[]>(`${BASE}/campaigns`);
}

// ===== Ad Sets =====

export async function getAdSets(campaignId?: string): Promise<FacebookAdSet[]> {
  const query = campaignId ? `?campaignId=${encodeURIComponent(campaignId)}` : '';
  return apiFetch<FacebookAdSet[]>(`${BASE}/adsets${query}`);
}

// ===== Ads =====

export async function getAds(adSetId?: string): Promise<FacebookAd[]> {
  const query = adSetId ? `?adSetId=${encodeURIComponent(adSetId)}` : '';
  return apiFetch<FacebookAd[]>(`${BASE}/ads${query}`);
}

// ===== Insights =====

export interface InsightsParams {
  datePreset?: string;
  since?: string;
  until?: string;
  level?: string;
}

export async function getInsights(params: InsightsParams = {}): Promise<FacebookInsight[]> {
  const q = new URLSearchParams();
  if (params.datePreset) q.set('datePreset', params.datePreset);
  if (params.since) q.set('since', params.since);
  if (params.until) q.set('until', params.until);
  if (params.level) q.set('level', params.level);
  const query = q.toString() ? `?${q.toString()}` : '';
  return apiFetch<FacebookInsight[]>(`${BASE}/insights${query}`);
}

// ===== Campaigns - Write =====

export async function createCampaign(data: CreateCampaignRequest): Promise<CampaignWriteResponse> {
  return apiFetch<CampaignWriteResponse>(`${BASE}/campaigns`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCampaignStatus(
  campaignId: string,
  data: UpdateCampaignStatusRequest
): Promise<CampaignWriteResponse> {
  return apiFetch<CampaignWriteResponse>(`${BASE}/campaigns/${campaignId}/status`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCampaignBudget(
  campaignId: string,
  data: UpdateCampaignBudgetRequest
): Promise<CampaignWriteResponse> {
  return apiFetch<CampaignWriteResponse>(`${BASE}/campaigns/${campaignId}/budget`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// ===== Ad Sets - Write =====

export async function createAdSet(data: CreateAdSetRequest): Promise<AdSetWriteResponse> {
  return apiFetch<AdSetWriteResponse>(`${BASE}/adsets`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateAdSetStatus(
  adSetId: string,
  data: UpdateAdSetStatusRequest
): Promise<AdSetWriteResponse> {
  return apiFetch<AdSetWriteResponse>(`${BASE}/adsets/${adSetId}/status`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateAdSetBudget(
  adSetId: string,
  data: UpdateAdSetBudgetRequest
): Promise<AdSetWriteResponse> {
  return apiFetch<AdSetWriteResponse>(`${BASE}/adsets/${adSetId}/budget`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// ===== Ads - Write =====

export async function createAd(data: CreateAdRequest): Promise<AdWriteResponse> {
  return apiFetch<AdWriteResponse>(`${BASE}/ads`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateAdStatus(
  adId: string,
  data: UpdateAdStatusRequest
): Promise<AdWriteResponse> {
  return apiFetch<AdWriteResponse>(`${BASE}/ads/${adId}/status`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// ===== Ad Creatives =====

export async function getCreatives(): Promise<AdCreativeResponse[]> {
  return apiFetch<AdCreativeResponse[]>(`${BASE}/creatives`);
}

export async function createCreative(data: CreateAdCreativeRequest): Promise<AdCreativeResponse> {
  return apiFetch<AdCreativeResponse>(`${BASE}/creatives`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
