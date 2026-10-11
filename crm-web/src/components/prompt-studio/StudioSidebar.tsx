'use client';

import { tagColorClass } from '@/lib/promptTemplate';
import type { PromptCollection } from '@/types/promptStudio';
import { Archive, Clock, Code2, Layers, LayoutTemplate, Plus, Star, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { COLLECTION_COLORS, VIEW_LABELS, type StudioView } from './shared';

const VIEW_ICONS: Record<StudioView, React.ElementType> = {
  all: Layers,
  favorites: Star,
  recent: Clock,
  templates: LayoutTemplate,
  mcp: Code2,
  archived: Archive,
};

interface Props {
  view: StudioView;
  collectionId: string | null;
  activeTags: string[];
  counts: Record<StudioView, number>;
  collections: PromptCollection[];
  tags: { name: string; count: number }[];
  onView: (v: StudioView) => void;
  onCollection: (id: string | null) => void;
  onToggleTag: (tag: string, additive: boolean) => void;
  onCreateCollection: (name: string) => Promise<void>;
  onDeleteCollection: (c: PromptCollection) => void;
}

export function StudioSidebar(p: Props) {
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState('');
  const [showAllTags, setShowAllTags] = useState(false);

  const submit = async () => {
    const n = name.trim();
    if (!n) { setAdding(false); return; }
    await p.onCreateCollection(n);
    setName('');
    setAdding(false);
  };

  const tags = showAllTags ? p.tags : p.tags.slice(0, 14);

  return (
    <nav className="ps-side ps-pane" aria-label="Navegação do Prompt Studio">
      <div>
        <div className="ps-h">Biblioteca</div>
        <div className="ps-nav">
          {(Object.keys(VIEW_LABELS) as StudioView[]).map(v => {
            const Icon = VIEW_ICONS[v];
            return (
              <button key={v} className="ps-nav-item" aria-current={p.view === v && !p.collectionId}
                onClick={() => p.onView(v)}>
                <Icon size={15} /> <span>{VIEW_LABELS[v]}</span><span className="n">{p.counts[v]}</span>
              </button>
            );
          })}
        </div>
      </div>

      <div>
        <div className="ps-h">
          <span>Coleções</span>
          <button onClick={() => setAdding(a => !a)} aria-label="Nova coleção" title="Nova coleção"><Plus size={13} /></button>
        </div>
        <div className="ps-nav">
          {p.collections.map((c, i) => (
            <div key={c.id} className="group" style={{ position: 'relative' }}>
              <button className="ps-nav-item" aria-current={p.collectionId === c.id}
                onClick={() => p.onCollection(p.collectionId === c.id ? null : c.id)}>
                <span className="ps-dot" style={{ background: c.color || COLLECTION_COLORS[i % COLLECTION_COLORS.length] }} />
                <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{c.name}</span>
                <span className="n">{c.promptCount}</span>
              </button>
              <button
                className="ps-btn ghost icon opacity-0 group-hover:opacity-100 focus:opacity-100"
                style={{ position: 'absolute', right: 26, top: 2, width: 24, height: 24 }}
                onClick={() => p.onDeleteCollection(c)}
                aria-label={`Excluir coleção ${c.name}`} title="Excluir coleção">
                <Trash2 size={12} />
              </button>
            </div>
          ))}
          {p.collections.length === 0 && !adding && (
            <button className="ps-nav-item" style={{ color: 'var(--ps-t3)' }} onClick={() => setAdding(true)}>
              <Plus size={14} /> Criar coleção
            </button>
          )}
          {adding && (
            <form className="ps-inline-input" onSubmit={e => { e.preventDefault(); void submit(); }}>
              <label className="sr-only" htmlFor="ps-new-collection">Nome da coleção</label>
              <input id="ps-new-collection" autoFocus value={name} maxLength={60} placeholder="Ex.: Vendas"
                onChange={e => setName(e.target.value)}
                onKeyDown={e => { if (e.key === 'Escape') { setAdding(false); setName(''); } }}
                onBlur={() => void submit()} />
            </form>
          )}
        </div>
      </div>

      {p.tags.length > 0 && (
        <div>
          <div className="ps-h">Tags</div>
          <div className="ps-tagcloud">
            {tags.map(t => (
              <button key={t.name} className={`ps-tag ${tagColorClass(t.name)}`}
                aria-pressed={p.activeTags.includes(t.name)}
                title={`${t.count} prompt(s) · Shift+clique soma filtros`}
                onClick={e => p.onToggleTag(t.name, e.shiftKey)}>
                #{t.name}
              </button>
            ))}
            {p.tags.length > 14 && (
              <button className="ps-tag" style={{ color: 'var(--ps-t3)' }} onClick={() => setShowAllTags(s => !s)}>
                {showAllTags ? 'menos' : `+${p.tags.length - 14}`}
              </button>
            )}
          </div>
        </div>
      )}
    </nav>
  );
}
