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
  status: CustomerStatus;
  createdAt: string;
  personType: number; // 0 = Individual, 1 = Company
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
  phone?: string;
  companyName?: string;
  personType: number;
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
