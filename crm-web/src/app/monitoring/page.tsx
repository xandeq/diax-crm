'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorLogDetail } from '@/components/monitoring/ErrorLogDetail';
import { ErrorLogFilters } from '@/components/monitoring/ErrorLogFilters';
import { ErrorLogTable } from '@/components/monitoring/ErrorLogTable';
import { type ErrorLogFilters as Filters, errorLogsService } from '@/services/errorLogs';
import { useQuery } from '@tanstack/react-query';
import { AlertCircle, Bug, ChevronLeft, RefreshCw } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useCallback, useMemo, useState } from 'react';

// ─── URL → Filtros ────────────────────────────────────────────────────────────

function searchParamsToFilters(sp: URLSearchParams): Filters {
  return {
    appName:    sp.get('appName')    ?? undefined,
    level:      (sp.get('level')    as Filters['level']) ?? undefined,
    isResolved: sp.has('isResolved') ? sp.get('isResolved') === 'true' : undefined,
    from:       sp.get('from')       ?? undefined,
    to:         sp.get('to')         ?? undefined,
    search:     sp.get('search')     ?? undefined,
    cursor:     sp.get('cursor')     ?? undefined,
  };
}

function filtersToSearchParams(f: Filters): URLSearchParams {
  const sp = new URLSearchParams();
  if (f.appName)          sp.set('appName', f.appName);
  if (f.level)            sp.set('level', f.level);
  if (f.isResolved != null) sp.set('isResolved', String(f.isResolved));
  if (f.from)             sp.set('from', f.from);
  if (f.to)               sp.set('to', f.to);
  if (f.search)           sp.set('search', f.search);
  if (f.cursor)           sp.set('cursor', f.cursor);
  return sp;
}

// ─── Stats Cards ─────────────────────────────────────────────────────────────

function StatCard({ label, value, color = '#9CA3AF', loading = false }: {
  label: string; value?: number; color?: string; loading?: boolean;
}) {
  return (
    <div className="rounded-xl p-4" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
      <p className="text-xs mb-2" style={{ color: '#6B7280' }}>{label}</p>
      {loading
        ? <Skeleton className="h-7 w-16" style={{ background: 'rgba(255,255,255,0.07)' }} />
        : <p className="text-2xl font-bold" style={{ color }}>{(value ?? 0).toLocaleString('pt-BR')}</p>
      }
    </div>
  );
}

// ─── Página ───────────────────────────────────────────────────────────────────

