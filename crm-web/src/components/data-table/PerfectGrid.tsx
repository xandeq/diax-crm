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

const STATUS_CONFIGS: Record<number, { bg: string; text: string; dot: string; label: string }> = {
  0: { bg: 'bg-sky-50', text: 'text-sky-700', dot: 'bg-sky-500', label: 'Lead' },
  1: { bg: 'bg-amber-50', text: 'text-amber-700', dot: 'bg-amber-500', label: 'Contatado' },
  2: { bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500', label: 'Qualificado' },
  3: { bg: 'bg-orange-50', text: 'text-orange-700', dot: 'bg-orange-500', label: 'Negociando' },
  4: { bg: 'bg-green-50', text: 'text-green-700', dot: 'bg-green-500', label: 'Cliente' },
  5: { bg: 'bg-slate-100', text: 'text-slate-600', dot: 'bg-slate-400', label: 'Inativo' },
  6: { bg: 'bg-red-50', text: 'text-red-700', dot: 'bg-red-500', label: 'Churn' },
};

export function StatusBadge({ status }: { status: number }) {
  const config = STATUS_CONFIGS[status];
  if (!config) return <span className="text-slate-400">-</span>;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${config.bg} ${config.text}`}
    >
      <span className={`h-1.5 w-1.5 rounded-full ${config.dot}`} />
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
      className={`px-3.5 py-1.5 rounded-full text-sm font-medium transition-all ${
        active
          ? 'bg-slate-900 text-white shadow-sm'
          : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50 hover:border-slate-300'
      }`}
    >
      {label}
      {count !== undefined && (
        <span className={`ml-1.5 text-xs ${active ? 'text-slate-400' : 'text-slate-400'}`}>
          {count}
        </span>
      )}
    </button>
  );
}

// ── Segment Badge ────────────────────────────────────────────────────────────

const SEGMENT_CONFIGS: Record<number, { bg: string; text: string; dot: string; label: string }> = {
  2: { bg: 'bg-red-50', text: 'text-red-700', dot: 'bg-red-500', label: 'Hot' },
  1: { bg: 'bg-orange-50', text: 'text-orange-700', dot: 'bg-orange-500', label: 'Warm' },
  0: { bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500', label: 'Cold' },
};

export function SegmentBadge({ segment }: { segment?: number | null }) {
  if (segment === undefined || segment === null) {
    return <span className="text-xs text-slate-400 italic">Não segmentado</span>;
  }
  const config = SEGMENT_CONFIGS[segment];
  if (!config) return <span className="text-xs text-slate-400">–</span>;
  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${config.bg} ${config.text}`}
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
    return <span className="text-xs text-slate-400 italic">Nenhum</span>;
  }

  return (
    <div className="flex items-center gap-1.5">
      {showEmail && (
        <span
          className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-blue-50 text-blue-600"
          title={emailOptOut ? 'Email (opt-out)' : 'Email disponível'}
        >
          <Mail className="h-3.5 w-3.5" />
        </span>
      )}
      {showWhatsApp && (
        <span
          className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-green-50 text-green-600"
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
  0: { label: 'Não especificado', color: 'text-slate-400' },
  1: { label: 'Manual', color: 'text-slate-600' },
  2: { label: 'Website', color: 'text-indigo-600' },
  3: { label: 'Indicação', color: 'text-emerald-600' },
  4: { label: 'Scraping', color: 'text-amber-600' },
  5: { label: 'Redes Sociais', color: 'text-pink-600' },
  6: { label: 'E-mail Marketing', color: 'text-blue-600' },
  7: { label: 'Anúncios Pagos', color: 'text-purple-600' },
  8: { label: 'Evento', color: 'text-cyan-600' },
  9: { label: 'Parceria', color: 'text-teal-600' },
  10: { label: 'Importação', color: 'text-orange-600' },
  11: { label: 'Google Maps', color: 'text-red-600' },
};

export function SourceLabel({ source, sourceDescription }: { source?: number; sourceDescription?: string }) {
  if (source === undefined || source === null) {
    return <span className="text-xs text-slate-400">–</span>;
  }
  const config = SOURCE_LABELS[source];
  const label = sourceDescription || config?.label || 'Desconhecido';
  const color = config?.color || 'text-slate-500';
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
  columnCount,
  hasCheckbox,
  index,
}: {
  columnCount: number;
  hasCheckbox: boolean;
  index: number;
}) {
  return (
    <tr className="border-b border-slate-100">
      {hasCheckbox && (
        <td className="px-5 py-4 w-12">
          <div className="h-4 w-4 bg-slate-200 rounded animate-pulse" />
        </td>
      )}
      {Array.from({ length: columnCount }).map((_, j) => (
        <td key={j} className="px-5 py-4">
          {j === 0 ? (
            <div className="flex items-center gap-3">
              <div
                className="h-9 w-9 bg-slate-200 rounded-full animate-pulse shrink-0"
                style={{ animationDelay: `${index * 80}ms` }}
              />
              <div className="space-y-1.5 flex-1">
                <div
                  className="h-3.5 bg-slate-200 rounded animate-pulse"
                  style={{ width: `${55 + (index * 7) % 30}%`, animationDelay: `${index * 80}ms` }}
                />
                <div
                  className="h-3 bg-slate-100 rounded animate-pulse"
                  style={{ width: `${35 + (index * 11) % 25}%`, animationDelay: `${index * 80 + 40}ms` }}
                />
              </div>
            </div>
          ) : (
            <div
              className="h-3.5 bg-slate-200 rounded animate-pulse"
              style={{
                width: `${45 + ((index + j) * 13) % 40}%`,
                animationDelay: `${(index * columnCount + j) * 40}ms`,
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
      <td colSpan={columnCount} className="py-20 text-center">
        <div className="flex flex-col items-center gap-3">
          {icon || <SearchX className="h-12 w-12 text-slate-300" />}
          <div className="space-y-1">
            <p className="font-medium text-slate-600">
              {title || 'Nenhum resultado encontrado'}
            </p>
            <p className="text-sm text-slate-400 max-w-sm mx-auto">
              {description || 'Tente limpar os filtros ou buscar por outro termo.'}
            </p>
          </div>
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
      <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
        {label}
      </span>
    );
  }

  return (
    <button
      type="button"
      onClick={onClick}
      className="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500 hover:text-slate-900 transition-colors group"
    >
      {label}
      {active ? (
        direction === 'asc' ? (
          <ArrowUp className="h-3.5 w-3.5 text-slate-900" />
        ) : (
          <ArrowDown className="h-3.5 w-3.5 text-slate-900" />
        )
      ) : (
        <ArrowUpDown className="h-3.5 w-3.5 opacity-0 group-hover:opacity-40 transition-opacity" />
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
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
      {/* Table */}
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/80">
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
          <tbody className="divide-y divide-slate-100">
            {loading ? (
              Array.from({ length: Math.min(pageSize, 8) }).map((_, i) => (
                <SkeletonRow
                  key={i}
                  columnCount={columns.length}
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
                    className={[
                      'transition-colors',
                      onRowClick ? 'cursor-pointer' : '',
                      isSelected
                        ? 'bg-blue-50/60 hover:bg-blue-50'
                        : 'hover:bg-slate-50/80',
                    ].join(' ')}
                  >
                    {selectable && (
                      <td
                        className="w-12 px-5 py-4"
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
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 px-5 py-3.5 border-t border-slate-200 bg-slate-50/50">
          <div className="flex items-center gap-4 flex-wrap">
            <p className="text-sm text-slate-600">
              Mostrando{' '}
              <span className="font-semibold text-slate-900">{from}</span> a{' '}
              <span className="font-semibold text-slate-900">{to}</span> de{' '}
              <span className="font-semibold text-slate-900">{totalCount}</span>{' '}
              {itemLabel}
            </p>
            {selectable && selectedRows && selectedRows.length > 0 && (
              <span className="text-sm text-blue-600 font-medium">
                ({selectedRows.length} selecionado{selectedRows.length > 1 ? 's' : ''})
              </span>
            )}
            <div className="flex items-center gap-1.5">
              <span className="text-sm text-slate-500">Exibir</span>
              <select
                value={pageSize}
                onChange={(e) => onPageSizeChange(Number(e.target.value))}
                className="h-8 rounded-md border border-slate-200 bg-white px-2 text-sm text-slate-700 focus:outline-none focus:ring-2 focus:ring-slate-400 cursor-pointer"
              >
                <option value={10}>10</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
              </select>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1}
              className="h-9 px-3"
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Anterior
            </Button>
            <span className="text-sm text-slate-500 min-w-[4.5rem] text-center tabular-nums">
              {page} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= totalPages}
              className="h-9 px-3"
            >
              Próximo
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
