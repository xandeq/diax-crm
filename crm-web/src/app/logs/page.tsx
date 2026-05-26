'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { LogFilters } from '@/components/logs/LogFilters';
import { LogsTable } from '@/components/logs/LogsTable';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AppLogFilterRequest, AppLogPagedResponse, AppLogStatsResponse, LogLevel, logLevelLabels, logsService } from '@/services/logs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, ChevronLeft, ChevronRight, RefreshCw, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

const levelColorClasses: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'bg-gray-100 text-gray-700',
  [LogLevel.Information]: 'bg-blue-100 text-blue-700',
  [LogLevel.Warning]: 'bg-yellow-100 text-yellow-700',
  [LogLevel.Error]: 'bg-red-100 text-red-700',
  [LogLevel.Critical]: 'bg-purple-100 text-purple-700'
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
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Logs do Sistema</h1>
            <p className="text-gray-500 mt-1">Visualize e analise os logs da aplicação</p>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              className="text-red-600 border-red-200 hover:bg-red-50"
              onClick={() => setShowDeleteConfirm(true)}
              disabled={deleteMutation.isPending || loading}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Excluir Todos
            </Button>
            <Button onClick={() => { refetchLogs(); refetchStats(); }} variant="outline" disabled={loading}>
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
              color="bg-gray-100 text-gray-800"
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
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-red-500" />
            <div>
              <p className="font-medium text-red-800">Erro ao carregar logs</p>
              <p className="text-sm text-red-600">{errorMessage}</p>
            </div>
            <Button variant="outline" size="sm" className="ml-auto" onClick={() => { refetchLogs(); refetchStats(); }}>
              Tentar novamente
            </Button>
          </div>
        )}

        {/* Table */}
        <div className="bg-white rounded-lg border">
          <LogsTable logs={data?.items || []} loading={loading} />

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between px-4 py-3 border-t">
              <div className="text-sm text-gray-500">
                Mostrando {((data.page - 1) * data.pageSize) + 1} a {Math.min(data.page * data.pageSize, data.totalCount)} de {data.totalCount} registros
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={data.page <= 1}
                  onClick={() => handlePageChange(data.page - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Anterior
                </Button>
                <span className="text-sm text-gray-600">
                  Página {data.page} de {data.totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={data.page >= data.totalPages}
                  onClick={() => handlePageChange(data.page + 1)}
                >
                  Próxima
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Delete All Confirmation Modal */}
        {showDeleteConfirm && (
          <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
            <div className="rounded-lg p-6 max-w-md w-full mx-4" style={{ background: '#0D1F18', border: '1px solid rgba(255,255,255,0.12)' }}>
              <div className="flex items-center gap-3 mb-4">
                <div className="p-2 rounded-full" style={{ background: 'rgba(239,68,68,0.15)' }}>
                  <Trash2 className="h-5 w-5 text-red-400" />
                </div>
                <h3 className="text-lg font-semibold" style={{ color: '#F9FAFB' }}>Excluir Todos os Logs</h3>
              </div>
              <p className="mb-2" style={{ color: '#9CA3AF' }}>
                Tem certeza que deseja excluir <strong>todos os {stats?.totalCount?.toLocaleString('pt-BR') || ''} logs</strong> do sistema?
              </p>
              <p className="text-sm text-red-600 mb-6">
                Esta ação é irreversível e não pode ser desfeita.
              </p>
              <div className="flex justify-end gap-3">
                <Button variant="outline" onClick={() => setShowDeleteConfirm(false)} disabled={deleteMutation.isPending}>
                  Cancelar
                </Button>
                <Button
                  variant="destructive"
                  onClick={() => deleteMutation.mutate()}
                  disabled={deleteMutation.isPending}
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
    <div className="bg-white rounded-lg border p-4">
      <div className="text-sm text-gray-500 mb-1">{label}</div>
      <div className="flex items-center gap-2">
        <span className="text-2xl font-bold">{value.toLocaleString('pt-BR')}</span>
        <Badge className={color}>{label}</Badge>
      </div>
    </div>
  );
}