export default function MonitoringPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Filtros vivem na URL — fonte de verdade para compartilhamento e back-button
  const filters = useMemo(() => searchParamsToFilters(searchParams), [searchParams]);

  // Histórico de cursores para navegação Back (stack)
  const [cursorHistory, setCursorHistory] = useState<string[]>([]);

  // Log selecionado para o drawer de detalhe
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // ── Queries ──────────────────────────────────────────────────────────────────

  const {
    data, isLoading, isError, error, refetch, isFetching
  } = useQuery({
    queryKey: ['error-logs', filters],
    queryFn: () => errorLogsService.getFiltered(filters),
    staleTime: 30_000,
    placeholderData: prev => prev, // mantém dados enquanto recarrega (transição suave)
  });

  const { data: stats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['error-log-stats'],
    queryFn: () => errorLogsService.getStats(),
    staleTime: 60_000,
  });

  // Apps conhecidos para o filtro dropdown
  const knownApps = useMemo(
    () => [...new Set((stats?.byApp ?? []).map(a => a.appName))].sort(),
    [stats]
  );

  // ── Navegação / Filtros ───────────────────────────────────────────────────────

  const applyFilters = useCallback((newFilters: Filters) => {
    const sp = filtersToSearchParams(newFilters);
    router.push(`/monitoring${sp.toString() ? `?${sp}` : ''}`, { scroll: false });
  }, [router]);

  const handleNextPage = useCallback(() => {
    if (!data?.nextCursor) return;
    // Empilha cursor atual para poder voltar
    setCursorHistory(h => [...h, filters.cursor ?? '']);
    applyFilters({ ...filters, cursor: data.nextCursor ?? undefined });
  }, [data?.nextCursor, filters, applyFilters]);

  const handlePrevPage = useCallback(() => {
    const history = [...cursorHistory];
    const prevCursor = history.pop() ?? undefined;
    setCursorHistory(history);
    applyFilters({ ...filters, cursor: prevCursor });
  }, [cursorHistory, filters, applyFilters]);

  const handleRefresh = useCallback(() => {
    refetch();
    refetchStats();
  }, [refetch, refetchStats]);

  // Ao alterar filtros, reseta paginação
  const handleFiltersChange = useCallback((f: Filters) => {
    setCursorHistory([]);
    applyFilters({ ...f, cursor: undefined });
  }, [applyFilters]);

  const currentPage = cursorHistory.length + 1;

  // ── Render ────────────────────────────────────────────────────────────────────

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <div className="space-y-5">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>
              <Bug className="inline h-5 w-5 mr-2 text-orange-400" aria-hidden />
              Central de Erros
            </h1>
            <p className="text-sm mt-0.5" style={{ color: '#9CA3AF' }}>
              Logs de erro de todas as aplicações em produção
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefresh}
            disabled={isFetching}
            className="gap-2"
          >
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} aria-hidden />
            Atualizar
          </Button>
        </div>

        {/* Stats */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
          <StatCard label="Não resolvidos" value={stats?.unresolvedTotal} color="#F87171" loading={statsLoading} />
          <StatCard label="Críticos hoje" value={stats?.criticalToday} color="#C084FC" loading={statsLoading} />
          <StatCard label="Total hoje" value={stats?.totalToday} color="#E5E7EB" loading={statsLoading} />
          {(stats?.byApp ?? []).slice(0, 1).map(a => (
            <StatCard key={a.appName} label={`${a.appName} (abertos)`} value={a.count} color="#FCD34D" loading={statsLoading} />
          ))}
        </div>

        {/* Filtros */}
        <ErrorLogFilters
          value={filters}
          knownApps={knownApps}
          onChange={handleFiltersChange}
        />

        {/* Erro de fetch */}
        {isError && (
          <div
            className="rounded-xl p-4 flex items-center gap-3"
            style={{ background: 'rgba(239,68,68,0.1)', border: '1px solid rgba(239,68,68,0.2)' }}
            role="alert"
          >
            <AlertCircle className="h-5 w-5 text-red-400 flex-shrink-0" aria-hidden />
            <div className="flex-1">
              <p className="font-medium text-red-300 text-sm">Erro ao carregar logs</p>
              <p className="text-xs text-red-400 mt-0.5">
                {error instanceof Error ? error.message : 'Tente novamente'}
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={() => refetch()} style={{ color: '#F87171', borderColor: 'rgba(239,68,68,0.3)' }}>
              Tentar novamente
            </Button>
          </div>
        )}

        {/* Tabela */}
        <div className="rounded-xl overflow-hidden" style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)' }}>
          <ErrorLogTable
            logs={data?.items ?? []}
            loading={isLoading}
            onSelect={setSelectedId}
            selectedId={selectedId}
          />

          {/* Empty state */}
          {!isLoading && !isError && data?.items.length === 0 && (
            <div className="py-16 text-center" role="status">
              <Bug className="h-10 w-10 mx-auto mb-3 opacity-20" style={{ color: '#9CA3AF' }} aria-hidden />
              <p className="font-medium" style={{ color: '#6B7280' }}>Nenhum log encontrado</p>
              <p className="text-xs mt-1" style={{ color: '#4B5563' }}>
                {Object.values(filters).some(Boolean)
                  ? 'Tente ajustar os filtros'
                  : 'Aguardando erros das aplicações'}
              </p>
            </div>
          )}

          {/* Paginação */}
          {(data?.items.length ?? 0) > 0 && (
            <div
              className="flex items-center justify-between px-4 py-3"
              style={{ borderTop: '1px solid rgba(255,255,255,0.06)' }}
            >
              <p className="text-xs" style={{ color: '#6B7280' }}>
                {data?.totalCount
                  ? `${data.totalCount.toLocaleString('pt-BR')} resultados · página ${currentPage}`
                  : `Página ${currentPage}`}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={currentPage <= 1 || isFetching}
                  onClick={handlePrevPage}
                  className="gap-1"
                  aria-label="Página anterior"
                >
                  <ChevronLeft className="h-4 w-4" />
                  Anterior
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!data?.nextCursor || isFetching}
                  onClick={handleNextPage}
                  className="gap-1"
                  aria-label="Próxima página"
                >
                  Próxima
                  <ChevronLeft className="h-4 w-4 rotate-180" />
                </Button>
              </div>
            </div>
          )}
        </div>

      </div>

      {/* Drawer de detalhe */}
      <ErrorLogDetail
        logId={selectedId}
        onClose={() => setSelectedId(null)}
        onResolved={() => {
          refetch();
          refetchStats();
        }}
      />
    </RoleGuard>
  );
}
