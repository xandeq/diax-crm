'use client';

import { AiModel } from '@/services/aiCatalog';

interface ModelCardProps {
  model: AiModel;
  selected: boolean;
  providerKey: string;
  onClick: () => void;
}

/**
 * Card component for displaying and selecting an AI model.
 * Shows model name, cost hints (if available), and free tier indicator.
 */
export function ModelCard({ model, selected, providerKey, onClick }: ModelCardProps) {
  const free = isModelFree(providerKey, model.modelKey);

  return (
    <button
      type="button"
      onClick={onClick}
      className={`w-full text-left px-3 py-2.5 rounded-xl border transition-all duration-150 ${
        selected
          ? 'bg-violet-500/15 border-violet-500/40 ring-1 ring-violet-500/30'
          : 'bg-white/5 border-white/10 hover:bg-white/10 hover:border-white/20'
      }`}
    >
      <div className="flex items-center justify-between gap-2">
        <span className={`text-xs font-medium leading-tight ${selected ? 'text-violet-200' : 'text-white/80'}`}>
          {model.displayName}
        </span>
        {free && (
          <span className="shrink-0 px-1.5 py-0.5 rounded text-[9px] font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
            GRÁTIS
          </span>
        )}
      </div>
      {model.inputCostHint != null && (
        <p className="text-[10px] text-white/30 mt-0.5">
          ${model.inputCostHint}/1k tokens
        </p>
      )}
    </button>
  );
}

/**
 * Checks if a model is free based on provider key and model key.
 * HuggingFace models are typically free-tier.
 */
function isModelFree(providerKey: string, modelKey: string): boolean {
  const pk = providerKey.toLowerCase();
  const mk = modelKey.toLowerCase();
  return pk === 'huggingface' || pk === 'hf' || mk.endsWith(':free') || mk.includes('/free/');
}
