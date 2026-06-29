import { apiFetch } from '@/services/api';

export interface ServiceStatus {
  name: string;
  url: string;
  domain: string;
  category: string;
  online: boolean;
  statusCode: number;
  responseTimeMs: number;
}

export interface StatusResponse {
  checkedAt: string;
  services: ServiceStatus[];
}

/**
 * Fetches the live status of all monitored SaaS apps and websites.
 * Auth/base-URL/error handling are delegated to the shared apiFetch wrapper.
 */
export async function getServicesStatus(): Promise<StatusResponse> {
  return apiFetch<StatusResponse>('/status/services');
}
