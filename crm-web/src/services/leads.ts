import { apiFetch } from './api';

export enum CustomerStatus {
  Lead = 0,
  Contacted = 1,
  Qualified = 2,
  Negotiating = 3,
  Customer = 4,
  Inactive = 5,
  Churned = 6
}

export interface Lead {
  id: string;
  name: string;
  email: string;
  phone?: string;
  whatsApp?: string;
  companyName?: string;
  website?: string;
  notes?: string;
  tags?: string;
  status: CustomerStatus;
  createdAt: string;
  personType: number; // 0 = Individual, 1 = Company

  // Origem
  source?: number;
  sourceDescription?: string;
  sourceDetails?: string;

  // Segmentação / Outreach
  segment?: number;
  segmentDescription?: string;
  leadScore?: number;
  emailSentCount?: number;
  whatsAppSentCount?: number;
  lastEmailSentAt?: string;
  lastWhatsAppSentAt?: string;
  emailOptOut?: boolean;
  whatsAppOptOut?: boolean;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateLeadRequest {
  name: string;
  email: string;
  phone?: string;
  companyName?: string;
  personType: number;
  source?: number;
}

export interface UpdateLeadRequest {
  name: string;
  email: string;
  personType: number;
  companyName?: string;
  phone?: string;
  whatsApp?: string;
  website?: string;
  notes?: string;
  tags?: string;
  source?: number;
}

export interface LeadListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: CustomerStatus;
  sortBy?: string;
  sortDescending?: boolean;
  hasEmail?: boolean;
  hasWhatsApp?: boolean;
  personType?: number;
  source?: number;
  segment?: number;
  neverEmailed?: boolean;
  createdAfter?: string; // ISO date string
}

export async function getLeads(params: LeadListParams = {}) {
  const {
    page = 1,
    pageSize = 10,
    search,
    status,
    sortBy,
    sortDescending,
    hasEmail,
    hasWhatsApp,
    personType,
    source,
    segment,
    neverEmailed,
    createdAfter,
  } = params;

  const qs = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });

  if (search) qs.append('search', search);
  if (status !== undefined) qs.append('status', status.toString());
  if (sortBy) qs.append('sortBy', sortBy);
  if (sortDescending) qs.append('sortDescending', 'true');
  if (hasEmail !== undefined) qs.append('hasEmail', hasEmail.toString());
  if (hasWhatsApp !== undefined) qs.append('hasWhatsApp', hasWhatsApp.toString());
  if (personType !== undefined) qs.append('personType', personType.toString());
  if (source !== undefined) qs.append('source', source.toString());
  if (segment !== undefined) qs.append('segment', segment.toString());
  if (neverEmailed) qs.append('neverEmailed', 'true');
  if (createdAfter) qs.append('createdAfter', createdAfter);

  return apiFetch<PagedResponse<Lead>>(`/leads?${qs.toString()}`, {
    method: 'GET'
  });
}

export async function getLead(id: string) {
  return apiFetch<Lead>(`/leads/${id}`, {
    method: 'GET'
  });
}

export async function createLead(data: CreateLeadRequest) {
  return apiFetch<Lead>('/leads', {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function updateLead(id: string, data: UpdateLeadRequest) {
  return apiFetch<Lead>(`/leads/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}

export async function deleteLead(id: string) {
  return apiFetch<void>(`/leads/${id}`, {
    method: 'DELETE'
  });
}

export async function bulkDeleteLeads(ids: string[]): Promise<{ deletedCount: number }> {
  return apiFetch<{ deletedCount: number }>('/leads/bulk', {
    method: 'DELETE',
    body: JSON.stringify({ ids })
  });
}

export interface BulkSanitizationResponse {
  analyzedLeads: number;
  correctedLeads: number;
  removedByInvalidEmail: number;
  removedBySuspiciousDomain: number;
  removedByDirectoryOrGeneric: number;
  duplicatesConsolidated: number;
  validLeadsRemaining: number;
}

export async function sanitizeLeadBase(customerIds?: string[]): Promise<BulkSanitizationResponse> {
  return apiFetch<BulkSanitizationResponse>('/leads/sanitize-base', {
    method: 'POST',
    body: JSON.stringify({ customerIds: customerIds && customerIds.length > 0 ? customerIds : null })
  });
}
