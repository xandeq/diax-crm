'use client';

import { Skeleton } from '@/components/ui/skeleton';
import type { ErrorLogResponse } from '@/services/errorLogs';
import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { AlertCircle, CheckCircle } from 'lucide-react';
import { memo } from 'react';
import { ErrorLevelBadge } from './ErrorLevelBadge';

interface Props {
  logs: ErrorLogResponse[];
  loading: boolean;
  onSelect: (id: string) => void;
  selectedId?: string | null;
}

function relativeTime(iso: string) {
  try {
    return formatDistanceToNow(parseISO(iso), { addSuffix: true, locale: ptBR });
  } catch {
    return iso;
  }
}

function absoluteTime(iso: string) {
  try {
    return parseISO(iso).toLocaleString('pt-BR', { timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone });
  } catch {
    return iso;
  }
}

const ROW_STYLE = {
  base: 'grid grid-cols-[100px_120px_1fr_80px_80px] gap-3 px-4 py-3 text-sm cursor-pointer transition-colors items-center',
  hover: 'hover:bg-white/5',
  selected: 'bg-white/8',
  border: 'border-b border-white/5',
};

const LogRow = memo(function LogRow({
  log, selected, onClick
}: { log: ErrorLogResponse; selected: boolean; onClick: () => void }) {
  return (
    <tr
      onClick={onClick}
      className={`${ROW_STYLE.base} ${ROW_STYLE.hover} ${selected ? ROW_STYLE.selected : ''} ${ROW_STYLE.border} focus:outline-none focus-visible:ring-2 focus-visible:ring-green-500/50`}
      role="row"
      tabIndex={0}
      onKeyDown={e => (e.key === 'Enter' || e.key === ' ') && onClick()}
      aria-selected={selected}
      aria-label={`${log.level}: ${log.message}`}
      style={{ display: 'grid' }}
    >
      {/* Level */}
      <td role="cell" className="flex items-center">
        <ErrorLevelBadge level={log.level} size="sm" />
      </td>

      {/* App */}
      <td role="cell" className="truncate text-xs" style={{ color: '#9CA3AF' }}>
        {log.appName}
      </td>

      {/* Mensagem + exceção */}
      <td role="cell" className="min-w-0">
        <p className="truncate text-xs font-medium" style={{ color: '#F9FAFB' }} title={log.message}>
          {log.message}
        </p>
        {log.exceptionType && (
          <p className="truncate text-xs mt-0.5" style={{ color: '#6B7280' }} title={log.exceptionType}>
            {log.exceptionType}
          </p>
        )}
      </td>

      {/* Ocorrências */}
      <td role="cell" className="text-center text-xs" style={{ color: log.occurrenceCount > 1 ? '#FCD34D' : '#9CA3AF' }}>
        {log.occurrenceCount > 1 && (
          <span
            className="inline-flex items-center justify-center rounded-full px-1.5 py-0.5 text-xs font-semibold"
            style={{ background: 'rgba(252,211,77,0.12)', color: '#FCD34D' }}
            title={`${log.occurrenceCount} ocorrências`}
          >
            ×{log.occurrenceCount}
          </span>
        )}
      </td>

      {/* Tempo + status */}
      <td role="cell" className="text-xs text-right" style={{ color: '#6B7280' }}>
        <span title={absoluteTime(log.occurredAt)}>{relativeTime(log.occurredAt)}</span>
        <div className="flex justify-end mt-0.5">
          {log.isResolved
            ? <CheckCircle className="h-3 w-3 text-green-500" aria-label="Resolvido" />
            : <AlertCircle className="h-3 w-3 text-orange-400" aria-label="Em aberto" />
          }
        </div>
      </td>
    </tr>
  );
});

const SKELETON_COUNT = 8;

export function ErrorLogTable({ logs, loading, onSelect, selectedId }: Props) {
  return (
    <div role="table" aria-label="Logs de erro" aria-busy={loading}>
      {/* Cabeçalho */}
      <div
        role="row"
        className="grid grid-cols-[100px_120px_1fr_80px_80px] gap-3 px-4 py-2 text-xs font-medium sticky top-0 z-10"
        style={{ color: '#6B7280', background: 'rgba(13,26,17,0.95)', borderBottom: '1px solid rgba(255,255,255,0.07)' }}
      >
        <span role="columnheader">Nível</span>
        <span role="columnheader">App</span>
        <span role="columnheader">Mensagem / Exceção</span>
        <span role="columnheader" className="text-center">Ocorr.</span>
        <span role="columnheader" className="text-right">Quando</span>
      </div>

      {/* Skeletons no carregamento */}
      {loading && logs.length === 0 && (
        <tbody role="rowgroup">
          {Array.from({ length: SKELETON_COUNT }).map((_, i) => (
            <tr key={i} role="row" style={{ display: 'grid', gridTemplateColumns: '100px 120px 1fr 80px 80px', gap: 12, padding: '12px 16px', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
              <td><Skeleton className="h-5 w-16 rounded-full" style={{ background: 'rgba(255,255,255,0.07)' }} /></td>
              <td><Skeleton className="h-4 w-full" style={{ background: 'rgba(255,255,255,0.05)' }} /></td>
              <td>
                <Skeleton className="h-4 w-full mb-1.5" style={{ background: 'rgba(255,255,255,0.06)' }} />
                <Skeleton className="h-3 w-2/3" style={{ background: 'rgba(255,255,255,0.04)' }} />
              </td>
              <td><Skeleton className="h-4 w-8 mx-auto" style={{ background: 'rgba(255,255,255,0.05)' }} /></td>
              <td><Skeleton className="h-4 w-full" style={{ background: 'rgba(255,255,255,0.05)' }} /></td>
            </tr>
          ))}
        </tbody>
      )}

      {/* Linhas */}
      {!loading || logs.length > 0 ? (
        <tbody role="rowgroup">
          {logs.map(log => (
            <LogRow
              key={log.id}
              log={log}
              selected={log.id === selectedId}
              onClick={() => onSelect(log.id)}
            />
          ))}
        </tbody>
      ) : null}
    </div>
  );
}
