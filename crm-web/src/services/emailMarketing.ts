import { apiFetch } from './api';

export interface EmailAttachmentRequest {
  fileName: string;
  contentType: string;
  base64Content: string;
}

export interface QueueSingleEmailRequest {
  customerId?: string;
  recipientName: string;
  recipientEmail: string;
  subject: string;
  htmlBody: string;
  scheduledAt?: string;
  attachments?: EmailAttachmentRequest[];
}

export interface QueueBulkEmailRequest {
  customerIds: string[];
  subject: string;
  htmlBody: string;
  scheduledAt?: string;
  attachments?: EmailAttachmentRequest[];
}

export interface QueueBulkEmailResponse {
  requestedCount: number;
  queuedCount: number;
  skippedCount: number;
  skippedRecipients: string[];
}

export interface CreateEmailCampaignRequest {
  name: string;
  subject: string;
  bodyHtml: string;
  sourceSnippetId?: string;
}

export interface UpdateEmailCampaignRequest {
  name: string;
  subject: string;
  bodyHtml: string;
  sourceSnippetId?: string;
}

export interface EmailCampaignResponse {
  id: string;
  name: string;
  subject: string;
  bodyHtml: string;
  scheduledAt?: string;
  status: number;
  sourceSnippetId?: string;
  totalRecipients: number;
  sentCount: number;
  failedCount: number;
  openCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface PreviewCampaignRequest {
  subjectOverride?: string;
  bodyHtmlOverride?: string;
  sourceSnippetIdOverride?: string;
  mockData: {
    firstName?: string;
    email?: string;
    company?: string;
    leadStatus?: string;
  };
}

export interface PreviewCampaignResponse {
  campaignId: string;
  templateSource: string;
  subjectTemplate: string;
  bodyTemplate: string;
  renderedSubject: string;
  renderedBodyHtml: string;
  variables: Record<string, string | null>;
}

export interface QueueCampaignRecipientsRequest {
  customerIds: string[];
  scheduledAt?: string;
  bodyHtmlOverride?: string;
  attachments?: EmailAttachmentRequest[];
}

export interface QueueCampaignRecipientsResponse {
  campaignId: string;
  requestedCount: number;
  queuedCount: number;
  skippedCount: number;
  effectiveScheduledAt: string;
  skippedRecipients: string[];
}

export async function queueSingleEmail(data: QueueSingleEmailRequest) {
  return apiFetch('/email-campaigns/send-single', {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function queueBulkEmail(data: QueueBulkEmailRequest) {
  return apiFetch<QueueBulkEmailResponse>('/email-campaigns/send-bulk', {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function createEmailCampaign(data: CreateEmailCampaignRequest) {
  return apiFetch<EmailCampaignResponse>('/email-campaigns/campaigns', {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function updateEmailCampaign(campaignId: string, data: UpdateEmailCampaignRequest) {
  return apiFetch<EmailCampaignResponse>(`/email-campaigns/campaigns/${campaignId}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}

export async function previewEmailCampaign(campaignId: string, data: PreviewCampaignRequest) {
  return apiFetch<PreviewCampaignResponse>(`/email-campaigns/campaigns/${campaignId}/preview`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function getEmailCampaigns(page = 1, pageSize = 20) {
  return apiFetch<{ items: EmailCampaignResponse[]; totalCount: number }>(`/email-campaigns/campaigns?page=${page}&pageSize=${pageSize}`, {
    method: 'GET'
  });
}

export async function queueCampaignRecipients(campaignId: string, data: QueueCampaignRecipientsRequest) {
  return apiFetch<QueueCampaignRecipientsResponse>(`/email-campaigns/campaigns/${campaignId}/queue`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
}