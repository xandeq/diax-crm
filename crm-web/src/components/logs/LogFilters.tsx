'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { AppLogFilterRequest, LogCategory, LogLevel, logLevelLabels } from '@/services/logs';
import { ChevronDown, ChevronUp, Filter, X } from 'lucide-react';
import { useState } from 'react';

const logCategoryLabels: Record<LogCategory, string> = {
  [LogCategory.System]: 'Sistema',
  [LogCategory.Security]: 'Segurança',
  [LogCategory.Audit]: 'Auditoria',
  [LogCategory.Business]: 'Negócio',
  [LogCategory.Performance]: 'Performance',
  [LogCategory.Integration]: 'Integração'
};

interface LogFiltersProps {
  filters: AppLogFilterRequest;
  onFilterChange: (filters: AppLogFilterRequest) => void;
  onApply: () => void;
}

export function LogFilters({ filters, onFilterChange, onApply }: LogFiltersProps) {
  const [expanded, setExpanded] = useState(false);

  const activeFiltersCount = [
    filters.startDate,
    filters.endDate,
    filters.level !== undefined,
    filters.category !== undefined,
    filters.searchTerm,
    filters.userId,
    filters.correlationId,
    filters.requestPath
  ].filter(Boolean).length;

  const handleClearFilters = () => {
    onFilterChange({
      pageNumber: 1,
      pageSize: filters.pageSize || 50
    });
  };

  return (
    <div className="bg-white rounded-lg border p-4 mb-4">
      <div className="flex items-center justify-between">
        <button
          onClick={() => setExpanded(!expanded)}
          className="flex items-center gap-2 text-sm font-medium text-gray-700 hover:text-gray-900"
        >
          <Filter className="h-4 w-4" />
          Filtros
          {activeFiltersCount > 0 && (
            <span className="bg-blue-100 text-blue-800 text-xs px-2 py-0.5 rounded-full">
              {activeFiltersCount}
            </span>
          )}
          {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </button>

        {activeFiltersCount > 0 && (
          <Button variant="ghost" size="sm" onClick={handleClearFilters}>
            <X className="h-4 w-4 mr-1" />
            Limpar filtros
          </Button>
        )}
      </div>

      {expanded && (
        <div className="mt-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div>
            <Label htmlFor="startDate">Data inicial</Label>
            <Input
              id="startDate"
              type="datetime-local"
              value={filters.startDate || ''}
              onChange={(e) => onFilterChange({ ...filters, startDate: e.target.value || undefined })}
            />
          </div>

          <div>
            <Label htmlFor="endDate">Data final</Label>
            <Input
              id="endDate"
              type="datetime-local"
              value={filters.endDate || ''}
              onChange={(e) => onFilterChange({ ...filters, endDate: e.target.value || undefined })}
            />
          </div>

          <div>
            <Label htmlFor="level">Nível</Label>
            <select
              id="level"
              className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
              value={filters.level ?? ''}
              onChange={(e) => onFilterChange({
                ...filters,
                level: e.target.value ? Number(e.target.value) as LogLevel : undefined
              })}
            >
              <option value="">Todos</option>
              {Object.entries(logLevelLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>

          <div>
            <Label htmlFor="category">Categoria</Label>
            <select
              id="category"
              className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm"
              value={filters.category ?? ''}
              onChange={(e) => onFilterChange({
                ...filters,
                category: e.target.value ? Number(e.target.value) as LogCategory : undefined
              })}
            >
              <option value="">Todas</option>
              {Object.entries(logCategoryLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>

          <div>
            <Label htmlFor="searchTerm">Buscar na mensagem</Label>
            <Input
              id="searchTerm"
              placeholder="Texto da mensagem..."
              value={filters.searchTerm || ''}
              onChange={(e) => onFilterChange({ ...filters, searchTerm: e.target.value || undefined })}
            />
          </div>

          <div>
            <Label htmlFor="userId">ID do usuário</Label>
            <Input
              id="userId"
              placeholder="UUID do usuário..."
              value={filters.userId || ''}
              onChange={(e) => onFilterChange({ ...filters, userId: e.target.value || undefined })}
            />
          </div>

          <div>
            <Label htmlFor="correlationId">Correlation ID</Label>
            <Input
              id="correlationId"
              placeholder="Correlation ID..."
              value={filters.correlationId || ''}
              onChange={(e) => onFilterChange({ ...filters, correlationId: e.target.value || undefined })}
            />
          </div>

          <div>
            <Label htmlFor="requestPath">Caminho da requisição</Label>
            <Input
              id="requestPath"
              placeholder="/api/v1/..."
              value={filters.requestPath || ''}
              onChange={(e) => onFilterChange({ ...filters, requestPath: e.target.value || undefined })}
            />
          </div>

          <div className="md:col-span-2 lg:col-span-4 flex justify-end">
            <Button onClick={onApply}>
              Aplicar filtros
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
