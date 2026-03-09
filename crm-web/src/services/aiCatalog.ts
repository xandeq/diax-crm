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
  supportsImage: boolean;
  supportsText: boolean;
  supportsVideo: boolean;
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
    console.warn('[aiCatalog] ⚠️ No access token available, skipping API call');
    return [];
  }

  try {
    console.log('[aiCatalog] 🔄 Fetching AI catalog...');

    const response = await apiFetch<AiCatalogResponse>('/ai/catalog');

    console.log('[aiCatalog] ✅ Response received:', {
      providersCount: response.providers?.length ?? 0,
      providers: response.providers?.map(p => p.name) ?? []
    });

    if (!response.providers || response.providers.length === 0) {
      console.warn('[aiCatalog] ⚠️ Catalog is empty. This may indicate missing API keys in backend configuration.');
      console.warn('[aiCatalog] 💡 Check backend logs for: "No AI provider API keys configured"');
    }

    return response.providers ?? [];
  } catch (error) {
    // Se for erro 401, logar detalhes mas NÃO propagar
    if (error instanceof ApiError && error.status === 401) {
      console.error('[aiCatalog] ❌ 401 Unauthorized - Token may be invalid or endpoint misconfigured:', {
        status: error.status,
        message: error.message
      });
      return [];
    }

    // Para outros erros, log e retorna vazio
    console.error('[aiCatalog] ❌ Error fetching catalog:', error);
    return [];
  }
}
