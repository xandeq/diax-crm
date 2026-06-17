'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { LogFilters } from '@/components/logs/LogFilters';
import { LogsTable } from '@/components/logs/LogsTable';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AppLogFilterRequest, LogLevel, logLevelLabels, logsService } from '@/services/logs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, ChevronLeft, ChevronRight, RefreshCw, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

const levelColorClasses: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20',
  [LogLevel.Information]: 'bg-blue-500/10 text-blue-300 border-blue-500/20',
  [LogLevel.Warning]: 'bg-amber-500/10 text-amber-300 border-amber-500/20',
  [LogLevel.Error]: 'bg-rose-500/10 text-rose-300 border-rose-500/20',
  [LogLevel.Critical]: 'bg-purple-500/10 text-purple-300 border-purple-500/20'
};

export default function LogsPage() {
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<AppLogFilterRequest>({ pageNumber: 1, pageSize: 50 });
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  const { data, isLoading: loading, isError, error, refetch: refetchLogs } = useQuery({
    queryKey: ['logs', filters],
    queryFn: () => logsService.getFilteredLogs(filters),
  });

  const { data: stats, refetch: refetchStats } = useQuery({
    queryKey: ['logStats'],
    queryFn: () => logsService.getStats(),
  });

  const errorMessage = isError ? (error instanceof Error ? error.message : 'Erro ao carregar logs') : null;

  const deleteMutation = useMutation({
    mutationFn: () => logsService.deleteAll(),
    onSuccess: (result) => {
      toast.success(`${result.deletedCount.toLocaleString('pt-BR')} logs excluídos com sucesso`);
      setShowDeleteConfirm(false);
      queryClient.invalidateQueries({ queryKey: ['logs'] });
      queryClient.invalidateQueries({ queryKey: ['logStats'] });
    },
    onError: (err: unknown) => {
      toast.error(err instanceof Error ? err.message : 'Erro ao excluir logs');
    },
  });

  const handleFilterChange = (newFilters: AppLogFilterRequest) => {
    setFilters(newFilters);
  };

  const handleApplyFilters = () => {
    setFilters((f) => ({ ...f, pageNumber: 1 }));
  };

  const handlePageChange = (page: number) => {
    setFilters((f) => ({ ...f, pageNumber: page }));
  };

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <div className="space-y-6">
        <div className="flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-2xl font-bold text-zinc-100">Logs do Sistema</h1>
            <p className="text-zinc-400 mt-1">Visualize e analise os logs da aplicação</p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              className="text-rose-400 border-rose-500/20 bg-rose-500/10 hover:bg-rose-500/20 font-semibold"
              onClick={() => setShowDeleteConfirm(true)}
              disabled={deleteMutation.isPending || loading}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Excluir Todos
            </Button>
            <Button
              onClick={() => { refetchLogs(); refetchStats(); }}
              variant="outline"
              disabled={loading}
              className="border-white/10 hover:bg-white/5 text-zinc-300"
            >
              <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Atualizar
            </Button>
          </div>
        </div>

        {/* Stats Cards */}
        {stats && (
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
            <StatCard
              label="Total"
              value={stats.totalCount}
              color="bg-zinc-500/10 text-zinc-300 border-zinc-500/20"
            />
            <StatCard
              label={logLevelLabels[LogLevel.Debug]}
              value={stats.debugCount}
              color={levelColorClasses[LogLevel.Debug]}
            />
            <StatCard
              label={logLevelLabels[LogLevel.Information]}
              value={stats.informationCount}
              color={levelColorClasses[LogLevel.Information]}
            />
            <StatCard
              label={logLevelLabels[LogLevel.Warning]}
              value={stats.warningCount}
              color={levelColorClasses[LogLevel.Warning]}
            />
            <StatCard
              label={logLevelLabels[LogLevel.Error]}
              value={stats.errorCount}
              color={levelColorClasses[LogLevel.Error]}
            />
            <StatCard
              label={logLevelLabels[LogLevel.Critical]}
              value={stats.criticalCount}
              color={levelColorClasses[LogLevel.Critical]}
            />
          </div>
        )}

        {/* Filters */}
        <LogFilters
          filters={filters}
          onFilterChange={handleFilterChange}
          onApply={handleApplyFilters}
        />

        {/* Error State */}
        {errorMessage && (
          <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-red-400" />
            <div>
              <p className="font-semibold text-red-300">Erro ao carregar logs</p>
              <p className="text-sm text-red-400">{errorMessage}</p>
            </div>
            <Button
              variant="outline"
              size="sm"
              className="ml-auto border-red-500/30 text-red-300 hover:bg-red-500/20"
              onClick={() => { refetchLogs(); refetchStats(); }}
            >
              Tentar novamente
            </Button>
          </div>
        )}

        {/* Table */}
        <div className="rounded-xl overflow-hidden border border-white/10 bg-white/[0.02] backdrop-blur-md">
          <LogsTable logs={data?.items || []} loading={loading} />

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between px-6 py-4 border-t border-white/[0.08]" style={{ background: 'rgba(255,255,255,0.01)' }}>
              <div className="text-sm text-zinc-400">
                Mostrando <span className="font-semibold text-zinc-100">{((data.page - 1) * data.pageSize) + 1}</span> a <span className="font-semibold text-zinc-100">{Math.min(data.page * data.pageSize, data.totalCount)}</span> de <span className="font-semibold text-zinc-100">{data.totalCount}</span> registros
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={data.page <= 1}
                  onClick={() => handlePageChange(data.page - 1)}
                  className="border-white/10 hover:bg-white/5 text-zinc-300"
                >
                  <ChevronLeft className="h-4 w-4 mr-1" />
                  Anterior
                </Button>
                <span className="text-sm text-zinc-400">
                  Página <span className="font-semibold text-zinc-100">{data.page}</span> de <span className="font-semibold text-zinc-100">{data.totalPages}</span>
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={data.page >= data.totalPages}
                  onClick={() => handlePageChange(data.page + 1)}
                  className="border-white/10 hover:bg-white/5 text-zinc-300"
                >
                  Próxima
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Delete All Confirmation Modal */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 backdrop-blur-sm">
            <div className="rounded-xl p-6 max-w-md w-full mx-4 bg-[#0A100D] border border-white/10 shadow-2xl">
              <div className="flex items-center gap-3 mb-4">
                <div className="p-2.5 rounded-full bg-red-500/10 border border-red-500/20">
                  <Trash2 className="h-5 w-5 text-red-400" />
                </div>
                <h3 className="text-lg font-bold text-zinc-100">Excluir Todos os Logs</h3>
              </div>
              <p className="mb-2 text-zinc-300">
                Tem certeza que deseja excluir <strong>todos os {stats?.totalCount?.toLocaleString('pt-BR') || ''} logs</strong> do sistema?
              </p>
              <p className="text-sm text-rose-400 font-semibold mb-6">
                Esta ação é irreversível e não pode ser desfeita.
              </p>
              <div className="flex justify-end gap-3">
                <Button
                  variant="outline"
                  onClick={() => setShowDeleteConfirm(false)}
                  disabled={deleteMutation.isPending}
                  className="border-white/10 hover:bg-white/5 text-zinc-300"
                >
                  Cancelar
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => deleteMutation.mutate()}
                  disabled={deleteMutation.isPending}
                  className="bg-red-600/20 border border-red-500/30 text-red-200 hover:bg-red-600/40"
                >
                  {deleteMutation.isPending ? (
                    <>
                      <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                      Excluindo...
                    </>
                  ) : (
                    <>
                      <Trash2 className="h-4 w-4 mr-2" />
                      Excluir Todos
                    </>
                  )}
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </RoleGuard>
  );
}

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="rounded-xl p-4 bg-white/[0.02] border border-white/10 transition-all duration-200 hover:scale-[1.02] hover:bg-white/[0.04]">
      <div className="text-xs font-semibold uppercase tracking-wider text-zinc-400 mb-1.5">{label}</div>
      <div className="flex items-center justify-between gap-2">
        <span className="text-2xl font-bold text-zinc-100 tabular-nums">{value.toLocaleString('pt-BR')}</span>
        <Badge variant="outline" className={`text-xs font-semibold px-2 py-0.5 rounded-full ${color}`}>{label}</Badge>
      </div>
    </div>
  );
}
