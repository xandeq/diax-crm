import { useCallback, useEffect, useRef, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { getAiCatalog, AiProvider, AiModel } from '@/services/aiCatalog';

export interface UseAiCatalogOptions {
  /** Filter providers/models by capability: text, image, or video generation */
  filterCapability?: 'supportsText' | 'supportsImage' | 'supportsVideo';
  /** Auto-select the first provider on load (default: true) */
  autoSelect?: boolean;
}

export function useAiCatalog(options: UseAiCatalogOptions = {}) {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const [providers, setProviders] = useState<AiProvider[]>([]);
  const [selectedProvider, setSelectedProviderKey] = useState('');
  const [selectedModel, setSelectedModelKey] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const initialized = useRef(false);

  const { filterCapability, autoSelect = true } = options;

  useEffect(() => {
    // Only load once, and only when authenticated
    if (!isAuthenticated || initialized.current) return;
    initialized.current = true;

    const loadCatalog = async () => {
      try {
        setIsLoading(true);
        setError(null);

        const catalog = await getAiCatalog();

        // Filter by isEnabled
        let filtered = catalog.filter(p => p.isEnabled);

        // Apply capability filter if specified
        if (filterCapability) {
          filtered = filtered
            .map(p => ({
              ...p,
              models: p.models.filter(m => m.isEnabled && (m as any)[filterCapability])
            }))
            .filter(p => p.models.length > 0);
        }

        setProviders(filtered);

        // Auto-select first provider + model
        if (autoSelect && filtered.length > 0) {
          const firstProvider = filtered[0];
          setSelectedProviderKey(firstProvider.key);

          const firstModel = getPreferredModel(firstProvider.models, firstProvider.key);
          if (firstModel) {
            setSelectedModelKey(firstModel.modelKey);
          } else {
            setSelectedModelKey('');
          }
        }

        setIsLoading(false);
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load AI catalog';
        setError(message);
        setIsLoading(false);
      }
    };

    loadCatalog();
  }, [isAuthenticated, filterCapability, autoSelect]);

  // Handle provider change: auto-select first enabled model
  const handleSetSelectedProvider = useCallback(
    (key: string) => {
      setSelectedProviderKey(key);
      const provider = providers.find(p => p.key === key);
      const firstModel = provider ? getPreferredModel(provider.models, provider.key) : undefined;
      setSelectedModelKey(firstModel?.modelKey ?? '');
    },
    [providers]
  );

  // Get current provider and its models
  const currentProvider = providers.find(p => p.key === selectedProvider);
  const currentModels = (currentProvider?.models ?? []).filter(m => m.isEnabled);

  return {
    // State
    providers,
    selectedProvider,
    selectedModel,
    isLoading: isLoading || authLoading,
    error,
    isReady: !isLoading && !authLoading && isAuthenticated,

    // Setters
    setSelectedProvider: handleSetSelectedProvider,
    setSelectedModel: setSelectedModelKey,

    // Computed
    currentProvider,
    currentModels,
  };
}

function getPreferredModel(models: AiModel[], providerKey: string): AiModel | undefined {
  const enabledModels = models.filter(m => m.isEnabled);
  if (enabledModels.length === 0) return undefined;

  return [...enabledModels].sort((a, b) => scoreModel(b, providerKey) - scoreModel(a, providerKey))[0];
}

function scoreModel(model: AiModel, providerKey: string): number {
  let score = 0;
  const normalizedProviderKey = providerKey.toLowerCase();
  const normalizedModelKey = model.modelKey.toLowerCase();

  switch (model.availabilityStatus) {
    case 'Available':
      score += 100;
      break;
    case 'Degraded':
      score += 60;
      break;
    case 'RateLimited':
      score += 30;
      break;
    case 'NoCredits':
      score += 10;
      break;
    case 'ConfigError':
      score -= 40;
      break;
    case 'Unavailable':
      score -= 60;
      break;
  }

  if (normalizedProviderKey === 'huggingface') {
    if (normalizedModelKey === 'black-forest-labs/flux.1-schnell') {
      score += 40;
    }

    if (normalizedModelKey === 'stabilityai/stable-diffusion-3-medium-diffusers') {
      score -= 50;
    }
  }

  if (model.lastSuccessAt) {
    score += 5;
  }

  score -= model.consecutiveFailureCount * 10;

  return score;
}
