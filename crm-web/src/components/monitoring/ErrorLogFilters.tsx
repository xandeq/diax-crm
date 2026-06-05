'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import type { ErrorLogFilters, ErrorLogLevel } from '@/services/errorLogs';
import { Search, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface Props {
  value: ErrorLogFilters;
  knownApps?: string[];
  onChange: (filters: ErrorLogFilters) => void;
}

const LEVELS: { value: ErrorLogLevel; label: string }[] = [
  { value: 'Warning',  label: 'Aviso' },
  { value: 'Error',    label: 'Erro' },
  { value: 'Critical', label: 'Crítico' },
];

export function ErrorLogFilters({ value, knownApps = [], onChange }: Props) {
  const [search, setSearch] = useState(value.search ?? '');

  // Debounce search para não disparar query a cada tecla
  useEffect(() => {
    const id = setTimeout(() => {
      if (search !== (value.search ?? '')) {
        onChange({ ...value, search: search || undefined, cursor: undefined });
      }
    }, 400);
    return () => clearTimeout(id);
  }, [search]); // eslint-disable-line react-hooks/exhaustive-deps

  const set = useCallback(
    (patch: Partial<ErrorLogFilters>) => onChange({ ...value, ...patch, cursor: undefined }),
    [value, onChange]
  );

  const hasFilters =
    !!value.level || !!value.appName || value.isResolved != null ||
    !!value.from || !!value.to || !!value.search;

  function clear() {
    setSearch('');
    onChange({});
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {/* Busca textual */}
      <div className="relative flex-1 min-w-[200px]">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5" style={{ color: '#6B7280' }} aria-hidden />
        <Input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Buscar por mensagem, exceção ou arquivo…"
          className="pl-9 h-9 text-sm"
          style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
          aria-label="Buscar logs"
        />
      </div>

      {/* Nível */}
      <Select
        value={value.level ?? '__all__'}
        onValueChange={v => set({ level: v === '__all__' ? undefined : v as ErrorLogLevel })}
      >
        <SelectTrigger
          className="h-9 w-32 text-sm"
          style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
          aria-label="Filtrar por nível"
        >
          <SelectValue placeholder="Nível" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">Todos os níveis</SelectItem>
          {LEVELS.map(l => <SelectItem key={l.value} value={l.value}>{l.label}</SelectItem>)}
        </SelectContent>
      </Select>

      {/* App */}
      {knownApps.length > 0 && (
        <Select
          value={value.appName ?? '__all__'}
          onValueChange={v => set({ appName: v === '__all__' ? undefined : v })}
        >
          <SelectTrigger
            className="h-9 w-36 text-sm"
            style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
            aria-label="Filtrar por aplicação"
          >
            <SelectValue placeholder="Aplicação" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">Todas as apps</SelectItem>
            {knownApps.map(a => <SelectItem key={a} value={a}>{a}</SelectItem>)}
          </SelectContent>
        </Select>
      )}

      {/* Status resolvido */}
      <Select
        value={value.isResolved == null ? '__all__' : value.isResolved ? 'true' : 'false'}
        onValueChange={v => set({ isResolved: v === '__all__' ? undefined : v === 'true' })}
      >
        <SelectTrigger
          className="h-9 w-32 text-sm"
          style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
          aria-label="Filtrar por status"
        >
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">Todos</SelectItem>
          <SelectItem value="false">Em aberto</SelectItem>
          <SelectItem value="true">Resolvidos</SelectItem>
        </SelectContent>
      </Select>

      {/* Data de */}
      <Input
        type="date"
        value={value.from ?? ''}
        onChange={e => set({ from: e.target.value || undefined })}
        className="h-9 w-36 text-sm"
        style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
        aria-label="Data inicial"
        title="Data inicial"
      />

      {/* Data até */}
      <Input
        type="date"
        value={value.to ?? ''}
        onChange={e => set({ to: e.target.value || undefined })}
        className="h-9 w-36 text-sm"
        style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
        aria-label="Data final"
        title="Data final"
      />

      {/* Limpar filtros */}
      {hasFilters && (
        <Button
          variant="ghost"
          size="sm"
          onClick={clear}
          className="h-9 gap-1 text-xs"
          style={{ color: '#9CA3AF' }}
          aria-label="Limpar filtros"
        >
          <X className="h-3.5 w-3.5" />
          Limpar
        </Button>
      )}
    </div>
  );
}
