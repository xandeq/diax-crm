import { apiFetch } from "./api";

/**
 * Estatísticas de email de um contato (via API Brevo com cache de 24h)
 */
export interface ContactEmailStats {
  email: string;
  totalSent: number;
  totalDelivered: number;
  totalOpened: number;
  totalClicked: number;
  totalBounced: number;
  totalUnsubscribed: number;
  openRate: number;
  clickRate: number;
  engagementLevel: 0 | 1 | 2; // 0=Low, 1=Medium, 2=High
  calculatedAt: string;
}

/**
 * Evento individual de email
 */
export interface EmailEvent {
  messageId: string;
  subject: string;
  event: 0 | 1 | 2 | 3 | 4 | 5 | 6; // Sent, Delivered, Opened, Clicked, Bounced, Spam, Unsubscribed
  eventAt: string;
  campaignName?: string;
  campaignId?: string;
  link?: string;
  reason?: string;
}

/**
 * Timeline de eventos de email
 */
export interface EmailTimelineData {
  email: string;
  events: EmailEvent[];
  fetchedAt: string;
}

/**
 * Busca estatísticas agregadas de email de um contato via API Brevo.
 * Cache de 24h no backend.
 */
export async function getContactEmailStats(
  customerId: string
): Promise<ContactEmailStats> {
  return apiFetch<ContactEmailStats>(`/customers/${customerId}/email-stats`);
}

/**
 * Busca timeline de eventos de email de um contato via API Brevo.
 * Cache de 24h no backend.
 */
export async function getContactEmailTimeline(
  customerId: string,
  days = 30
): Promise<EmailTimelineData> {
  return apiFetch<EmailTimelineData>(
    `/customers/${customerId}/email-timeline?days=${days}`
  );
}
