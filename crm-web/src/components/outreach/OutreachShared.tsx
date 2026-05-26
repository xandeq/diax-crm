'use client';

import { Flame, Snowflake, Thermometer } from 'lucide-react';
import React from 'react';

// ─── Types ───────────────────────────────────────────────────────────────────

export type ActiveTab = 'dashboard' | 'configuracao' | 'templates' | 'leads' | 'whatsapp';

// ─── Segment Badge ────────────────────────────────────────────────────────────

export function getSegmentBadge(segment: string) {
  const lower = segment.toLowerCase();
  if (lower === 'hot') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium" style={{ background: 'rgba(239,68,68,0.12)', color: '#f87171', border: '1px solid rgba(239,68,68,0.2)' }}>
        <Flame className="h-3 w-3" />
        Quente
      </span>
    );
  }
  if (lower === 'warm') {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium" style={{ background: 'rgba(251,146,60,0.12)', color: '#fb923c', border: '1px solid rgba(251,146,60,0.2)' }}>
        <Thermometer className="h-3 w-3" />
        Morno
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium" style={{ background: 'rgba(96,165,250,0.12)', color: '#60a5fa', border: '1px solid rgba(96,165,250,0.2)' }}>
      <Snowflake className="h-3 w-3" />
      Frio
    </span>
  );
}

// ─── Toggle Switch ────────────────────────────────────────────────────────────

export interface ToggleSwitchProps {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  id?: string;
}

export function ToggleSwitch({ checked, onCheckedChange, disabled, id }: ToggleSwitchProps) {
  return (
    <button
      id={id}
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onCheckedChange(!checked)}
      className="relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
      style={{ background: checked ? '#10B981' : 'rgba(255,255,255,0.12)' }}
    >
      <span
        className={[
          'pointer-events-none block h-5 w-5 rounded-full bg-white shadow-lg ring-0 transition-transform',
          checked ? 'translate-x-5' : 'translate-x-0',
        ].join(' ')}
      />
    </button>
  );
}

// ─── Stat Card ────────────────────────────────────────────────────────────────

export interface StatCardProps {
  title: string;
  value: number | string;
  icon: React.ReactNode;
  description?: string;
  accent?: string;
}

export function StatCard({ title, value, icon, description, accent }: StatCardProps) {
  return (
    <div className="rounded-xl p-4" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
      <div className="flex flex-row items-center justify-between pb-2">
        <span className="text-sm font-medium" style={{ color: '#9CA3AF' }}>{title}</span>
        <span className={`h-8 w-8 rounded-md flex items-center justify-center ${accent ?? ''}`} style={!accent ? { background: 'rgba(255,255,255,0.08)' } : undefined}>
          {icon}
        </span>
      </div>
      <div className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>{value}</div>
      {description && (
        <p className="text-xs mt-1" style={{ color: '#6B7280' }}>{description}</p>
      )}
    </div>
  );
}

// ─── Status Indicator ─────────────────────────────────────────────────────────

export function StatusIndicator({ enabled, label }: { enabled: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span
        className="inline-block h-2 w-2 rounded-full"
        style={{ background: enabled ? '#10B981' : 'rgba(255,255,255,0.2)' }}
      />
      <span className="text-sm" style={{ color: '#D1D5DB' }}>{label}</span>
      <span className="text-xs font-medium" style={{ color: enabled ? '#34d399' : '#6B7280' }}>
        {enabled ? 'Ativo' : 'Inativo'}
      </span>
    </div>
  );
}
