import { apiFetch } from './api';

export interface OutreachConfigResponse {
  id: string;
  apifyDatasetUrl: string | null;
  apifyApiToken: string | null;
  importEnabled: boolean;
  segmentationEnabled: boolean;
  sendEnabled: boolean;
  dailyEmailLimit: number;
  emailCooldownDays: number;
  hotTemplateSubject: string | null;
  hotTemplateBody: string | null;
  warmTemplateSubject: string | null;
  warmTemplateBody: string | null;
  coldTemplateSubject: string | null;
  coldTemplateBody: string | null;
}

export interface UpdateOutreachConfigRequest {
  apifyDatasetUrl?: string;
  apifyApiToken?: string;
  importEnabled?: boolean;
  segmentationEnabled?: boolean;
  sendEnabled?: boolean;
  dailyEmailLimit?: number;
  emailCooldownDays?: number;
  hotTemplateSubject?: string;
  hotTemplateBody?: string;
  warmTemplateSubject?: string;
  warmTemplateBody?: string;
  coldTemplateSubject?: string;
  coldTemplateBody?: string;
}

export interface OutreachDashboardResponse {
  totalLeads: number;
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  unsegmentedLeads: number;
  emailsSentToday: number;
  emailsSentThisWeek: number;
  emailsSentThisMonth: number;
  pendingInQueue: number;
  failedInQueue: number;
  importEnabled: boolean;
  segmentationEnabled: boolean;
  sendEnabled: boolean;
}

export interface SegmentationResultResponse {
  totalProcessed: number;
  hotCount: number;
  warmCount: number;
  coldCount: number;
}

export interface OutreachSendResultResponse {
  campaignId: string | null;
  queuedCount: number;
  skippedCount: number;
  skippedReasons: string[];
}

export interface ReadyLeadResponse {
  id: string;
  name: string;
  email: string;
  segment: string;
  leadScore: number;
  companyName: string | null;
  lastEmailSentAt: string | null;
}

export async function getOutreachConfig() {
  return apiFetch<OutreachConfigResponse>('/outreach/config', {
    method: 'GET',
  });
}

export async function updateOutreachConfig(data: UpdateOutreachConfigRequest) {
  return apiFetch<OutreachConfigResponse>('/outreach/config', {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function runSegmentation() {
  return apiFetch<SegmentationResultResponse>('/outreach/segment', {
    method: 'POST',
  });
}

export async function sendOutreachCampaign() {
  return apiFetch<OutreachSendResultResponse>('/outreach/send', {
    method: 'POST',
  });
}

export async function getOutreachDashboard() {
  return apiFetch<OutreachDashboardResponse>('/outreach/dashboard', {
    method: 'GET',
  });
}

export async function getReadyLeads(limit = 50) {
  return apiFetch<ReadyLeadResponse[]>(`/outreach/ready-leads?limit=${limit}`, {
    method: 'GET',
  });
}
