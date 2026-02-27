import { apiFetch } from './api';
import type {
  AdAccountResponse,
  AdAccountSummary,
  ConnectAdAccountRequest,
  FacebookAd,
  FacebookAdSet,
  FacebookCampaign,
  FacebookInsight,
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
