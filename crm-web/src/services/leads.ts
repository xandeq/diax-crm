import { apiFetch } from './api';

export enum CustomerStatus {
  Lead = 0,
  Contacted = 1,
  Qualified = 2,
  ProposalSent = 3,
  Negotiation = 4,
  Customer = 5,
  Churned = 6,
  Lost = 7
}

export interface Lead {
  id: string;
  name: string;
  email: string;
  phone?: string;
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

export async function getLeads(page = 1, pageSize = 10, search = '', status?: CustomerStatus) {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });

  if (search) params.append('search', search);
  if (status !== undefined) params.append('status', status.toString());

  return apiFetch<PagedResponse<Lead>>(`/leads?${params.toString()}`, {
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
