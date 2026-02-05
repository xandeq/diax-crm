import { apiFetch, ApiError } from './api';

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
    // Se for erro 401, deixa propagar (o apiFetch já disparou auth:expired)
    if (error instanceof ApiError && error.status === 401) {
      throw error;
    }
    
    // Para outros erros, log e retorna vazio
    console.error('Error fetching AI catalog:', error);
    return [];
  }
}
