'use client';

import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { estimateTokens, normalizeTag, tagColorClass } from '@/lib/promptTemplate';
import { promptTypeOptions } from '@/services/promptGenerator';
import type { PromptCollection, PromptDetail, PromptKind, UpdatePromptInput } from '@/types/promptStudio';
import {
  Archive, ArchiveRestore, ArrowLeft, Code2, Copy, Download, Loader2, MoreHorizontal, Save, Star, Trash2, X,
} from 'lucide-react';
import { forwardRef, useEffect, useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { relativeTime, SOURCE_LABELS, type StudioTab } from './shared';
import { TemplateEditor } from './TemplateEditor';
import { VersionHistory } from './VersionHistory';

interface Props {
  detail: PromptDetail;
  content: string;
  rendered: string;
  dirty: boolean;
  variableCount: number;
  collections: PromptCollection[];
  allTags: string[];
  tab: StudioTab;
  saving: boolean;
  restoring: boolean;
  onTab: (t: StudioTab) => void;
  onContentChange: (value: string) => void;
  onUpdate: (input: UpdatePromptInput) => void;
  onSave: () => void;
  onDiscard: () => void;
  onRestore: (n: number) => void;
  onDuplicate: () => void;
  onExport: (flavor: 'diax' | 'claude-command') => void;
  onDelete: () => void;
  onCopySlashCommand: () => void;
  onBack: () => void;
}

const KIND_LABELS: Record<PromptKind, string> = { prompt: 'Prompt', template: 'Template', system: 'System prompt' };

export const PromptEditorPane = forwardRef<HTMLTextAreaElement, Props>(function PromptEditorPane(p, editorRef) {
  const { prompt, current } = p.detail;
  const [title, setTitle] = useState(prompt.title);
  const [tagDraft, setTagDraft] = useState('');
  const [previewRaw, setPreviewRaw] = useState(false);

  useEffect(() => setTitle(prompt.title), [prompt.id, prompt.title]);

  const commitTitle = () => {
    const t = title.trim();
    if (!t) { setTitle(prompt.title); return; }
    if (t !== prompt.title) p.onUpdate({ title: t });
  };

  const addTag = (raw: string) => {
    const t = normalizeTag(raw);
    setTagDraft('');
    if (!t || prompt.tags.includes(t)) return;
    p.onUpdate({ tags: [...prompt.tags, t] });
  };

  const tabs: [StudioTab, string][] = [['editor', 'Editor'], ['preview', 'Preview'], ['history', 'Histórico']];

  return (
    <section className="ps-editor ps-pane" aria-label="Editor do prompt">
      <div className="ps-ed-head">
        <div className="ps-ed-top">
          <button className="ps-btn ghost icon ps-back" onClick={p.onBack} aria-label="Voltar para a lista"><ArrowLeft size={16} /></button>
          <div style={{ flex: 1, minWidth: 0 }}>
            <label className="sr-only" htmlFor="ps-title">Título</label>
            <input id="ps-title" className="ps-title-in" value={title} maxLength={160}
              onChange={e => setTitle(e.target.value)} onBlur={commitTitle}
              onKeyDown={e => { if (e.key === 'Enter') (e.target as HTMLInputElement).blur(); if (e.key === 'Escape') { setTitle(prompt.title); (e.target as HTMLInputElement).blur(); } }} />
            <div className="ps-slug">
              <span>/mcp__diax-crm__{prompt.slug}</span>
              <button className="ps-btn ghost icon" style={{ width: 22, height: 22 }} onClick={p.onCopySlashCommand}
                aria-label="Copiar comando do Claude Code" title="Copiar comando do Claude Code"><Copy size={12} /></button>
            </div>
          </div>
          <button className={`ps-btn icon ${prompt.isFavorite ? '' : 'ghost'}`} style={prompt.isFavorite ? { color: 'var(--ps-fav)' } : undefined}
            aria-pressed={prompt.isFavorite} aria-label={prompt.isFavorite ? 'Remover dos favoritos' : 'Favoritar'} title="Favoritar (F)"
            onClick={() => p.onUpdate({ isFavorite: !prompt.isFavorite })}>
            <Star size={16} fill={prompt.isFavorite ? 'currentColor' : 'none'} />
          </button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="ps-btn ghost icon" aria-label="Mais ações"><MoreHorizontal size={16} /></button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="bg-[#0D1F18] border-white/10 text-slate-200">
              <DropdownMenuItem onClick={p.onDuplicate}><Copy size={14} className="mr-2" /> Duplicar</DropdownMenuItem>
              <DropdownMenuItem onClick={() => p.onExport('diax')}><Download size={14} className="mr-2" /> Exportar .md</DropdownMenuItem>
              <DropdownMenuItem onClick={() => p.onExport('claude-command')}><Code2 size={14} className="mr-2" /> Exportar como command do Claude Code</DropdownMenuItem>
              <DropdownMenuItem onClick={() => p.onUpdate({ isArchived: !prompt.isArchived })}>
                {prompt.isArchived ? <><ArchiveRestore size={14} className="mr-2" /> Desarquivar</> : <><Archive size={14} className="mr-2" /> Arquivar</>}
              </DropdownMenuItem>
              <DropdownMenuSeparator className="bg-white/10" />
              <DropdownMenuItem onClick={p.onDelete} className="text-red-300 focus:text-red-200"><Trash2 size={14} className="mr-2" /> Excluir…</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
          <button className="ps-btn primary" onClick={p.onSave} disabled={!p.dirty || p.saving} title="Salvar nova versão (Ctrl+S)">
            {p.saving ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
            <span>Salvar v{current.number + 1}</span>
          </button>
        </div>

        <div className="ps-chips">
          <span className="ps-chip">
            <label className="sr-only" htmlFor="ps-collection">Coleção</label>
            <select id="ps-collection" value={prompt.collectionId ?? ''}
              onChange={e => p.onUpdate(e.target.value ? { collectionId: e.target.value } : { clearCollection: true })}>
              <option value="">Sem coleção</option>
              {p.collections.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </span>
          <span className="ps-chip">
            <label className="sr-only" htmlFor="ps-kind">Tipo</label>
            <select id="ps-kind" value={prompt.kind} onChange={e => p.onUpdate({ kind: e.target.value as PromptKind })}>
              {(Object.keys(KIND_LABELS) as PromptKind[]).map(k => <option key={k} value={k}>{KIND_LABELS[k]}</option>)}
            </select>
          </span>
          <span className="ps-chip">
            <label className="sr-only" htmlFor="ps-framework">Framework</label>
            <select id="ps-framework" value={prompt.framework ?? ''} onChange={e => p.onUpdate({ framework: e.target.value })}>
              <option value="">Sem framework</option>
              {promptTypeOptions.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
              {prompt.framework && !promptTypeOptions.some(f => f.value === prompt.framework) && <option value={prompt.framework}>{prompt.framework}</option>}
            </select>
          </span>
          <button className={`ps-chip ${prompt.exposeToMcp ? 'on' : ''}`} aria-pressed={prompt.exposeToMcp}
            title="Disponível como slash command no Claude Code via MCP"
            onClick={() => p.onUpdate({ exposeToMcp: !prompt.exposeToMcp })}>
            <Code2 size={13} /> {prompt.exposeToMcp ? 'No Claude Code' : 'Expor no Claude Code'}
          </button>
          {prompt.tags.map(t => (
            <span key={t} className={`ps-tag ${tagColorClass(t)}`}>
              #{t}
              <button className="x" aria-label={`Remover tag ${t}`} onClick={() => p.onUpdate({ tags: prompt.tags.filter(x => x !== t) })}><X size={11} /></button>
            </span>
          ))}
          <span className="ps-chip">
            <label className="sr-only" htmlFor="ps-tag-add">Adicionar tag</label>
            <input id="ps-tag-add" className="ps-tag-input" list="ps-tag-options" placeholder="+ tag" value={tagDraft}
              onChange={e => setTagDraft(e.target.value)}
              onKeyDown={e => {
                if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addTag(tagDraft); }
                if (e.key === 'Backspace' && !tagDraft && prompt.tags.length) p.onUpdate({ tags: prompt.tags.slice(0, -1) });
                if (e.key === 'Escape') { setTagDraft(''); (e.target as HTMLInputElement).blur(); }
              }}
              onBlur={() => tagDraft && addTag(tagDraft)} />
            <datalist id="ps-tag-options">{p.allTags.filter(t => !prompt.tags.includes(t)).map(t => <option key={t} value={t} />)}</datalist>
          </span>
        </div>

        <div className="ps-tabs" role="tablist" aria-label="Seções do prompt">
          {tabs.map(([key, label], i) => (
            <button key={key} role="tab" className="ps-tab" aria-selected={p.tab === key} title={`Ctrl+${i + 1}`}
              onClick={() => p.onTab(key)}>
              {label}{key === 'history' && <span className="ps-mini" style={{ marginLeft: 6 }}>{current.number}</span>}
            </button>
          ))}
        </div>
      </div>

      <div className="ps-panel" role="tabpanel">
        {p.tab === 'editor' && (
          <>
            <TemplateEditor ref={editorRef} value={p.content} onChange={p.onContentChange}
              placeholder={'Escreva o prompt. Use {{variavel}} para campos preenchíveis.'} />
            <div className="ps-status">
              <span>v{current.number} · {relativeTime(current.createdAt)} · {SOURCE_LABELS[current.source] ?? current.source}</span>
              <span>{p.content.length.toLocaleString('pt-BR')} caracteres</span>
              <span>~{estimateTokens(p.content).toLocaleString('pt-BR')} tokens</span>
              <span>{p.variableCount} variáve{p.variableCount === 1 ? 'l' : 'is'}</span>
              {p.dirty && (
                <span className="draft">
                  ● Rascunho não salvo{' '}
                  <button className="ps-btn ghost" style={{ height: 22, padding: '0 6px', fontSize: 11.5 }} onClick={p.onDiscard}>descartar</button>
                </span>
              )}
              <span className="sp" />
              <span>Use <code style={{ fontFamily: 'var(--ps-mono)' }}>{'{{nome}}'}</code> para variáveis</span>
            </div>
          </>
        )}

        {p.tab === 'preview' && (
          <>
            <div className="ps-status" style={{ paddingBottom: 0 }}>
              <span>Prévia com as variáveis preenchidas</span><span className="sp" />
              <button className={`ps-chip ${previewRaw ? '' : 'on'}`} onClick={() => setPreviewRaw(false)}>Markdown</button>
              <button className={`ps-chip ${previewRaw ? 'on' : ''}`} onClick={() => setPreviewRaw(true)}>Texto</button>
            </div>
            {previewRaw ? (
              <div className="ps-preview raw">{p.rendered}</div>
            ) : (
              <div className="ps-preview prose prose-invert prose-sm max-w-none">
                <ReactMarkdown remarkPlugins={[remarkGfm]}>{p.rendered}</ReactMarkdown>
              </div>
            )}
          </>
        )}

        {p.tab === 'history' && (
          <VersionHistory promptId={prompt.id} current={current} onRestore={p.onRestore} restoring={p.restoring} />
        )}
      </div>
    </section>
  );
});
