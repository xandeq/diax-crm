'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { AppEntry, AppType, APPS_LAST_UPDATED, appsInventory } from '@/data/apps-inventory';
import { Bot, Box, Calendar, Code2, ExternalLink, Filter, Layers, PackageSearch, RefreshCw, Search, Server, Wrench } from 'lucide-react';
import { useMemo, useState } from 'react';

const TYPE_COLORS: Record<AppType, string> = {
  'SaaS':           'bg-blue-100 text-blue-800',
  'Pipeline':       'bg-purple-100 text-purple-800',
  'Bot/Automação':  'bg-orange-100 text-orange-800',
  'Scraper':        'bg-yellow-100 text-yellow-800',
  'Projeto':        'bg-slate-100 text-slate-700',
  'Infraestrutura': 'bg-red-100 text-red-800',
  'Conteúdo':       'bg-pink-100 text-pink-800',
  'Scripts':        'bg-teal-100 text-teal-800',
  'Ferramenta':     'bg-green-100 text-green-800',
  'Placeholder':    'bg-gray-100 text-gray-500',
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
          <h1 className="text-2xl font-semibold text-slate-900">Inventário de Apps</h1>
          <p className="text-sm text-slate-500 mt-1 flex items-center gap-1.5">
            <Calendar className="h-3.5 w-3.5" />
            Atualizado em {APPS_LAST_UPDATED} · {appsInventory.length} projetos em{' '}
            <code className="font-mono text-xs bg-slate-100 px-1 rounded">D:\claude-code</code>
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
            className={`text-left rounded-lg border p-3 transition-all hover:shadow-sm ${
              activeType === type
                ? 'ring-2 ring-slate-400 bg-slate-50'
                : 'bg-white hover:border-slate-300'
            }`}
          >
            <div className="flex items-center gap-1.5 mb-1">
              <span className={`inline-flex items-center gap-1 text-[11px] font-medium rounded-full px-1.5 py-0.5 ${TYPE_COLORS[type]}`}>
                {TYPE_ICONS[type]}
                {type}
              </span>
            </div>
            <p className="text-xl font-bold text-slate-800">{count}</p>
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
        <span className="text-sm text-slate-400 ml-auto">
          {filtered.length} resultado{filtered.length !== 1 ? 's' : ''}
        </span>
      </div>

      {/* Table */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base font-medium text-slate-700 flex items-center gap-2">
            <PackageSearch className="h-4 w-4" />
            {activeType ? `${activeType} (${filtered.length})` : `Todos os projetos (${filtered.length})`}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-50">
                  <th className="text-left px-4 py-2.5 font-medium text-slate-600 w-[200px]">Pasta</th>
                  <th className="text-left px-4 py-2.5 font-medium text-slate-600 w-[140px]">Tipo</th>
                  <th className="text-left px-4 py-2.5 font-medium text-slate-600 w-[200px]">Stack</th>
                  <th className="text-left px-4 py-2.5 font-medium text-slate-600">Descrição</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="text-center py-12 text-slate-400">
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
        </CardContent>
      </Card>

      {/* Footer hint */}
      <p className="text-xs text-slate-400 text-center">
        Para atualizar, diga ao Claude:{' '}
        <span className="font-mono bg-slate-100 px-1.5 py-0.5 rounded">&quot;atualiza o inventário de apps&quot;</span>
      </p>
    </div>
  );
}

function AppRow({ app, isLast }: { app: AppEntry; isLast: boolean }) {
  const isPlaceholder = app.type === 'Placeholder';

  return (
    <tr className={`${isLast ? '' : 'border-b border-slate-50'} hover:bg-slate-50/60 transition-colors ${isPlaceholder ? 'opacity-50' : ''}`}>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5">
          <a
            href={`file:///D:/claude-code/${app.folder}`}
            className="font-mono text-xs font-medium text-slate-800 hover:text-blue-600 flex items-center gap-1 group"
            title={`D:\\claude-code\\${app.folder}`}
          >
            {app.folder}
            <ExternalLink className="h-3 w-3 opacity-0 group-hover:opacity-60 transition-opacity" />
          </a>
        </div>
      </td>
      <td className="px-4 py-3">
        <Badge
          variant="secondary"
          className={`text-[11px] font-medium gap-1 ${TYPE_COLORS[app.type]}`}
        >
          {TYPE_ICONS[app.type]}
          {app.type}
        </Badge>
      </td>
      <td className="px-4 py-3">
        {app.stack === '—' ? (
          <span className="text-slate-300">—</span>
        ) : (
          <span className="text-xs text-slate-600 font-mono">{app.stack}</span>
        )}
      </td>
      <td className="px-4 py-3 text-slate-600 text-xs leading-relaxed max-w-[400px]">
        {app.description}
      </td>
    </tr>
  );
}
