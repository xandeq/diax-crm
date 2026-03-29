const FREE_MODEL_KEYS = new Set([
  'black-forest-labs/flux.1-schnell',
  'stabilityai/stable-diffusion-xl-base-1.0',
  'black-forest-labs/flux-1-schnell-fp8',
  'stability-ai/stable-diffusion-xl',
]);

const GATED_MODEL_KEYS = new Set([
  'stabilityai/stable-diffusion-3-medium-diffusers',
]);

export function isModelFree(providerKey: string, modelKey: string): boolean {
  const normalizedProviderKey = providerKey.toLowerCase();
  const normalizedModelKey = modelKey.toLowerCase();

  if (normalizedModelKey.endsWith(':free') || normalizedModelKey.includes('/free/')) {
    return true;
  }

  if (normalizedProviderKey === 'huggingface' || normalizedProviderKey === 'hf') {
    return FREE_MODEL_KEYS.has(normalizedModelKey);
  }

  return false;
}

export function requiresLicenseAcceptance(providerKey: string, modelKey: string): boolean {
  const normalizedProviderKey = providerKey.toLowerCase();
  const normalizedModelKey = modelKey.toLowerCase();

  if (normalizedProviderKey !== 'huggingface' && normalizedProviderKey !== 'hf') {
    return false;
  }

  return GATED_MODEL_KEYS.has(normalizedModelKey);
}
