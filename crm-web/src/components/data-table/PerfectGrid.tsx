'use client';

import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  ChevronLeft,
  ChevronRight,
  Mail,
  MessageCircle,
  SearchX,
} from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';

// ── Types ────────────────────────────────────────────────────────────────────

export interface GridColumn<T> {
  id: string;
  header: string;
  cell: (row: T) => React.ReactNode;
  sortable?: boolean;
  className?: string;
  headerClassName?: string;
}

export interface PerfectGridProps<T> {
  columns: GridColumn<T>[];
  data: T[];
  loading: boolean;

  // Server-side pagination
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;

  // Server-side sorting (controlled)
  sortColumn?: string | null;
  sortDirection?: 'asc' | 'desc';
  onSort?: (columnId: string, direction: 'asc' | 'desc') => void;

  // Selection (controlled)
  selectable?: boolean;
  selectedRows?: T[];
  onSelectionChange?: (rows: T[]) => void;

  // Row interaction
  onRowClick?: (row: T) => void;

  // Row identity
  getRowId: (row: T) => string;

  // Empty state
  emptyIcon?: React.ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;

  // Label for pagination text
  itemLabel?: string;
}

// ── Hooks ────────────────────────────────────────────────────────────────────

export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);
  useEffect(() => {
    const handler = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(handler);
  }, [value, delay]);
  return debouncedValue;
}

// ── Avatar ───────────────────────────────────────────────────────────────────

const AVATAR_COLORS = [
  'bg-blue-500',
  'bg-emerald-500',
  'bg-violet-500',
  'bg-amber-500',
  'bg-rose-500',
  'bg-cyan-500',
  'bg-indigo-500',
  'bg-teal-500',
  'bg-pink-500',
  'bg-orange-500',
];

function hashStr(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  return Math.abs(hash);
}

export function Avatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' }) {
  const initials = (() => {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  })();
  const color = AVATAR_COLORS[hashStr(name) % AVATAR_COLORS.length];
  const sizeClass = size === 'sm' ? 'h-7 w-7 text-[10px]' : 'h-9 w-9 text-xs';

  return (
    <div
      className={`${sizeClass} ${color} rounded-full flex items-center justify-center text-white font-semibold shrink-0 select-none`}
    >
      {initials}
    </div>
  );
}

// ── Status Badge ─────────────────────────────────────────────────────────────

const STATUS_CONFIGS: Record<number, { bg: string; text: string; dot: string; border: string; label: string }> = {
  0: { bg: 'bg-sky-500/10', text: 'text-sky-400', dot: 'bg-sky-400', border: 'border-sky-500/20', label: 'Lead' },
  1: { bg: 'bg-amber-500/10', text: 'text-amber-400', dot: 'bg-amber-400', border: 'border-amber-500/20', label: 'Contatado' },
  2: { bg: 'bg-[#00D4AA]/10', text: 'text-[#00D4AA]', dot: 'bg-[#00D4AA]', border: 'border-[#00D4AA]/20', label: 'Qualificado' },
  3: { bg: 'bg-orange-500/10', text: 'text-orange-400', dot: 'bg-orange-400', border: 'border-orange-500/20', label: 'Negociando' },
  4: { bg: 'bg-teal-500/10', text: 'text-teal-400', dot: 'bg-teal-400', border: 'border-teal-500/20', label: 'Cliente' },
  5: { bg: 'bg-zinc-500/10', text: 'text-zinc-400', dot: 'bg-zinc-400', border: 'border-zinc-500/20', label: 'Inativo' },
  6: { bg: 'bg-rose-500/10', text: 'text-rose-400', dot: 'bg-rose-400', border: 'border-rose-500/20', label: 'Churn' },
};

