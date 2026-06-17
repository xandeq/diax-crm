'use client';

import * as React from 'react';
import { cn } from '@/lib/utils';

export type HealthStatus = 'healthy' | 'warning' | 'error' | 'critical' | 'online' | 'offline';

interface HealthBadgeProps {
  status: HealthStatus;
  label?: string;
  className?: string;
}

export function HealthBadge({ status, label, className }: HealthBadgeProps) {
  const isHealthy = status === 'healthy' || status === 'online';
  const isWarning = status === 'warning';
  const isCritical = status === 'error' || status === 'critical' || status === 'offline';

  const cfg = {
    colorClass: isHealthy 
      ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' 
      : isWarning 
        ? 'bg-amber-500/10 text-amber-400 border-amber-500/20' 
        : 'bg-red-500/10 text-red-400 border-red-500/20',
    dotClass: isHealthy 
      ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]' 
      : isWarning 
        ? 'bg-amber-400 shadow-[0_0_8px_#fbbf24]' 
        : 'bg-red-400 shadow-[0_0_8px_#f87171]',
    labelText: label ?? (
      isHealthy ? 'Operacional' : isWarning ? 'Degradado' : 'Inativo'
    ),
  };

  return (
    <span className={cn(
      "inline-flex items-center gap-1.5 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-full border",
      cfg.colorClass,
      className
    )}>
      <span className={cn("h-1.5 w-1.5 rounded-full shrink-0", cfg.dotClass)} />
      <span>{cfg.labelText}</span>
    </span>
  );
}
