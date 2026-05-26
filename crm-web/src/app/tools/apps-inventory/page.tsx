'use client';

import React from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { AppEntry, AppType, APPS_LAST_UPDATED, appsInventory } from '@/data/apps-inventory';
import { Bot, Box, Calendar, Code2, ExternalLink, Filter, Layers, PackageSearch, RefreshCw, Search, Server, Wrench } from 'lucide-react';
import { useMemo, useState } from 'react';

const TYPE_STYLES: Record<AppType, React.CSSProperties> = {
  'SaaS':           { background: 'rgba(96,165,250,0.12)',  color: '#60a5fa' },
  'Pipeline':       { background: 'rgba(167,139,250,0.12)', color: '#a78bfa' },
  'Bot/Automação':  { background: 'rgba(251,146,60,0.12)',  color: '#fb923c' },
  'Scraper':        { background: 'rgba(250,204,21,0.12)',  color: '#facc15' },
  'Projeto':        { background: 'rgba(148,163,184,0.12)', color: '#94a3b8' },
  'Infraestrutura': { background: 'rgba(248,113,113,0.12)', color: '#f87171' },
  'Conteúdo':       { background: 'rgba(244,114,182,0.12)', color: '#f472b6' },
  'Scripts':        { background: 'rgba(45,212,191,0.12)',  color: '#2dd4bf' },
  'Ferramenta':     { background: 'rgba(52,211,153,0.12)',  color: '#34d399' },
  'Placeholder':    { background: 'rgba(107,114,128,0.12)', color: '#6b7280' },
};

const TYPE_ICONS: Record<AppType, React.ReactNode> = {
  'SaaS':           <Box className="h-3.5 w-3.5" />,
  'Pipeline':       <Layers className="h-3.5 w-3.5" />,
  'Bot/Automação':  <Bot className="h-3.5 w-3.5" />,
  'Scraper':        <Code2 className="h-3.5 w-3.5" />,
  'Projeto':        <PackageSearch className="h-3.5 w-3.5" />,
  'Infraestrutura': <Server className="h-3.5 w-3.5" />,
  'Conteúdo':       <Filter className="h-3.5 w-3.5" />,
  'Scripts':        <Code2 className="h-3.5 w-3.5" />,
  'Ferramenta':     <Wrench className="h-3.5 w-3.5" />,
  'Placeholder':    <Box className="h-3.5 w-3.5" />,
};

const ALL_TYPES = Array.from(new Set(appsInventory.map(a => a.type))).sort() as AppType[];

const SUMMARY = ALL_TYPES.map(type => ({
  type,
  count: appsInventory.filter(a => a.type === type).length,
})).filter(s => s.count > 0).sort((a, b) => b.count - a.count);

