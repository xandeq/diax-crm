import { apiFetch } from './api';

// ── Agendamento de reuniões ──────────────────────────────────────────────────

export interface AvailabilityDay {
  date: string; // yyyy-MM-dd (dia em BRT)
  slots: string[]; // ISO UTC
}

export interface Meeting {
  id: string;
  customerId?: string | null;
  contactName: string;
  contactEmail: string;
  contactPhone?: string | null;
  notes?: string | null;
  scheduledAt: string;
  durationMinutes: number;
  status: 0 | 1 | 2; // Confirmed | Cancelled | Completed
}

export interface PublicBookingRequest {
  userId: string;
  scheduledAt: string;
  name: string;
  email: string;
  phone?: string | null;
  notes?: string | null;
}

// Públicos (página /agendar)
export async function getAvailability(userId: string, days = 7): Promise<AvailabilityDay[]> {
  return apiFetch<AvailabilityDay[]>(`/meetings/public/availability?u=${userId}&days=${days}`, { method: 'GET' });
}

export async function bookMeeting(data: PublicBookingRequest): Promise<Meeting> {
  return apiFetch<Meeting>('/meetings/public/book', { method: 'POST', body: JSON.stringify(data) });
}

// Autenticados
export async function listUpcomingMeetings(): Promise<Meeting[]> {
  return apiFetch<Meeting[]>('/meetings', { method: 'GET' });
}

export async function cancelMeeting(id: string): Promise<Meeting> {
  return apiFetch<Meeting>(`/meetings/${id}/cancel`, { method: 'POST' });
}

export async function getBookingLinkUserId(): Promise<string> {
  const r = await apiFetch<{ userId: string }>('/meetings/booking-link', { method: 'GET' });
  return r.userId;
}

/** URL pública de agendamento (query string — deploy é export estático). */
export function publicBookingUrl(userId: string): string {
  return `${window.location.origin}/agendar?u=${userId}`;
}
