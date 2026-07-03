import { apiFetch } from './api';

// ── Propostas comerciais ─────────────────────────────────────────────────────

export type ProposalStatus = 0 | 1 | 2 | 3 | 4; // Draft | Sent | Accepted | Paid | Cancelled

export const PROPOSAL_STATUS_LABEL: Record<number, string> = {
  0: 'Rascunho', 1: 'Enviada', 2: 'Aceita', 3: 'Paga', 4: 'Cancelada',
};

export interface Proposal {
  id: string;
  customerId: string;
  customerName: string;
  title: string;
  description: string;
  amount: number;
  status: ProposalStatus;
  publicToken: string;
  pixKey?: string | null;
  validUntil?: string | null;
  sentAt?: string | null;
  acceptedAt?: string | null;
  paidAt?: string | null;
  viewCount: number;
  createdAt: string;
}

export interface PublicProposal {
  title: string;
  description: string;
  amount: number;
  customerName: string;
  status: ProposalStatus;
  isExpired: boolean;
  validUntil?: string | null;
  acceptedAt?: string | null;
  pixCopiaECola?: string | null;
}

export interface CreateProposalRequest {
  customerId: string;
  title: string;
  description: string;
  amount: number;
  pixKey?: string | null;
  validUntil?: string | null;
}

export async function createProposal(data: CreateProposalRequest): Promise<Proposal> {
  return apiFetch<Proposal>('/proposals', { method: 'POST', body: JSON.stringify(data) });
}

export async function listProposals(): Promise<Proposal[]> {
  return apiFetch<Proposal[]>('/proposals', { method: 'GET' });
}

export async function markProposalPaid(id: string): Promise<Proposal> {
  return apiFetch<Proposal>(`/proposals/${id}/mark-paid`, { method: 'POST' });
}

export async function cancelProposal(id: string): Promise<Proposal> {
  return apiFetch<Proposal>(`/proposals/${id}/cancel`, { method: 'POST' });
}

/** Envia a proposta por email ao cliente (fallback multi-provider; idempotente por dia). */
export async function sendProposalEmail(id: string): Promise<string> {
  const r = await apiFetch<{ message: string }>(`/proposals/${id}/send-email`, {
    method: 'POST',
    body: JSON.stringify({ publicBaseUrl: window.location.origin }),
  });
  return r.message;
}

// Públicos (sem auth — usados pela página /proposta/[token])
export async function getPublicProposal(token: string): Promise<PublicProposal> {
  return apiFetch<PublicProposal>(`/proposals/public/${token}`, { method: 'GET' });
}

export async function acceptPublicProposal(token: string): Promise<PublicProposal> {
  return apiFetch<PublicProposal>(`/proposals/public/${token}/accept`, { method: 'POST' });
}

/** Monta a URL pública da proposta a partir do token (query string — deploy é export estático). */
export function publicProposalUrl(token: string): string {
  return `${window.location.origin}/proposta?t=${token}`;
}