export function StatusBadge({ status }: { status: number }) {
  const config = STATUS_CONFIGS[status];
  if (!config) return <span className="text-zinc-500">-</span>;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${config.bg} ${config.text} ${config.border}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${config.dot} animate-pulse`} />
      {config.label}
    </span>
  );
}

// ── Filter Chip ──────────────────────────────────────────────────────────────

export function FilterChip({
  label,
  active,
  onClick,
  count,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
  count?: number;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`px-3.5 py-1.5 rounded-full text-sm font-medium transition-all duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-[#00D4AA] ${
        active
          ? 'bg-[#00D4AA]/25 text-[#00D4AA] border border-[#00D4AA]/30 shadow-[0_0_12px_rgba(0,212,170,0.15)]'
          : 'bg-white/5 border border-white/10 text-zinc-400 hover:bg-white/8 hover:border-white/20 hover:text-zinc-200'
      }`}
    >
      {label}
      {count !== undefined && (
        <span className={`ml-1.5 text-xs ${active ? 'text-[#00D4AA]/70' : 'text-zinc-500'}`}>
          {count}
        </span>
      )}
    </button>
  );
}

// ── Segment Badge ────────────────────────────────────────────────────────────

const SEGMENT_CONFIGS: Record<number, { bg: string; text: string; dot: string; border: string; label: string }> = {
  2: { bg: 'bg-rose-500/10', text: 'text-rose-400', dot: 'bg-rose-400', border: 'border-rose-500/20', label: 'Hot' },
  1: { bg: 'bg-orange-500/10', text: 'text-orange-400', dot: 'bg-orange-400', border: 'border-orange-500/20', label: 'Warm' },
  0: { bg: 'bg-sky-500/10', text: 'text-sky-400', dot: 'bg-sky-400', border: 'border-sky-500/20', label: 'Cold' },
};

export function SegmentBadge({ segment }: { segment?: number | null }) {
  if (segment === undefined || segment === null) {
    return <span className="text-xs text-zinc-500 italic">Não segmentado</span>;
  }
  const config = SEGMENT_CONFIGS[segment];
  if (!config) return <span className="text-xs text-zinc-500">–</span>;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${config.bg} ${config.text} ${config.border}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${config.dot}`} />
      {config.label}
    </span>
  );
}

// ── Channel Icons ────────────────────────────────────────────────────────────

