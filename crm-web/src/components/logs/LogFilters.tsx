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

  const selectClass = "w-full h-10 px-3 rounded-md border border-white/10 bg-white/5 text-sm text-zinc-300 focus:outline-none focus:border-[#00D4AA]/50 focus:ring-1 focus:ring-[#00D4AA]/50 transition-all duration-200";

  return (
    <div className="rounded-xl p-4 mb-4 border border-white/10 bg-white/[0.02] backdrop-blur-md">
      <div className="flex items-center justify-between">
        <button
          onClick={() => setExpanded(!expanded)}
          className="flex items-center gap-2 text-sm font-semibold cursor-pointer select-none transition-colors hover:text-zinc-100"
          style={{ color: '#D1D5DB' }}
        >
          <Filter className="h-4 w-4 text-[#00D4AA]" />
          Filtros
          {activeFiltersCount > 0 && (
            <span className="text-xs px-2 py-0.5 rounded-full font-bold bg-[#00D4AA]/15 text-[#00D4AA] border border-[#00D4AA]/20">
              {activeFiltersCount}
            </span>
          )}
          {expanded ? <ChevronUp className="h-4 w-4 text-zinc-400" /> : <ChevronDown className="h-4 w-4 text-zinc-400" />}
        </button>

        {activeFiltersCount > 0 && (
          <Button variant="ghost" size="sm" onClick={handleClearFilters} className="text-zinc-400 hover:text-zinc-100 hover:bg-white/5">
            <X className="h-4 w-4 mr-1" />
            Limpar filtros
          </Button>
        )}
      </div>

      {expanded && (
        <div className="mt-4 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="startDate" className="text-zinc-400 font-semibold">Data inicial</Label>
            <Input
              id="startDate"
              type="datetime-local"
              value={filters.startDate || ''}
              onChange={(e) => onFilterChange({ ...filters, startDate: e.target.value || undefined })}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="endDate" className="text-zinc-400 font-semibold">Data final</Label>
            <Input
              id="endDate"
              type="datetime-local"
              value={filters.endDate || ''}
              onChange={(e) => onFilterChange({ ...filters, endDate: e.target.value || undefined })}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="level" className="text-zinc-400 font-semibold">Nível</Label>
            <select
              id="level"
              className={selectClass}
              value={filters.level ?? ''}
              onChange={(e) => onFilterChange({
                ...filters,
                level: e.target.value ? Number(e.target.value) as LogLevel : undefined
              })}
            >
              <option value="" className="bg-[#0C1712] text-zinc-300">Todos</option>
              {Object.entries(logLevelLabels).map(([value, label]) => (
                <option key={value} value={value} className="bg-[#0C1712] text-zinc-300">{label}</option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="category" className="text-zinc-400 font-semibold">Categoria</Label>
            <select
              id="category"
              className={selectClass}
              value={filters.category ?? ''}
              onChange={(e) => onFilterChange({
                ...filters,
                category: e.target.value ? Number(e.target.value) as LogCategory : undefined
              })}
            >
              <option value="" className="bg-[#0C1712] text-zinc-300">Todas</option>
              {Object.entries(logCategoryLabels).map(([value, label]) => (
                <option key={value} value={value} className="bg-[#0C1712] text-zinc-300">{label}</option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="searchTerm" className="text-zinc-400 font-semibold">Buscar na mensagem</Label>
            <Input
              id="searchTerm"
              placeholder="Texto da mensagem..."
              value={filters.searchTerm || ''}
              onChange={(e) => onFilterChange({ ...filters, searchTerm: e.target.value || undefined })}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="userId" className="text-zinc-400 font-semibold">ID do usuário</Label>
            <Input
              id="userId"
              placeholder="UUID do usuário..."
              value={filters.userId || ''}
              onChange={(e) => onFilterChange({ ...filters, userId: e.target.value || undefined })}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="correlationId" className="text-zinc-400 font-semibold">Correlation ID</Label>
            <Input
              id="correlationId"
              placeholder="Correlation ID..."
              value={filters.correlationId || ''}
              onChange={(e) => onFilterChange({ ...filters, correlationId: e.target.value || undefined })}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="requestPath" className="text-zinc-400 font-semibold">Caminho da requisição</Label>
            <Input
              id="requestPath"
              placeholder="/api/v1/..."
              value={filters.requestPath || ''}
              onChange={(e) => onFilterChange({ ...filters, requestPath: e.target.value || undefined })}
            />
          </div>

          <div className="md:col-span-2 lg:col-span-4 flex justify-end">
            <Button onClick={onApply} className="bg-[#00D4AA] text-[#050B08] hover:bg-[#00bda0] font-bold">
              Aplicar filtros
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
