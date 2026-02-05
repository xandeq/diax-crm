import { apiFetch } from './api';

export interface AiModel {
  id: string;
  modelKey: string;
  displayName: string;
  isEnabled: boolean;
  isDiscovered: boolean;
  inputCostHint?: number;
  outputCostHint?: number;
  maxTokensHint?: number;
}

export interface AiProvider {
  id: string;
  key: string;
  name: string;
  isEnabled: boolean;
  supportsListModels: boolean;
  baseUrl?: string;
  models: AiModel[];
}

export interface AiCatalogResponse {
  providers: AiProvider[];
}

export async function getAiCatalog(): Promise<AiProvider[]> {
  try {
    const response = await apiFetch<AiCatalogResponse>('/ai/catalog');
    return response.providers;
  } catch (error) {
    console.error('Error fetching AI catalog:', error);
    return [];
  }
}
