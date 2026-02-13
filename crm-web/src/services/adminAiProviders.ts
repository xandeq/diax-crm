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

export interface DiscoveredModel {
  id: string;
  name: string;
  provider: string;
  contextLength?: number;
  inputCostHint?: string;
  outputCostHint?: string;
}

export interface DiscoverModelsResponse {
  success: boolean;
  data: DiscoveredModel[];
  totalCount: number;
  error?: string;
  details?: string;
}

export interface CredentialStatusDto {
  isConfigured: boolean;
  lastFourDigits: string | null;
}

export interface TestConnectionResultDto {
  success: boolean;
  message: string;
  errorDetails?: string | null;
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
  },

  /**
   * Discover available models from provider's API (OpenAI, Gemini, OpenRouter, DeepSeek)
   */
  discoverModels: async (providerKey: string): Promise<DiscoverModelsResponse> => {
    return await apiFetch<DiscoverModelsResponse>(`/admin/ai/providers/discover-models/${providerKey}`);
  },

  /**
   * Add multiple models to a provider in batch
   */
  addBatchModels: async (providerId: string, models: DiscoveredModel[]): Promise<{ success: boolean; message: string }> => {
    return await apiRequest<{ success: boolean; message: string }>(`/admin/ai/providers/${providerId}/batch-models`, 'POST', models);
  },

  // ===== API Key Management =====

  /**
   * Save encrypted API key for a provider
   */
  saveApiKey: async (providerId: string, apiKey: string): Promise<void> => {
    await apiRequest(`/admin/ai/providers/${providerId}/credentials`, 'POST', { apiKey });
  },

  /**
   * Get credential status (configured + last 4 digits)
   */
  getCredentialStatus: async (providerId: string): Promise<CredentialStatusDto> => {
    return await apiFetch<CredentialStatusDto>(`/admin/ai/providers/${providerId}/credentials/status`);
  },

  /**
   * Test connection with provider using configured API key
   */
  testConnection: async (providerId: string): Promise<TestConnectionResultDto> => {
    return await apiRequest<TestConnectionResultDto>(`/admin/ai/providers/${providerId}/test-connection`, 'POST');
  }
};
