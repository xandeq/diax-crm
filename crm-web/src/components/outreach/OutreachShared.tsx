'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Flame, Snowflake, Thermometer } from 'lucide-react';
import React from 'react';

// ─── Types ───────────────────────────────────────────────────────────────────

export type ActiveTab = 'dashboard' | 'configuracao' | 'templates' | 'leads' | 'whatsapp';

// ─── Segment Badge ────────────────────────────────────────────────────────────

export function getSegmentBadge(segment: string) {
  const lower = segment.toLowerCase();
  if (lower === 'hot') {
    return (
      <Badge className="bg-red-100 text-red-700 hover:bg-red-100 flex items-center gap-1 w-fit">
        <Flame className="h-3 w-3" />
        Quente
      </Badge>
    );
  }
  if (lower === 'warm') {
    return (
      <Badge className="bg-orange-100 text-orange-700 hover:bg-orange-100 flex items-center gap-1 w-fit">
        <Thermometer className="h-3 w-3" />
        Morno
      </Badge>
    );
  }
  return (
    <Badge className="bg-blue-100 text-blue-700 hover:bg-blue-100 flex items-center gap-1 w-fit">
      <Snowflake className="h-3 w-3" />
      Frio
    </Badge>
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
      className={[
        'relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
        checked ? 'bg-slate-900' : 'bg-slate-200',
      ].join(' ')}
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
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-slate-600">{title}</CardTitle>
        <span className={`h-8 w-8 rounded-md flex items-center justify-center ${accent ?? 'bg-slate-100'}`}>
          {icon}
        </span>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-slate-900">{value}</div>
        {description && (
          <p className="text-xs text-slate-500 mt-1">{description}</p>
        )}
      </CardContent>
    </Card>
  );
}

// ─── Status Indicator ─────────────────────────────────────────────────────────

export function StatusIndicator({ enabled, label }: { enabled: boolean; label: string }) {
  return (
    <div className="flex items-center gap-2">
      <span
        className={`inline-block h-2 w-2 rounded-full ${enabled ? 'bg-green-500' : 'bg-slate-300'}`}
      />
      <span className="text-sm text-slate-700">{label}</span>
      <span className={`text-xs font-medium ${enabled ? 'text-green-600' : 'text-slate-400'}`}>
        {enabled ? 'Ativo' : 'Inativo'}
      </span>
    </div>
  );
}
