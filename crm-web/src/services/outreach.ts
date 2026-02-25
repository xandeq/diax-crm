import { apiFetch } from './api';
import { EmailAttachmentRequest } from './emailMarketing';

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
  whatsAppSendEnabled: boolean;
  dailyWhatsAppLimit: number;
  whatsAppCooldownDays: number;
  whatsAppHotTemplate: string | null;
  whatsAppWarmTemplate: string | null;
  whatsAppColdTemplate: string | null;
  whatsAppFollowUpTemplate: string | null;
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
  whatsAppSendEnabled?: boolean;
  dailyWhatsAppLimit?: number;
  whatsAppCooldownDays?: number;
  whatsAppHotTemplate?: string;
  whatsAppWarmTemplate?: string;
  whatsAppColdTemplate?: string;
  whatsAppFollowUpTemplate?: string;
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
  whatsAppSendEnabled: boolean;
  whatsAppSentToday: number;
  whatsAppSentThisWeek: number;
  whatsAppReadyCount: number;
  whatsAppConnectionStatus: string;
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

export interface WhatsAppReadyLeadResponse {
  id: string;
  name: string;
  whatsApp: string | null;
  phone: string | null;
  email: string | null;
  segmentLabel: string;
  leadScore: number | null;
  whatsAppSentCount: number;
  lastWhatsAppSentAt: string | null;
}

export interface WhatsAppSendResultResponse {
  sentCount: number;
  skippedCount: number;
  failedCount: number;
  skippedReasons: string[];
  failedReasons: string[];
}

export interface WhatsAppSendRequest {
  customerId?: string;
  phoneNumber?: string;
  message: string;
}

export interface WhatsAppConnectionStatus {
  isConnected: boolean;
  state: string;
  instanceName: string | null;
}

export interface WhatsAppSendResult {
  success: boolean;
  messageId: string | null;
  error: string | null;
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

export async function getWhatsAppStatus(): Promise<WhatsAppConnectionStatus> {
  return apiFetch('/whatsapp/status');
}

export async function getWhatsAppReadyLeads(): Promise<WhatsAppReadyLeadResponse[]> {
  return apiFetch('/whatsapp/ready-leads');
}

export async function sendWhatsApp(request: WhatsAppSendRequest): Promise<WhatsAppSendResult> {
  return apiFetch('/whatsapp/send', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function sendWhatsAppCampaign(): Promise<WhatsAppSendResultResponse> {
  return apiFetch('/whatsapp/send-campaign', {
    method: 'POST',
  });
}

export async function sendWhatsAppFollowUp(): Promise<WhatsAppSendResultResponse> {
  return apiFetch('/whatsapp/send-followup', {
    method: 'POST',
  });
}

// ===== Email Queue =====

export enum EmailQueueStatus {
  Queued = 0,
  Processing = 1,
  Sent = 2,
  Failed = 3,
}

export interface EmailQueueItemResponse {
  id: string;
  campaignId: string | null;
  customerId: string | null;
  recipientName: string;
  recipientEmail: string;
  subject: string;
  status: EmailQueueStatus;
  scheduledAt: string;
  sentAt: string | null;
  attemptCount: number;
  lastError: string | null;
  createdAt: string;
}

export interface PagedEmailQueueResponse {
  items: EmailQueueItemResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export async function getEmailQueue(page = 1, pageSize = 15): Promise<PagedEmailQueueResponse> {
  return apiFetch(`/email-campaigns/queue?page=${page}&pageSize=${pageSize}`);
}

// ===== Email Marketing =====

export interface ValidEmailCountResponse {
  count: number;
}

export interface SendEmailMarketingRequest {
  subject: string;
  bodyHtml: string;
  attachments?: EmailAttachmentRequest[];
}

export interface SendEmailMarketingResponse {
  campaignId: string;
  totalValidContacts: number;
  queuedCount: number;
  skippedCount: number;
  skippedReasons: string[];
}

export async function getValidEmailCount(): Promise<ValidEmailCountResponse> {
  return apiFetch('/outreach/valid-email-count');
}

export async function sendEmailMarketing(data: SendEmailMarketingRequest): Promise<SendEmailMarketingResponse> {
  return apiFetch('/outreach/send-email-marketing', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
