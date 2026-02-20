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

export enum PersonType {
  Individual = 0,
  Company = 1
}

export interface Customer {
  id: string;
  name: string;
  email: string;
  phone?: string;
  companyName?: string;
  personType: PersonType;
  document?: string;
  secondaryEmail?: string;
  whatsApp?: string;
  website?: string;
  status: CustomerStatus;
  createdAt: string;
  notes?: string;
  tags?: string;
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

export interface CreateCustomerRequest {
  name: string;
  email: string;
  personType: PersonType;
  companyName?: string;
  document?: string;
  phone?: string;
  secondaryEmail?: string;
  whatsApp?: string;
  website?: string;
  notes?: string;
  tags?: string;
  source?: number;
}

export interface UpdateCustomerRequest {
  name: string;
  email: string;
  personType: PersonType;
  companyName?: string;
  document?: string;
  phone?: string;
  secondaryEmail?: string;
  whatsApp?: string;
  website?: string;
  notes?: string;
  tags?: string;
  source?: number;
}

export async function getCustomers(page = 1, pageSize = 10, search = '', status?: CustomerStatus) {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
    onlyCustomers: 'true'
  });

  if (search) params.append('search', search);
  if (status !== undefined) params.append('status', status.toString());

  return apiFetch<PagedResponse<Customer>>(`/customers?${params.toString()}`, {
    method: 'GET'
  });
}

export async function getCustomer(id: string) {
  return apiFetch<Customer>(`/customers/${id}`, {
    method: 'GET'
  });
}

export async function createCustomer(data: CreateCustomerRequest) {
  return apiFetch<Customer>('/customers', {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

export async function updateCustomer(id: string, data: UpdateCustomerRequest) {
  return apiFetch<Customer>(`/customers/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}

export async function deleteCustomer(id: string) {
  return apiFetch<void>(`/customers/${id}`, {
    method: 'DELETE'
  });
}

export interface LeadActivity {
  type: string;
  title: string;
  detail?: string;
  date: string;
  status: 'success' | 'warning' | 'info' | 'error';
}

export async function getCustomerActivities(id: string): Promise<LeadActivity[]> {
  return apiFetch<LeadActivity[]>(`/customers/${id}/activities`, {
    method: 'GET'
  });
}
