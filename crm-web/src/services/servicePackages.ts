import { apiFetch } from './api';

// ── Pacotes de serviço (catálogo com preço fixo, base das propostas) ─────────

export interface ServicePackage {
  id: string;
  name: string;
  shortDescription: string;
  /** Itens do escopo, um por linha. */
  scope: string;
  price: number;
  deliveryDays: number;
  sortOrder: number;
  isActive: boolean;
}

export type UpsertServicePackage = Omit<ServicePackage, 'id'>;

export async function listServicePackages(onlyActive = false): Promise<ServicePackage[]> {
  return apiFetch<ServicePackage[]>(`/service-packages?onlyActive=${onlyActive}`, { method: 'GET' });
}

export async function createServicePackage(data: UpsertServicePackage): Promise<ServicePackage> {
  return apiFetch<ServicePackage>('/service-packages', { method: 'POST', body: JSON.stringify(data) });
}

export async function updateServicePackage(id: string, data: UpsertServicePackage): Promise<ServicePackage> {
  return apiFetch<ServicePackage>(`/service-packages/${id}`, { method: 'PUT', body: JSON.stringify(data) });
}