export default function AppsInventoryPage() {
  const [search, setSearch] = useState('');
  const [activeType, setActiveType] = useState<AppType | null>(null);

  const filtered = useMemo(() => {
    const q = search.toLowerCase().trim();
    return appsInventory.filter(app => {
      const matchesType = activeType ? app.type === activeType : true;
      const matchesSearch = !q
        || app.folder.toLowerCase().includes(q)
        || app.description.toLowerCase().includes(q)
        || app.stack.toLowerCase().includes(q)
        || app.type.toLowerCase().includes(q);
      return matchesType && matchesSearch;
    });
  }, [search, activeType]);

  return (
    <div className="py-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold" style={{ color: '#F9FAFB' }}>Inventário de Apps</h1>
          <p className="text-sm mt-1 flex items-center gap-1.5" style={{ color: '#9CA3AF' }}>
            <Calendar className="h-3.5 w-3.5" />
            Atualizado em {APPS_LAST_UPDATED} · {appsInventory.length} projetos em{' '}
            <code className="font-mono text-xs px-1 rounded" style={{ background: 'rgba(255,255,255,0.08)', color: '#D1D5DB' }}>D:\claude-code</code>
          </p>
        </div>
        <div
          title='Diga "atualiza o inventário de apps" para o Claude atualizar automaticamente'
          className="cursor-help"
        >
          <Button variant="outline" size="sm" className="gap-1.5 text-slate-500" disabled>
            <RefreshCw className="h-3.5 w-3.5" />
            Atualizar via Claude
          </Button>
        </div>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {SUMMARY.map(({ type, count }) => (
          <button
            key={type}
            onClick={() => setActiveType(activeType === type ? null : type)}
            className="text-left rounded-lg p-3 transition-all"
            style={activeType === type
              ? { background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.3)', outline: '2px solid rgba(16,185,129,0.2)' }
              : { background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}
          >
            <div className="flex items-center gap-1.5 mb-1">
              <span className="inline-flex items-center gap-1 text-[11px] font-medium rounded-full px-1.5 py-0.5" style={TYPE_STYLES[type]}>
                {TYPE_ICONS[type]}
                {type}
              </span>
            </div>
            <p className="text-xl font-bold" style={{ color: '#F9FAFB' }}>{count}</p>
          </button>
        ))}
      </div>

      {/* Search + filter bar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-slate-400" />
          <Input
            placeholder="Buscar por nome, stack, descrição..."
            className="pl-8"
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        {(search || activeType) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => { setSearch(''); setActiveType(null); }}
            className="text-slate-500"
          >
            Limpar filtros
          </Button>
        )}
        <span className="text-sm ml-auto" style={{ color: '#6B7280' }}>
          {filtered.length} resultado{filtered.length !== 1 ? 's' : ''}
        </span>
      </div>

      {/* Table */}
      <div className="rounded-xl overflow-hidden" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
        <div className="px-4 py-3 flex items-center gap-2" style={{ borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
          <PackageSearch className="h-4 w-4" style={{ color: '#9CA3AF' }} />
          <span className="text-base font-medium" style={{ color: '#D1D5DB' }}>
            {activeType ? `${activeType} (${filtered.length})` : `Todos os projetos (${filtered.length})`}
          </span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.07)', background: 'rgba(255,255,255,0.03)' }}>
                <th className="text-left px-4 py-2.5 font-medium w-[200px]" style={{ color: '#6B7280' }}>Pasta</th>
                <th className="text-left px-4 py-2.5 font-medium w-[140px]" style={{ color: '#6B7280' }}>Tipo</th>
                <th className="text-left px-4 py-2.5 font-medium w-[200px]" style={{ color: '#6B7280' }}>Stack</th>
                <th className="text-left px-4 py-2.5 font-medium" style={{ color: '#6B7280' }}>Descrição</th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-12" style={{ color: '#6B7280' }}>
                    Nenhum projeto encontrado
                  </td>
                </tr>
              ) : (
                filtered.map((app, i) => (
                  <AppRow key={app.folder} app={app} isLast={i === filtered.length - 1} />
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Footer hint */}
      <p className="text-xs text-center" style={{ color: '#6B7280' }}>
        Para atualizar, diga ao Claude:{' '}
        <span className="font-mono px-1.5 py-0.5 rounded" style={{ background: 'rgba(255,255,255,0.08)', color: '#D1D5DB' }}>&quot;atualiza o inventário de apps&quot;</span>
      </p>
    </div>
  );
}

function AppRow({ app, isLast }: { app: AppEntry; isLast: boolean }) {
  const isPlaceholder = app.type === 'Placeholder';

  return (
    <tr
      className="transition-colors"
      style={{ borderBottom: isLast ? undefined : '1px solid rgba(255,255,255,0.05)', opacity: isPlaceholder ? 0.5 : 1 }}
      onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
      onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
    >
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5">
          <a
            href={`file:///D:/claude-code/${app.folder}`}
            className="font-mono text-xs font-medium flex items-center gap-1 group hover:text-blue-400 transition-colors"
            style={{ color: '#D1D5DB' }}
            title={`D:\\claude-code\\${app.folder}`}
          >
            {app.folder}
            <ExternalLink className="h-3 w-3 opacity-0 group-hover:opacity-60 transition-opacity" />
          </a>
        </div>
      </td>
      <td className="px-4 py-3">
        <span
          className="inline-flex items-center gap-1 text-[11px] font-medium rounded-full px-1.5 py-0.5"
          style={TYPE_STYLES[app.type]}
        >
          {TYPE_ICONS[app.type]}
          {app.type}
        </span>
      </td>
      <td className="px-4 py-3">
        {app.stack === '—' ? (
          <span style={{ color: '#374151' }}>—</span>
        ) : (
          <span className="text-xs font-mono" style={{ color: '#9CA3AF' }}>{app.stack}</span>
        )}
      </td>
      <td className="px-4 py-3 text-xs leading-relaxed max-w-[400px]" style={{ color: '#9CA3AF' }}>
        {app.description}
      </td>
    </tr>
  );
}
