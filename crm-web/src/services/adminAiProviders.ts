import { AiModel, AiProvider } from './aiCatalog';
import { apiFetch, apiRequest } from './api';

export interface CreateAiProviderRequest {
  key: string;
  name: string;
  supportsListModels: boolean;
  baseUrl?: string;
}

export interface UpdateAiProviderRequest {
  name: string;
  supportsListModels: boolean;
  baseUrl?: string;
  isEnabled: boolean;
}

export interface SyncModelsResponse {
  discoveredCount: number;
  newModels: number;
  existingModelsUpdated: number;
  errors: string[];
}

export const adminAiProvidersService = {
  getAll: async (): Promise<AiProvider[]> => {
    return await apiFetch<AiProvider[]>('/admin/ai/providers');
  },

  getById: async (id: string): Promise<AiProvider> => {
    return await apiFetch<AiProvider>(`/admin/ai/providers/${id}`);
  },

  create: async (data: CreateAiProviderRequest): Promise<AiProvider> => {
    return await apiRequest<AiProvider>('/admin/ai/providers', 'POST', data);
  },

  update: async (id: string, data: UpdateAiProviderRequest): Promise<void> => {
    await apiRequest(`/admin/ai/providers/${id}`, 'PUT', data);
  },

  delete: async (id: string): Promise<void> => {
    await apiRequest(`/admin/ai/providers/${id}`, 'DELETE');
  },

  // Models
  getModels: async (providerId: string): Promise<AiModel[]> => {
    return await apiFetch<AiModel[]>(`/admin/ai/providers/${providerId}/models`);
  },

  addModel: async (providerId: string, model: Partial<AiModel>): Promise<AiModel> => {
    return await apiRequest<AiModel>(`/admin/ai/providers/${providerId}/models`, 'POST', model);
  },

  updateModel: async (modelId: string, model: Partial<AiModel>): Promise<void> => {
    await apiRequest(`/admin/ai/models/${modelId}`, 'PUT', model);
  },

  deleteModel: async (modelId: string): Promise<void> => {
    await apiRequest(`/admin/ai/models/${modelId}`, 'DELETE');
  },

  syncModels: async (providerId: string): Promise<SyncModelsResponse> => {
    return await apiRequest<SyncModelsResponse>(`/admin/ai/providers/${providerId}/sync-models`, 'POST');
  }
};
