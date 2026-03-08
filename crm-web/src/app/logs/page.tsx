'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { LogFilters } from '@/components/logs/LogFilters';
import { LogsTable } from '@/components/logs/LogsTable';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AppLogFilterRequest, AppLogPagedResponse, AppLogStatsResponse, LogLevel, logLevelLabels, logsService } from '@/services/logs';
import { AlertCircle, ChevronLeft, ChevronRight, RefreshCw, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

const levelColorClasses: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'bg-gray-100 text-gray-700',
  [LogLevel.Information]: 'bg-blue-100 text-blue-700',
  [LogLevel.Warning]: 'bg-yellow-100 text-yellow-700',
  [LogLevel.Error]: 'bg-red-100 text-red-700',
  [LogLevel.Critical]: 'bg-purple-100 text-purple-700'
};

export default function LogsPage() {
  const [data, setData] = useState<AppLogPagedResponse | null>(null);
  const [stats, setStats] = useState<AppLogStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<AppLogFilterRequest>({
    pageNumber: 1,
    pageSize: 50
  });
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    loadLogs();
    loadStats();
  }, []);

  async function loadLogs() {
    try {
      setLoading(true);
      setError(null);
      const result = await logsService.getFilteredLogs(filters);
      setData(result);
    } catch (err: any) {
      setError(err.message || 'Erro ao carregar logs');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  async function loadStats() {
    try {
      const statsData = await logsService.getStats();
      setStats(statsData);
    } catch (err) {
      console.error('Error loading stats:', err);
    }
  }

  const handleFilterChange = (newFilters: AppLogFilterRequest) => {
    setFilters(newFilters);
  };

  const handleApplyFilters = () => {
    setFilters({ ...filters, pageNumber: 1 });
    loadLogs();
  };

  const handlePageChange = (page: number) => {
    setFilters({ ...filters, pageNumber: page });
    setTimeout(() => loadLogs(), 0);
  };

  async function handleDeleteAll() {
    try {
      setDeleting(true);
      const result = await logsService.deleteAll();
      toast.success(`${result.deletedCount.toLocaleString('pt-BR')} logs excluídos com sucesso`);
      setShowDeleteConfirm(false);
      loadLogs();
      loadStats();
    } catch (err: any) {
      toast.error(err.message || 'Erro ao excluir logs');
    } finally {
      setDeleting(false);
    }
  }

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
              disabled={deleting || loading}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Excluir Todos
            </Button>
            <Button onClick={() => { loadLogs(); loadStats(); }} variant="outline" disabled={loading}>
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
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-center gap-3">
            <AlertCircle className="h-5 w-5 text-red-500" />
            <div>
              <p className="font-medium text-red-800">Erro ao carregar logs</p>
              <p className="text-sm text-red-600">{error}</p>
            </div>
            <Button variant="outline" size="sm" className="ml-auto" onClick={() => { loadLogs(); loadStats(); }}>
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
            <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4 shadow-xl">
              <div className="flex items-center gap-3 mb-4">
                <div className="bg-red-100 p-2 rounded-full">
                  <Trash2 className="h-5 w-5 text-red-600" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900">Excluir Todos os Logs</h3>
              </div>
              <p className="text-gray-600 mb-2">
                Tem certeza que deseja excluir <strong>todos os {stats?.totalCount?.toLocaleString('pt-BR') || ''} logs</strong> do sistema?
              </p>
              <p className="text-sm text-red-600 mb-6">
                Esta ação é irreversível e não pode ser desfeita.
              </p>
              <div className="flex justify-end gap-3">
                <Button variant="outline" onClick={() => setShowDeleteConfirm(false)} disabled={deleting}>
                  Cancelar
                </Button>
                <Button
                  variant="destructive"
                  onClick={handleDeleteAll}
                  disabled={deleting}
                >
                  {deleting ? (
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
