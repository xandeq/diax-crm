'use client';

import { tagColorClass } from '@/lib/promptTemplate';
import type { PromptListItem } from '@/types/promptStudio';
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Code2, Download, Menu, MoreVertical, Plus, Search, Star, Upload } from 'lucide-react';
import { forwardRef, useEffect, useRef } from 'react';
import { relativeTime, type StudioSort } from './shared';

interface Props {
  title: string;
  items: PromptListItem[];
  loading: boolean;
  error: string | null;
  selectedId: string | null;
  filter: string;
  sort: StudioSort;
  hasFilters: boolean;
  onFilter: (q: string) => void;
  onSort: (s: StudioSort) => void;
  onSelect: (id: string) => void;
  onToggleFavorite: (p: PromptListItem) => void;
  onNew: () => void;
  onClearFilters: () => void;
  onMenu: () => void;
  onImport: () => void;
  onExportVisible: (flavor: 'diax' | 'claude-command') => void;
}

export const PromptList = forwardRef<HTMLInputElement, Props>(function PromptList(p, filterRef) {
  const listRef = useRef<HTMLUListElement>(null);

  useEffect(() => {
    if (!p.selectedId) return;
    document.getElementById(`ps-row-${p.selectedId}`)?.scrollIntoView({ block: 'nearest' });
  }, [p.selectedId]);

  return (
    <section className="ps-list ps-pane" aria-label="Lista de prompts">
      <div className="ps-list-head">
        <div className="ps-list-title">
          <button className="ps-btn ghost icon ps-menu" onClick={p.onMenu} aria-label="Mostrar coleções e filtros"><Menu size={16} /></button>
          <h2>{p.title}</h2>
          <label className="sr-only" htmlFor="ps-sort">Ordenar</label>
          <select id="ps-sort" className="ps-select" value={p.sort} onChange={e => p.onSort(e.target.value as StudioSort)}>
            <option value="updated">Atualizado</option>
            <option value="used">Mais usado</option>
            <option value="title">A–Z</option>
          </select>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="ps-btn ghost icon" aria-label="Importar e exportar" title="Importar / exportar"><MoreVertical size={16} /></button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="bg-[#0D1F18] border-white/10 text-slate-200">
              <DropdownMenuItem onClick={p.onImport}><Upload size={14} className="mr-2" /> Importar .md / .txt / .zip…</DropdownMenuItem>
              <DropdownMenuSeparator className="bg-white/10" />
              <DropdownMenuItem disabled={!p.items.length} onClick={() => p.onExportVisible('diax')}>
                <Download size={14} className="mr-2" /> Exportar {p.items.length} visíveis (.zip)
              </DropdownMenuItem>
              <DropdownMenuItem disabled={!p.items.length} onClick={() => p.onExportVisible('claude-command')}>
                <Code2 size={14} className="mr-2" /> Exportar como commands do Claude Code
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <button className="ps-btn primary icon" onClick={p.onNew} aria-label="Novo prompt" title="Novo prompt (N)"><Plus size={16} /></button>
        </div>
        <div className="ps-filter">
          <Search size={14} />
          <label className="sr-only" htmlFor="ps-filter">Filtrar prompts</label>
          <input id="ps-filter" ref={filterRef} value={p.filter} placeholder="Filtrar título, tag, conteúdo…"
            autoComplete="off" onChange={e => p.onFilter(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Escape') { p.onFilter(''); (e.target as HTMLInputElement).blur(); }
              if (e.key === 'ArrowDown' || e.key === 'Enter') {
                e.preventDefault();
                const first = p.items[0];
                if (first) { p.onSelect(first.id); listRef.current?.focus(); }
              }
            }} />
          <span className="ps-kbd" aria-hidden="true">/</span>
        </div>
      </div>

      {p.loading ? (
        <div aria-busy="true">{Array.from({ length: 8 }).map((_, i) => <div key={i} className="ps-skel" />)}</div>
      ) : p.error ? (
        <div className="ps-empty" role="alert">{p.error}</div>
      ) : p.items.length === 0 ? (
        <div className="ps-empty">
          {p.hasFilters ? (
            <>Nada encontrado para esse filtro.<button className="ps-btn" onClick={p.onClearFilters}>Limpar filtros</button></>
          ) : (
            <>Nenhum prompt aqui ainda.<button className="ps-btn primary" onClick={p.onNew}><Plus size={14} /> Novo prompt</button></>
          )}
        </div>
      ) : (
        <ul ref={listRef} className="ps-rows" role="listbox" aria-label="Prompts" tabIndex={0}
          aria-activedescendant={p.selectedId ? `ps-row-${p.selectedId}` : undefined}>
          {p.items.map(item => (
            <li key={item.id} id={`ps-row-${item.id}`} role="option" aria-selected={item.id === p.selectedId}
              className="ps-row" onClick={() => p.onSelect(item.id)}>
              <button className={`ps-star ${item.isFavorite ? 'on' : ''}`} aria-pressed={item.isFavorite}
                aria-label={item.isFavorite ? 'Remover dos favoritos' : 'Favoritar'}
                onClick={e => { e.stopPropagation(); p.onToggleFavorite(item); }}>
                <Star size={14} fill={item.isFavorite ? 'currentColor' : 'none'} />
              </button>
              <span className="ttl">{item.title}</span>
              <span className="meta">v{item.currentVersionNumber} · {relativeTime(item.updatedAt)}</span>
              <span className="sub">
                {item.tags.slice(0, 2).map(t => <span key={t} className={`ps-tag ${tagColorClass(t)}`}>{t}</span>)}
                {item.tags.length > 2 && <span className="ps-mini">+{item.tags.length - 2}</span>}
                {item.kind === 'template' && <span className="ps-mini" title="Template">TPL</span>}
                {item.exposeToMcp && <span className="ps-mini" title="Exposto no Claude Code">MCP</span>}
                <span className="prev">{item.description || item.preview}</span>
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
});
