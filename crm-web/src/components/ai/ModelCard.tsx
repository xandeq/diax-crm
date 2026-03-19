'use client';

import { AiModel, ModelAvailabilityStatus } from '@/services/aiCatalog';

interface ModelCardProps {
  model: AiModel;
  selected: boolean;
  providerKey: string;
  onClick: () => void;
}

/**
 * Card component for displaying and selecting an AI model.
 * Shows model name, free tier indicator, cost hints, and availability status badge.
 *
 * POLICY: Models are ALWAYS shown regardless of availability status.
 * The badge informs the user about runtime health without hiding the model.
 * Only explicitly disabled models (IsEnabled=false) are filtered server-side.
 */
export function ModelCard({ model, selected, providerKey, onClick }: ModelCardProps) {
  const free = isModelFree(providerKey, model.modelKey);
  const status = model.availabilityStatus ?? 'Available';
  const statusInfo = getStatusBadge(status);
  const hasIssue = status !== 'Available';

  return (
    <button
      type="button"
      onClick={onClick}
      title={hasIssue ? statusInfo.tooltip : undefined}
      className={`w-full text-left px-3 py-2.5 rounded-xl border transition-all duration-150 ${
        selected
          ? 'bg-violet-500/15 border-violet-500/40 ring-1 ring-violet-500/30'
          : hasIssue
          ? 'bg-white/3 border-white/8 hover:bg-white/8 hover:border-white/15'
          : 'bg-white/5 border-white/10 hover:bg-white/10 hover:border-white/20'
      }`}
    >
      <div className="flex items-center justify-between gap-2">
        <span
          className={`text-xs font-medium leading-tight ${
            selected ? 'text-violet-200' : hasIssue ? 'text-white/50' : 'text-white/80'
          }`}
        >
          {model.displayName}
        </span>

        <div className="flex items-center gap-1 shrink-0">
          {free && (
            <span className="px-1.5 py-0.5 rounded text-[9px] font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
              GRÁTIS
            </span>
          )}
          {hasIssue && statusInfo.label && (
            <span className={`px-1.5 py-0.5 rounded text-[9px] font-bold border ${statusInfo.className}`}>
              {statusInfo.label}
            </span>
          )}
        </div>
      </div>

      {model.inputCostHint != null && (
        <p className="text-[10px] text-white/30 mt-0.5">${model.inputCostHint}/1k tokens</p>
      )}

      {hasIssue && model.lastFailureCategory && (
        <p className="text-[10px] text-white/25 mt-0.5 leading-tight">
          {getFriendlyFailureReason(model.lastFailureCategory)}
        </p>
      )}
    </button>
  );
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

interface StatusBadge {
  label: string;
  className: string;
  tooltip: string;
}

function getStatusBadge(status: ModelAvailabilityStatus): StatusBadge {
  switch (status) {
    case 'Degraded':
      return {
        label: 'INSTÁVEL',
        className: 'bg-yellow-500/15 text-yellow-400 border-yellow-500/30',
        tooltip: 'Este modelo apresentou falhas recentes, mas pode estar funcionando. Tente usar.',
      };
    case 'Unavailable':
      return {
        label: 'INDISPONÍVEL',
        className: 'bg-red-500/15 text-red-400 border-red-500/30',
        tooltip: 'Este modelo apresentou múltiplas falhas consecutivas. Pode estar temporariamente indisponível.',
      };
    case 'NoCredits':
      return {
        label: 'SEM CRÉDITO',
        className: 'bg-orange-500/15 text-orange-400 border-orange-500/30',
        tooltip: 'Quota ou crédito esgotado. Aguarde a renovação ou recarregue créditos.',
      };
    case 'RateLimited':
      return {
        label: 'LIMITE',
        className: 'bg-amber-500/15 text-amber-400 border-amber-500/30',
        tooltip: 'Limite de requisições atingido. Aguarde alguns instantes antes de tentar novamente.',
      };
    case 'ConfigError':
      return {
        label: 'CONFIG',
        className: 'bg-purple-500/15 text-purple-400 border-purple-500/30',
        tooltip: 'Erro de configuração ou autenticação. Verifique as credenciais em Administração > AI.',
      };
    case 'Available':
    default:
      return { label: '', className: '', tooltip: '' };
  }
}

function getFriendlyFailureReason(category: string): string {
  const map: Record<string, string> = {
    QuotaExhausted: 'Crédito esgotado',
    RateLimit: 'Limite de requisições',
    AuthFailed: 'Falha de autenticação',
    ModelNotFound: 'Modelo não encontrado no provider',
    InvalidRequest: 'Requisição inválida',
    ProviderUnavailable: 'Provider indisponível',
    Timeout: 'Timeout na requisição',
    ConfigurationMissing: 'API Key não configurada',
    CapabilityMismatch: 'Modelo não suporta esta operação',
  };
  return map[category] ?? category;
}

/**
 * Checks if a model is free based on provider key and model key.
 */
function isModelFree(providerKey: string, modelKey: string): boolean {
  const pk = providerKey.toLowerCase();
  const mk = modelKey.toLowerCase();
  return pk === 'huggingface' || pk === 'hf' || mk.endsWith(':free') || mk.includes('/free/');
}