export function ChannelIcons({
  hasEmail,
  hasWhatsApp,
  emailOptOut,
  whatsAppOptOut,
}: {
  hasEmail?: boolean;
  hasWhatsApp?: boolean;
  emailOptOut?: boolean;
  whatsAppOptOut?: boolean;
}) {
  const showEmail = hasEmail && !emailOptOut;
  const showWhatsApp = hasWhatsApp && !whatsAppOptOut;

  if (!showEmail && !showWhatsApp) {
    return <span className="text-xs text-zinc-500 italic">Nenhum</span>;
  }

  return (
    <div className="flex items-center gap-1.5">
      {showEmail && (
        <span
          className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-blue-500/10 text-blue-400 border border-blue-500/20"
          title={emailOptOut ? 'Email (opt-out)' : 'Email disponível'}
        >
          <Mail className="h-3.5 w-3.5" />
        </span>
      )}
      {showWhatsApp && (
        <span
          className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-[#00D4AA]/10 text-[#00D4AA] border border-[#00D4AA]/20"
          title={whatsAppOptOut ? 'WhatsApp (opt-out)' : 'WhatsApp disponível'}
        >
          <MessageCircle className="h-3.5 w-3.5" />
        </span>
      )}
    </div>
  );
}

// ── Source Label ──────────────────────────────────────────────────────────────

const SOURCE_LABELS: Record<number, { label: string; color: string }> = {
  0: { label: 'Não especificado', color: 'text-zinc-500' },
  1: { label: 'Manual', color: 'text-zinc-400' },
  2: { label: 'Website', color: 'text-indigo-400' },
  3: { label: 'Indicação', color: 'text-emerald-400' },
  4: { label: 'Scraping', color: 'text-amber-400' },
  5: { label: 'Redes Sociais', color: 'text-pink-400' },
  6: { label: 'E-mail Marketing', color: 'text-sky-400' },
  7: { label: 'Anúncios Pagos', color: 'text-violet-400' },
  8: { label: 'Evento', color: 'text-cyan-400' },
  9: { label: 'Parceria', color: 'text-teal-400' },
  10: { label: 'Importação', color: 'text-orange-400' },
  11: { label: 'Google Maps', color: 'text-rose-400' },
};

export function SourceLabel({ source, sourceDescription }: { source?: number; sourceDescription?: string }) {
  if (source === undefined || source === null) {
    return <span className="text-xs text-zinc-500">–</span>;
  }
  const config = SOURCE_LABELS[source];
  const label = sourceDescription || config?.label || 'Desconhecido';
  const color = config?.color || 'text-zinc-400';
  return (
    <span className={`text-sm font-medium ${color}`}>
      {label}
    </span>
  );
}

// ── Source / Segment Filter Configs (for pages to use) ───────────────────────

export const SOURCE_FILTER_OPTIONS = [
  { value: undefined as number | undefined, label: 'Todas' },
  { value: 1, label: 'Manual' },
  { value: 2, label: 'Website' },
  { value: 3, label: 'Indicação' },
  { value: 4, label: 'Scraping' },
  { value: 5, label: 'Redes Sociais' },
  { value: 6, label: 'E-mail Mktg' },
  { value: 7, label: 'Anúncios' },
  { value: 8, label: 'Evento' },
  { value: 9, label: 'Parceria' },
  { value: 10, label: 'Importação' },
  { value: 11, label: 'Google Maps' },
];

export const SEGMENT_FILTER_OPTIONS = [
  { value: undefined as number | undefined, label: 'Todos' },
  { value: 2, label: 'Hot' },
  { value: 1, label: 'Warm' },
  { value: 0, label: 'Cold' },
];

// ── Skeleton Row ─────────────────────────────────────────────────────────────

function SkeletonRow({
  columns,
  hasCheckbox,
  index,
}: {
  columns: GridColumn<any>[];
  hasCheckbox: boolean;
  index: number;
}) {
  return (
    <tr className="border-b border-white/5">
      {hasCheckbox && (
        <td className="px-5 py-4 w-12" data-checkbox="true">
          <div className="h-4 w-4 rounded shimmer-bg" style={{ animationDelay: `${index * 80}ms` }} />
        </td>
      )}
      {columns.map((col, j) => (
        <td key={col.id} className="px-5 py-4" data-label={col.header}>
          {j === 0 ? (
            <div className="flex items-center gap-3 justify-end md:justify-start">
              <div
                className="h-9 w-9 rounded-full shimmer-bg shrink-0"
                style={{ animationDelay: `${index * 80}ms` }}
              />
              <div className="space-y-1.5 flex-1 text-right md:text-left">
                <div
                  className="h-3.5 rounded shimmer-bg w-24 md:w-32 ml-auto md:ml-0"
                  style={{ animationDelay: `${index * 80}ms` }}
                />
                <div
                  className="h-3 rounded shimmer-bg w-16 md:w-20 ml-auto md:ml-0"
                  style={{ animationDelay: `${index * 80 + 40}ms` }}
                />
              </div>
            </div>
          ) : (
            <div
              className="h-3.5 rounded shimmer-bg w-16 md:w-24 ml-auto md:ml-0"
              style={{
                animationDelay: `${(index * columns.length + j) * 40}ms`,
              }}
            />
          )}
        </td>
      ))}
    </tr>
  );
}

// ── Empty State ──────────────────────────────────────────────────────────────

function EmptyState({
  columnCount,
  icon,
  title,
  description,
}: {
  columnCount: number;
  icon?: React.ReactNode;
  title?: string;
  description?: string;
}) {
  return (
    <tr>
      <td colSpan={columnCount} className="p-8">
        <div className="flex flex-col items-center justify-center text-center p-12 rounded-xl border border-dashed border-white/10 bg-white/[0.01] hover:bg-white/[0.02] transition-colors max-w-lg mx-auto my-4">
          <div className="p-4 rounded-full bg-[#00D4AA]/10 text-[#00D4AA] mb-4 shadow-[0_0_15px_rgba(0,212,170,0.1)]">
            {icon || <SearchX className="h-8 w-8" />}
          </div>
          <h3 className="text-lg font-semibold text-zinc-200 mb-1">
            {title || 'Nenhum resultado encontrado'}
          </h3>
          <p className="text-sm text-zinc-400 max-w-xs leading-relaxed">
            {description || 'Tente limpar os filtros ativos ou buscar por outro termo para encontrar o que procura.'}
          </p>
        </div>
      </td>
    </tr>
  );
}

// ── Sort Header ──────────────────────────────────────────────────────────────

function SortHeader({
  label,
  sortable,
  active,
  direction,
  onClick,
}: {
  label: string;
  sortable?: boolean;
  active: boolean;
  direction: 'asc' | 'desc';
  onClick: () => void;
}) {
  if (!sortable) {
    return (
      <span className="text-xs font-semibold uppercase tracking-wider text-zinc-400">
        {label}
      </span>
    );
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-zinc-400 hover:text-zinc-100 transition-colors group focus:outline-none focus-visible:ring-2 focus-visible:ring-[#00D4AA] rounded px-1 -mx-1"
    >
      {label}
      {active ? (
        direction === 'asc' ? (
          <ArrowUp className="h-3.5 w-3.5 text-[#00D4AA]" />
        ) : (
          <ArrowDown className="h-3.5 w-3.5 text-[#00D4AA]" />
        )
      ) : (
        <ArrowUpDown className="h-3.5 w-3.5 opacity-40 group-hover:opacity-100 transition-opacity" />
      )}
    </button>
  );
}

// ── Main Component ───────────────────────────────────────────────────────────

export function PerfectGrid<T>({
  columns,
  data,
  loading,
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  sortColumn,
  sortDirection = 'asc',
  onSort,
  selectable = false,
  selectedRows,
  onSelectionChange,
  onRowClick,
  getRowId,
  emptyIcon,
  emptyTitle,
  emptyDescription,
  itemLabel = 'registros',
}: PerfectGridProps<T>) {
  // Compute selectedIds from controlled prop
  const selectedIds = useMemo(() => {
    if (!selectedRows) return new Set<string>();
    return new Set(selectedRows.map(getRowId));
  }, [selectedRows, getRowId]);

  // Handle sort toggle — calls parent callback for server-side sorting
  const handleSort = (columnId: string) => {
    if (!onSort) return;
    if (sortColumn === columnId) {
      onSort(columnId, sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      onSort(columnId, 'asc');
    }
  };

  // Handle select all
  const handleSelectAll = (checked: boolean) => {
    if (!onSelectionChange) return;
    onSelectionChange(checked ? [...data] : []);
  };

  // Handle select single row
  const handleSelectRow = (row: T, checked: boolean) => {
    if (!onSelectionChange || !selectedRows) return;
    if (checked) {
      onSelectionChange([...selectedRows, row]);
    } else {
      const id = getRowId(row);
      onSelectionChange(selectedRows.filter((r) => getRowId(r) !== id));
    }
  };

  const allSelected = data.length > 0 && data.every((r) => selectedIds.has(getRowId(r)));
  const someSelected = data.some((r) => selectedIds.has(getRowId(r))) && !allSelected;
  const totalColCount = columns.length + (selectable ? 1 : 0);

  // Pagination info
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className="rounded-xl overflow-hidden border border-white/10 bg-white/[0.02] backdrop-blur-md">


      {/* Table */}
      <div className="overflow-x-auto">
        <table className="w-full responsive-table">
          <thead>
            <tr className="border-b border-white/10 bg-white/[0.03]">
              {selectable && (
                <th className="w-12 px-5 py-3.5">
                  <Checkbox
                    checked={allSelected || (someSelected && 'indeterminate')}
                    onCheckedChange={(v) => handleSelectAll(!!v)}
                    aria-label="Selecionar todos"
                  />
                </th>
              )}
              {columns.map((col) => (
                <th
                  key={col.id}
                  className={`px-5 py-3.5 text-left ${col.headerClassName || ''}`}
                >
                  <SortHeader
                    label={col.header}
                    sortable={col.sortable}
                    active={sortColumn === col.id}
                    direction={sortDirection}
                    onClick={() => handleSort(col.id)}
                  />
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              Array.from({ length: Math.min(pageSize, 8) }).map((_, i) => (
                <SkeletonRow
                  key={i}
                  columns={columns}
                  hasCheckbox={selectable}
                  index={i}
                />
              ))
            ) : data.length === 0 ? (
              <EmptyState
                columnCount={totalColCount}
                icon={emptyIcon}
                title={emptyTitle}
                description={emptyDescription}
              />
            ) : (
              data.map((row) => {
                const id = getRowId(row);
                const isSelected = selectedIds.has(id);
                return (
                  <tr
                    key={id}
                    onClick={() => onRowClick?.(row)}
                    data-selected={isSelected}
                    className={[
                      'transition-all duration-200 border-b border-white/5 hover:bg-white/[0.04]',
                      isSelected ? 'bg-[#00D4AA]/5' : '',
                      onRowClick ? 'cursor-pointer' : '',
                    ].join(' ')}
                  >
                    {selectable && (
                      <td
                        className="w-12 px-5 py-4"
                        data-checkbox="true"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <Checkbox
                          checked={isSelected}
                          onCheckedChange={(v) => handleSelectRow(row, !!v)}
                          aria-label="Selecionar"
                        />
                      </td>
                    )}
                    {columns.map((col) => (
                      <td
                        key={col.id}
                        data-label={col.header}
                        className={`px-5 py-4 ${col.className || ''}`}
                      >
                        {col.cell(row)}
                      </td>
                    ))}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {!loading && totalCount > 0 && (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-5 py-3.5 border-t border-white/[0.08] bg-white/[0.02]">
          <div className="flex items-center gap-4 flex-wrap">
            <p className="text-sm text-zinc-400">
              Mostrando{' '}
              <span className="font-semibold text-zinc-100">{from}</span> a{' '}
              <span className="font-semibold text-zinc-100">{to}</span> de{' '}
              <span className="font-semibold text-zinc-100">{totalCount}</span>{' '}
              {itemLabel}
            </p>
            {selectable && selectedRows && selectedRows.length > 0 && (
              <span className="text-sm text-[#00D4AA] font-medium">
                ({selectedRows.length} selecionado{selectedRows.length > 1 ? 's' : ''})
              </span>
            )}
            <div className="flex items-center gap-1.5">
              <span className="text-sm text-zinc-500">Exibir</span>
              <select
                value={pageSize}
                onChange={(e) => onPageSizeChange(Number(e.target.value))}
                className="h-8 rounded-md px-2 text-sm cursor-pointer focus:outline-none border border-white/10 bg-white/5 text-zinc-300 focus:border-[#00D4AA]/50"
              >
                <option value={10}>10</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
                <option value={200}>200</option>
              </select>
            </div>
          </div>
          <div className="flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(1)}
              disabled={page <= 1}
              className="h-9 px-2 border-white/10 text-zinc-300 hover:bg-white/5"
              title="Primeira página"
            >
              <ChevronLeft className="h-4 w-4" />
              <ChevronLeft className="h-4 w-4 -ml-2.5" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1}
              className="h-9 px-2 border-white/10 text-zinc-300 hover:bg-white/5"
              title="Anterior"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <div className="flex items-center gap-1.5 mx-1">
              <input
                type="number"
                min={1}
                max={totalPages}
                value={page}
                onChange={(e) => {
                  const val = parseInt(e.target.value, 10);
                  if (val >= 1 && val <= totalPages) onPageChange(val);
                }}
                className="h-8 w-14 rounded-md px-2 text-sm text-center tabular-nums focus:outline-none border border-white/10 bg-white/5 text-zinc-300 focus:border-[#00D4AA]/50 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
              />
              <span className="text-sm text-zinc-500">/ {totalPages}</span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages}
              className="h-9 px-2 border-white/10 text-zinc-300 hover:bg-white/5"
              title="Próximo"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(totalPages)}
              disabled={page >= totalPages}
              className="h-9 px-2 border-white/10 text-zinc-300 hover:bg-white/5"
              title="Última página"
            >
              <ChevronRight className="h-4 w-4" />
              <ChevronRight className="h-4 w-4 -ml-2.5" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
