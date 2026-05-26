import React from 'react';
import { AlertCircle, RotateCw } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

export interface QuotaStatusDto {
  providerName: string;
  quotaType: string; // "Generations", "Credits", "Minutes", "Cost"
  dailyLimit: number | null;
  currentUsage: number;
  remaining: number;
  percentageUsed: number;
  resetAt: string; // ISO date string
  timeUntilReset: {
    hours: number;
    minutes: number;
    seconds: number;
  };
  isExhausted: boolean;
}

interface QuotaStatusCardProps {
  quota: QuotaStatusDto | null;
  isLoading?: boolean;
  onResetClick?: () => void;
}

export function QuotaStatusCard({ quota, isLoading = false, onResetClick }: QuotaStatusCardProps) {
  if (!quota) return null;

  const percentUsed = Math.min(quota.percentageUsed, 100);
  const resetTime = new Date(quota.resetAt);
  const resetTimeFormatted = resetTime.toLocaleTimeString('pt-BR', {
    hour: '2-digit',
    minute: '2-digit',
  });

  const getProgressColor = (): string => {
    if (quota.isExhausted) return 'bg-red-600';
    if (percentUsed > 75) return 'bg-orange-500';
    if (percentUsed > 50) return 'bg-yellow-500';
    return 'bg-green-500';
  };

  const getStatusBadgeVariant = (): 'default' | 'secondary' | 'destructive' | 'outline' => {
    if (quota.isExhausted) return 'destructive';
    if (percentUsed > 75) return 'secondary';
    return 'default';
  };

  const STAT_PILL = {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(255,255,255,0.09)',
    borderRadius: '0.5rem',
    padding: '0.5rem',
    textAlign: 'center' as const,
  };

  return (
    <div style={{ background: 'rgba(99,102,241,0.08)', border: '1px solid rgba(99,102,241,0.2)', borderRadius: '0.75rem', padding: '1.5rem' }}>
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold" style={{ color: '#F9FAFB' }}>
          Quota de Uso — {quota.providerName}
        </h3>
        <Badge variant={getStatusBadgeVariant()}>
          {quota.remaining}/{quota.dailyLimit} {quota.quotaType === 'Credits' ? 'créditos' : 'gerações'}
        </Badge>
      </div>

      <div className="space-y-4">
        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span style={{ color: '#D1D5DB' }} className="font-medium">Uso diário</span>
            <span style={{ color: '#9CA3AF' }}>{percentUsed.toFixed(0)}%</span>
          </div>
          <div className="h-3 w-full rounded-full overflow-hidden" style={{ background: 'rgba(255,255,255,0.1)' }}>
            <div
              className={`h-full rounded-full transition-all duration-500 ${getProgressColor()}`}
              style={{ width: `${percentUsed}%` }}
            />
          </div>
        </div>

        {/* Usage Details */}
        <div className="grid grid-cols-3 gap-3 text-sm">
          <div style={STAT_PILL}>
            <div style={{ color: '#9CA3AF' }} className="font-medium">Usado</div>
            <div className="text-lg font-bold" style={{ color: '#F9FAFB' }}>{quota.currentUsage}</div>
          </div>
          <div style={STAT_PILL}>
            <div style={{ color: '#9CA3AF' }} className="font-medium">Limite</div>
            <div className="text-lg font-bold" style={{ color: '#F9FAFB' }}>{quota.dailyLimit}</div>
          </div>
          <div style={STAT_PILL}>
            <div style={{ color: '#9CA3AF' }} className="font-medium">Restante</div>
            <div className={`text-lg font-bold ${quota.isExhausted ? 'text-red-400' : 'text-emerald-400'}`}>
              {quota.remaining}
            </div>
          </div>
        </div>

        {/* Reset Time */}
        <div className="rounded-lg p-3 text-sm" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.07)' }}>
          <div style={{ color: '#9CA3AF' }} className="font-medium mb-1">Próximo reset</div>
          <div className="flex items-center justify-between">
            <span style={{ color: '#F9FAFB' }} className="font-semibold">{resetTimeFormatted} (UTC)</span>
            <span style={{ color: '#9CA3AF' }}>
              {quota.timeUntilReset.hours}h {quota.timeUntilReset.minutes}m
            </span>
          </div>
        </div>

        {/* Exhausted Alert */}
        {quota.isExhausted && (
          <div className="rounded-lg p-3 flex gap-3 mt-3" style={{ background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.25)' }}>
            <AlertCircle className="h-4 w-4 text-red-400 shrink-0 mt-0.5" />
            <div>
              <div className="font-semibold text-sm text-red-400">Limite diário atingido</div>
              <div className="text-xs mt-0.5" style={{ color: '#9CA3AF' }}>
                Você alcançou o limite de {quota.dailyLimit} {quota.quotaType === 'Credits' ? 'créditos' : 'gerações'} por dia. Tente novamente após o reset.
              </div>
            </div>
          </div>
        )}

        {/* Reset Button (for admins) */}
        {onResetClick && (
          <button
            onClick={onResetClick}
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            style={{ background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.12)', color: '#D1D5DB' }}
          >
            <RotateCw className="h-4 w-4" />
            Reset Manual (Admin)
          </button>
        )}
      </div>
    </div>
  );
}
