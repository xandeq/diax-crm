'use client';

import { AiProvider } from '@/services/aiCatalog';

interface ProviderBadgeProps {
  provider: AiProvider;
  selected: boolean;
  onClick: () => void;
}

/**
 * Badge component for displaying and selecting an AI provider.
 * Used primarily in the image/video generation provider selector.
 */
export function ProviderBadge({ provider, selected, onClick }: ProviderBadgeProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-all duration-150 ${
        selected
          ? 'bg-violet-500/20 border-violet-500/50 text-violet-300 ring-1 ring-violet-500/30'
          : 'bg-white/5 border-white/10 text-white/60 hover:bg-white/10 hover:text-white/80 hover:border-white/20'
      }`}
    >
      {provider.name}
    </button>
  );
}
