import { ApiError, apiFetch, getAccessToken } from './api';

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
  // Verificar se há token antes de fazer a requisição
  const token = getAccessToken();
  if (!token) {
    console.warn('[aiCatalog] No access token available, skipping API call');
    // Retornar array vazio em vez de lançar erro para não disparar logout
    return [];
  }

  try {
    const response = await apiFetch<AiCatalogResponse>('/ai/catalog');
    return response.providers;
  } catch (error) {
    // Se for erro 401, logar detalhes mas NÃO propagar
    // O AuthGuard cuidará do redirect se o token realmente estiver inválido
    if (error instanceof ApiError && error.status === 401) {
      console.error('[aiCatalog] 401 Unauthorized - Token may be invalid or endpoint misconfigured:', {
        status: error.status,
        message: error.message
      });
      // Retornar vazio para permitir que a página carregue
      return [];
    }

    // Para outros erros, log e retorna vazio
    console.error('Error fetching AI catalog:', error);
    return [];
  }
}
