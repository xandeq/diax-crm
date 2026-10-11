'use client';

import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { normalizeForSearch, ownValue, renderTemplate } from '@/lib/promptTemplate';
import { ApiError } from '@/services/api';
import { promptStudioService as svc } from '@/services/promptStudio';
import type {
  CreatePromptInput, ExportFlavor, PromptCollection, PromptDetail, PromptListItem, PromptVariable, PromptVersion,
  UpdatePromptInput,
} from '@/types/promptStudio';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FileText, Loader2, Plus } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { toast } from 'sonner';
import { ImportDialog } from './ImportDialog';
import { NewPromptDialog } from './NewPromptDialog';
import { PromptEditorPane } from './PromptEditorPane';
import { PromptList } from './PromptList';
import {
  isTypingTarget, matchesView, readLocal, resolveVariables, sameVariables, writeLocal,
  VIEW_LABELS, type StudioSort, type StudioTab, type StudioView,
} from './shared';
import { StudioSidebar } from './StudioSidebar';
import { STUDIO_CSS } from './styles';
import { VariablesPanel } from './VariablesPanel';

type Draft = { content: string; variables: PromptVariable[]; base: number };

const TABS: StudioTab[] = ['editor', 'preview', 'history'];
const errMsg = (e: unknown) => (e instanceof Error ? e.message : 'Erro inesperado');

function score(p: PromptListItem, tokens: string[]): number {
  if (tokens.length === 0) return 1;
  const title = normalizeForSearch(p.title);
  const tags = normalizeForSearch(p.tags.join(' '));
  const meta = normalizeForSearch(`${p.slug} ${p.description ?? ''} ${p.framework ?? ''}`);
  const body = normalizeForSearch(p.preview);
  let s = 0;
  for (const t of tokens) {
    if (title.includes(t)) s += 4;
    else if (tags.includes(t)) s += 3;
    else if (meta.includes(t)) s += 2;
    else if (body.includes(t)) s += 1;
    else return 0;
  }
  return s;
}

export function PromptStudio() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const qc = useQueryClient();

  const selectedId = searchParams.get('p');
  const tabParam = searchParams.get('tab') as StudioTab | null;
  const tab: StudioTab = tabParam && TABS.includes(tabParam) ? tabParam : 'editor';

  const [view, setView] = useState<StudioView>('all');
  const [collectionId, setCollectionId] = useState<string | null>(null);
  const [activeTags, setActiveTags] = useState<string[]>([]);
  const [filter, setFilter] = useState('');
  const [sort, setSort] = useState<StudioSort>('updated');
  const [sideOpen, setSideOpen] = useState(false);
  const [showDetail, setShowDetail] = useState(false);
  const [newOpen, setNewOpen] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [drafts, setDrafts] = useState<Record<string, Draft>>({});
  const [values, setValues] = useState<Record<string, Record<string, string>>>({});
  const [saveOpen, setSaveOpen] = useState(false);
  const [note, setNote] = useState('');
  const [conflict, setConflict] = useState(false);
  const [copied, setCopied] = useState(false);

  const filterRef = useRef<HTMLInputElement>(null);
  const editorRef = useRef<HTMLTextAreaElement>(null);
  const autoSelected = useRef(false);
  const { showConfirm, confirmDialogNode } = useConfirmDialog();

  // ───────────── Dados ─────────────
  const activeQ = useQuery({ queryKey: ['prompts', 'active'], queryFn: () => svc.list() });
  const archivedQ = useQuery({ queryKey: ['prompts', 'archived'], queryFn: () => svc.list({ archived: true }) });
  const collectionsQ = useQuery({ queryKey: ['prompt-collections'], queryFn: () => svc.collections() });
  const detailQ = useQuery({
    queryKey: ['prompt', selectedId],
    queryFn: () => svc.get(selectedId!),
    enabled: Boolean(selectedId),
  });

  const active = useMemo(() => activeQ.data ?? [], [activeQ.data]);
  const archived = useMemo(() => archivedQ.data ?? [], [archivedQ.data]);
  const collections = useMemo(() => collectionsQ.data ?? [], [collectionsQ.data]);
  const detail = detailQ.data;

  const counts = useMemo(() => {
    const c = {} as Record<StudioView, number>;
    (Object.keys(VIEW_LABELS) as StudioView[]).forEach(v => {
      c[v] = v === 'archived' ? archived.length : active.filter(p => matchesView(p, v)).length;
    });
    return c;
  }, [active, archived]);

  const tagCounts = useMemo(() => {
    const m = new Map<string, number>();
    active.forEach(p => p.tags.forEach(t => m.set(t, (m.get(t) ?? 0) + 1)));
    return [...m.entries()].map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count || a.name.localeCompare(b.name));
  }, [active]);

  // O índice local só tem o preview (280 chars); para termos mais adiante no texto,
  // a busca do servidor (conteúdo completo da versão atual) entra como complemento.
  const [serverTerm, setServerTerm] = useState('');
  useEffect(() => {
    const t = setTimeout(() => setServerTerm(filter.trim().length >= 3 ? filter.trim() : ''), 350);
    return () => clearTimeout(t);
  }, [filter]);
  const contentSearchQ = useQuery({
    queryKey: ['prompts', 'search', view === 'archived', serverTerm],
    queryFn: () => svc.list({ q: serverTerm, archived: view === 'archived' }),
    enabled: serverTerm.length >= 3,
    staleTime: 30_000,
  });
  const serverHits = useMemo(() => new Set((contentSearchQ.data ?? []).map(p => p.id)), [contentSearchQ.data]);

  const items = useMemo(() => {
    const tokens = normalizeForSearch(filter).split(/\s+/).filter(Boolean);
    const base = (view === 'archived' ? archived : active)
      .filter(p => view === 'archived' || matchesView(p, view))
      .filter(p => !collectionId || p.collectionId === collectionId)
      .filter(p => activeTags.every(t => p.tags.includes(t)));
    const scored = base
      .map(p => [p, score(p, tokens) || (serverTerm && serverHits.has(p.id) ? 0.5 : 0)] as const)
      .filter(([, s]) => s > 0);
    const by: Record<StudioSort, (a: PromptListItem, b: PromptListItem) => number> = {
      updated: (a, b) => b.updatedAt.localeCompare(a.updatedAt),
      used: (a, b) => b.useCount - a.useCount || b.updatedAt.localeCompare(a.updatedAt),
      title: (a, b) => a.title.localeCompare(b.title, 'pt-BR'),
    };
    return scored
      .sort((a, b) => (tokens.length ? b[1] - a[1] : 0) || by[sort](a[0], b[0]))
      .map(([p]) => p);
  }, [active, archived, view, collectionId, activeTags, filter, sort, serverTerm, serverHits]);

  const listTitle = collectionId
    ? collections.find(c => c.id === collectionId)?.name ?? 'Coleção'
    : VIEW_LABELS[view];

  // ───────────── Navegação ─────────────
  const navigate = useCallback((id: string | null, nextTab: StudioTab = 'editor') => {
    const qs = new URLSearchParams();
    if (id) qs.set('p', id);
    if (id && nextTab !== 'editor') qs.set('tab', nextTab);
    const s = qs.toString();
    router.replace(`/prompts/${s ? `?${s}` : ''}`, { scroll: false });
  }, [router]);

  const select = useCallback((id: string) => {
    navigate(id, id === selectedId ? tab : 'editor');
    setShowDetail(true);
  }, [navigate, selectedId, tab]);

  const setTab = useCallback((t: StudioTab) => { if (selectedId) navigate(selectedId, t); }, [navigate, selectedId]);

  useEffect(() => {
    if (autoSelected.current || selectedId || !activeQ.isSuccess) return;
    autoSelected.current = true;
    if (active[0] && window.matchMedia('(min-width: 768px)').matches) navigate(active[0].id);
  }, [activeQ.isSuccess, active, selectedId, navigate]);

  useEffect(() => {
    if (detailQ.isError && detailQ.error instanceof ApiError && detailQ.error.status === 404) {
      toast.error('Esse prompt não existe mais.');
      navigate(null);
    }
  }, [detailQ.isError, detailQ.error, navigate]);

  // ───────────── Rascunho + valores (persistidos localmente) ─────────────
  const draft = selectedId ? drafts[selectedId] : undefined;

  // Prompts cujo rascunho salvo já foi lido. Até lá NÃO gravamos no storage — senão o
  // "nada em memória" inicial apagaria o rascunho antes de ele ser recuperado.
  const recovered = useRef(new Set<string>());
  const draftsRef = useRef(drafts);
  draftsRef.current = drafts;

  // Só grava rascunho que ESTA aba tem em memória. Apagar o storage é exclusivo de ações
  // explícitas (salvar/descartar/restaurar/excluir via clearDraft) — senão uma aba "limpa"
  // apagaria o rascunho salvo por outra aba do mesmo prompt.
  const persistDraft = useCallback((id: string) => {
    const d = draftsRef.current[id];
    if (d && recovered.current.has(id)) writeLocal(`ps:draft:${id}`, d);
  }, []);

  useEffect(() => {
    if (!detail) return;
    const id = detail.prompt.id;
    if (!values[id]) {
      // Valores digitados (podem conter dados de cliente) vivem só na sessão.
      const saved = readLocal<Record<string, string>>(`ps:vals:${id}`, 'session');
      if (saved) setValues(v => ({ ...v, [id]: saved }));
    }
    if (recovered.current.has(id)) return;
    recovered.current.add(id);
    if (drafts[id]) return;
    const saved = readLocal<Draft>(`ps:draft:${id}`);
    if (!saved) return;
    const changed = saved.content !== detail.current.content ||
      !sameVariables(resolveVariables(saved.content, saved.variables), resolveVariables(detail.current.content, detail.current.variables));
    if (!changed) { writeLocal(`ps:draft:${id}`, null); return; }
    // Mantém o base antigo: se o prompt avançou em outro lugar, salvar dá 409 e o
    // diálogo de conflito decide — nunca descartamos texto em silêncio.
    setDrafts(d => ({ ...d, [id]: saved }));
    toast.info(saved.base === detail.current.number
      ? 'Rascunho não salvo recuperado.'
      : `Rascunho recuperado (feito sobre a v${saved.base}; o prompt já está na v${detail.current.number}).`);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detail?.prompt.id, detail?.current.number]);

  // Debounce ao digitar + flush imediato ao trocar de prompt.
  useEffect(() => {
    if (!selectedId) return;
    const t = setTimeout(() => persistDraft(selectedId), 500);
    return () => clearTimeout(t);
  }, [drafts, selectedId, persistDraft]);

  useEffect(() => {
    if (!selectedId) return;
    const id = selectedId;
    return () => persistDraft(id);
  }, [selectedId, persistDraft]);

  useEffect(() => {
    const flush = () => draftsRef.current && Object.keys(draftsRef.current).forEach(persistDraft);
    window.addEventListener('beforeunload', flush);
    return () => window.removeEventListener('beforeunload', flush);
  }, [persistDraft]);

  const content = draft?.content ?? detail?.current.content ?? '';
  const declared = draft?.variables ?? detail?.current.variables ?? [];
  const variables = useMemo(() => resolveVariables(content, declared), [content, declared]);
  const dirty = Boolean(detail && draft) && (
    draft!.content !== detail!.current.content ||
    !sameVariables(variables, resolveVariables(detail!.current.content, detail!.current.variables))
  );
  const promptValues = useMemo(() => (selectedId ? values[selectedId] ?? {} : {}), [values, selectedId]);
  const rendered = useMemo(() => {
    const defaults = Object.fromEntries(variables.map(v => [v.name, v.default]));
    const optional = variables.filter(v => v.required === false).map(v => v.name);
    return renderTemplate(content, promptValues, defaults, optional).rendered;
  }, [content, promptValues, variables]);

  const updateDraft = (patch: Partial<Draft>) => {
    if (!detail) return;
    const id = detail.prompt.id;
    setDrafts(d => {
      const curr = d[id] ?? { content: detail.current.content, variables: detail.current.variables, base: detail.current.number };
      return { ...d, [id]: { ...curr, ...patch } };
    });
  };

  const clearDraft = (id: string) => {
    setDrafts(d => { const { [id]: _, ...rest } = d; return rest; });
    writeLocal(`ps:draft:${id}`, null);
  };

  const setValue = (name: string, value: string) => {
    if (!selectedId) return;
    setValues(v => {
      const next = { ...(v[selectedId] ?? {}), [name]: value };
      if (!value) delete next[name];
      writeLocal(`ps:vals:${selectedId}`, Object.keys(next).length ? next : null, 'session');
      return { ...v, [selectedId]: next };
    });
  };

  const setVariableMeta = (v: PromptVariable) => {
    const list = declared.some(d => d.name === v.name) ? declared.map(d => (d.name === v.name ? v : d)) : [...declared, v];
    updateDraft({ variables: list });
  };

  // ───────────── Mutações ─────────────
  const refreshLists = () => {
    void qc.invalidateQueries({ queryKey: ['prompts'] });
    void qc.invalidateQueries({ queryKey: ['prompt-collections'] });
  };

  const patchListCache = (id: string, patch: Partial<PromptListItem>) => {
    for (const key of [['prompts', 'active'], ['prompts', 'archived']]) {
      qc.setQueryData<PromptListItem[]>(key, old => old?.map(p => (p.id === id ? { ...p, ...patch } : p)));
    }
    qc.setQueryData<PromptDetail>(['prompt', id], old => (old ? { ...old, prompt: { ...old.prompt, ...patch } } : old));
  };

  const updatePrompt = async (id: string, input: UpdatePromptInput) => {
    const optimistic: Partial<PromptListItem> = {};
    if (input.isFavorite !== undefined) optimistic.isFavorite = input.isFavorite;
    if (input.exposeToMcp !== undefined) optimistic.exposeToMcp = input.exposeToMcp;
    if (input.tags) optimistic.tags = input.tags;
    if (input.title) optimistic.title = input.title;
    patchListCache(id, optimistic);
    try {
      const res = await svc.update(id, input);
      qc.setQueryData(['prompt', id], res);
      refreshLists();
      if (input.isArchived !== undefined) {
        toast.success(input.isArchived ? 'Prompt arquivado' : 'Prompt restaurado do arquivo');
      }
      if (input.exposeToMcp) toast.success('Vai aparecer no Claude Code após /mcp reconnect.');
    } catch (e) {
      toast.error(`Não foi possível salvar: ${errMsg(e)}`);
      refreshLists();
      void qc.invalidateQueries({ queryKey: ['prompt', id] });
    }
  };

  const afterVersion = (id: string, base: number, created: PromptVersion) => {
    // Escreve a versão salva no cache ANTES de limpar o rascunho: senão o editor volta a mostrar
    // a versão antiga até o refetch e qualquer tecla nesse intervalo cria rascunho sobre ela.
    qc.setQueryData<PromptDetail>(['prompt', id], old => old && ({
      ...old,
      current: created,
      prompt: { ...old.prompt, currentVersionNumber: created.number },
    }));
    clearDraft(id);
    void qc.invalidateQueries({ queryKey: ['prompt', id] });
    void qc.invalidateQueries({ queryKey: ['prompt-versions', id] });
    refreshLists();
    if (created.number > base) toast.success(`v${created.number} salva`);
    else toast.info('Nada mudou desde a última versão.');
  };

  const saveMutation = useMutation({
    mutationFn: async ({ base, changeNote }: { base: number; changeNote: string }) => {
      const id = detail!.prompt.id;
      const v = await svc.addVersion(id, { content, variables, changeNote: changeNote || null, baseVersionNumber: base, source: 'manual' });
      return { id, base, v };
    },
    onSuccess: ({ id, base, v }) => { setSaveOpen(false); setConflict(false); setNote(''); afterVersion(id, base, v); },
    onError: e => {
      if (e instanceof ApiError && e.status === 409) {
        setSaveOpen(false);
        setConflict(true);
        void qc.invalidateQueries({ queryKey: ['prompt', selectedId] });
      } else {
        toast.error(`Não foi possível salvar a versão: ${errMsg(e)}`);
      }
    },
  });

  const restoreMutation = useMutation({
    mutationFn: async (n: number) => ({ id: detail!.prompt.id, base: detail!.current.number, v: await svc.restore(detail!.prompt.id, n) }),
    onSuccess: ({ id, base, v }) => { afterVersion(id, base, v); setTab('editor'); },
    onError: e => toast.error(`Não foi possível restaurar: ${errMsg(e)}`),
  });

  const createMutation = useMutation({
    mutationFn: (input: CreatePromptInput) => svc.create(input),
    onSuccess: res => {
      qc.setQueryData(['prompt', res.prompt.id], res);
      refreshLists();
      setNewOpen(false);
      setView('all');
      navigate(res.prompt.id);
      setShowDetail(true);
      setTimeout(() => editorRef.current?.focus(), 150);
      toast.success('Prompt criado');
    },
    onError: e => toast.error(`Não foi possível criar: ${errMsg(e)}`),
  });

  const openSave = () => {
    if (!detail) return;
    if (!dirty) { toast.info('Nada mudou desde a última versão.'); return; }
    setNote('');
    setSaveOpen(true);
  };

  const overwriteOnConflict = async () => {
    if (!detail) return;
    const fresh = await qc.fetchQuery({ queryKey: ['prompt', detail.prompt.id], queryFn: () => svc.get(detail.prompt.id) });
    saveMutation.mutate({ base: fresh.current.number, changeNote: 'Salva sobre alteração concorrente' });
  };

  const discardDraft = () => {
    if (!selectedId) return;
    clearDraft(selectedId);
    setConflict(false);
    toast.info('Rascunho descartado');
  };

  const duplicate = async () => {
    if (!detail) return;
    try {
      const res = await svc.duplicate(detail.prompt.id);
      refreshLists();
      navigate(res.prompt.id);
      toast.success('Prompt duplicado');
    } catch (e) { toast.error(`Não foi possível duplicar: ${errMsg(e)}`); }
  };

  const exportPrompts = async (ids: string[], flavor: ExportFlavor) => {
    if (!ids.length) return;
    if (dirty && detail && ids.includes(detail.prompt.id)) toast.info('O rascunho não salvo não entra no export (só a versão atual).');
    try {
      const fileName = await svc.exportPrompts({ ids, flavor, format: ids.length === 1 ? 'md' : 'zip' });
      toast.success(flavor === 'claude-command'
        ? `${fileName} baixado. Copie para ~/.claude/commands/ para usar como /comando.`
        : `${fileName} baixado`);
    } catch (e) { toast.error(`Não foi possível exportar: ${errMsg(e)}`); }
  };

  const remove = () => {
    if (!detail) return;
    const { id, title } = detail.prompt;
    showConfirm(`Excluir "${title}" e todas as ${detail.current.number} versões? Isso não pode ser desfeito. Para só tirar da lista, use Arquivar.`, async () => {
      try {
        await svc.remove(id);
        clearDraft(id);
        writeLocal(`ps:vals:${id}`, null, 'session');
        qc.removeQueries({ queryKey: ['prompt', id] });
        refreshLists();
        navigate(null);
        setShowDetail(false);
        toast.success('Prompt excluído');
      } catch (e) { toast.error(`Não foi possível excluir: ${errMsg(e)}`); }
    });
  };

  const createCollection = async (name: string) => {
    try {
      await svc.createCollection({ name });
      void qc.invalidateQueries({ queryKey: ['prompt-collections'] });
    } catch (e) { toast.error(errMsg(e)); }
  };

  const deleteCollection = (c: PromptCollection) => {
    showConfirm(`Excluir a coleção "${c.name}"? Os ${c.promptCount} prompt(s) continuam na biblioteca, sem coleção.`, async () => {
      try {
        await svc.deleteCollection(c.id);
        if (collectionId === c.id) setCollectionId(null);
        refreshLists();
        void qc.invalidateQueries({ queryKey: ['prompt'] });
      } catch (e) { toast.error(errMsg(e)); }
    });
  };

  const copyText = async (text: string) => {
    try {
      // Sem foco na janela a Clipboard API pode ficar pendente: limita a espera e cai no fallback.
      await Promise.race([
        navigator.clipboard.writeText(text),
        new Promise((_, reject) => setTimeout(() => reject(new Error('clipboard timeout')), 1500)),
      ]);
      return true;
    } catch {
      const ta = document.createElement('textarea');
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      let ok = false;
      try { ok = document.execCommand('copy'); } catch { /* sem fallback */ }
      ta.remove();
      return ok;
    }
  };

  const copyRendered = async () => {
    if (!detail) return;
    const missing = variables.filter(v => v.required !== false && !(ownValue(promptValues, v.name) || v.default));
    if (missing.length) {
      document.getElementById(`ps-var-${missing[0].name}`)?.focus();
      toast.warning(`Preencha: ${missing.map(v => v.label || v.name).join(', ')}`);
      return;
    }
    if (!(await copyText(rendered))) { toast.error('O navegador bloqueou a cópia. Selecione o texto renderizado.'); return; }
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
    toast.success(variables.length ? `Copiado (${variables.length} variáve${variables.length === 1 ? 'l' : 'is'} preenchida${variables.length === 1 ? '' : 's'})` : 'Copiado');
    svc.registerUse(detail.prompt.id).then(() => {
      patchListCache(detail.prompt.id, { useCount: detail.prompt.useCount + 1, lastUsedAt: new Date().toISOString() });
    }).catch(() => { /* contagem de uso é best-effort */ });
  };

  const copySlash = async () => {
    if (!detail) return;
    if (await copyText(`/mcp__diax-crm__${detail.prompt.slug}`)) toast.success('Comando do Claude Code copiado');
  };

  // ───────────── Teclado ─────────────
  const keyHandler = useRef<(e: KeyboardEvent) => void>(() => {});
  keyHandler.current = (e: KeyboardEvent) => {
    // Só diálogos abertos bloqueiam atalhos (o Radix mantém o nó montado durante a animação de saída).
    if (document.querySelector('[role="dialog"][data-state="open"]')) return;
    const mod = e.ctrlKey || e.metaKey;
    if (mod && e.key.toLowerCase() === 's') { e.preventDefault(); openSave(); return; }
    if (mod && e.key === 'Enter') { e.preventDefault(); void copyRendered(); return; }
    if (mod && /^[1-3]$/.test(e.key)) { e.preventDefault(); setTab(TABS[Number(e.key) - 1]); return; }
    if (mod || e.altKey || isTypingTarget(e.target)) return;
    const idx = items.findIndex(p => p.id === selectedId);
    switch (e.key) {
      case '/': e.preventDefault(); filterRef.current?.focus(); break;
      case 'j': case 'ArrowDown': if (items.length) { e.preventDefault(); select(items[Math.min(items.length - 1, idx + 1)].id); } break;
      case 'k': case 'ArrowUp': if (items.length) { e.preventDefault(); select(items[Math.max(0, idx - 1)].id); } break;
      case 'f': if (detail) void updatePrompt(detail.prompt.id, { isFavorite: !detail.prompt.isFavorite }); break;
      case 'e': if (detail) void updatePrompt(detail.prompt.id, { isArchived: !detail.prompt.isArchived }); break;
      case 'n': e.preventDefault(); setNewOpen(true); break;
    }
  };
  useEffect(() => {
    const h = (e: KeyboardEvent) => keyHandler.current(e);
    window.addEventListener('keydown', h);
    return () => window.removeEventListener('keydown', h);
  }, []);

  const hasFilters = Boolean(filter || activeTags.length || collectionId || view !== 'all');
  const listError = activeQ.isError ? `Não foi possível carregar a biblioteca: ${errMsg(activeQ.error)}` : null;

  return (
    <>
      <style>{STUDIO_CSS}</style>
      <div className={`ps ${sideOpen ? 'side-open' : ''} ${showDetail ? 'show-detail' : ''}`}>
        <StudioSidebar
          view={view} collectionId={collectionId} activeTags={activeTags} counts={counts}
          collections={collections} tags={tagCounts}
          onView={v => { setView(v); setCollectionId(null); setSideOpen(false); }}
          onCollection={id => { setCollectionId(id); setSideOpen(false); }}
          onToggleTag={(t, additive) => setActiveTags(curr =>
            curr.includes(t) ? curr.filter(x => x !== t) : additive ? [...curr, t] : [t])}
          onCreateCollection={createCollection}
          onDeleteCollection={deleteCollection}
        />

        <PromptList
          ref={filterRef}
          title={listTitle + (activeTags.length ? ` · ${activeTags.map(t => `#${t}`).join(' ')}` : '')}
          items={items} loading={activeQ.isLoading} error={listError} selectedId={selectedId}
          filter={filter} sort={sort} hasFilters={hasFilters}
          onFilter={setFilter} onSort={setSort} onSelect={select}
          onToggleFavorite={p => void updatePrompt(p.id, { isFavorite: !p.isFavorite })}
          onNew={() => setNewOpen(true)}
          onClearFilters={() => { setFilter(''); setActiveTags([]); setCollectionId(null); setView('all'); }}
          onMenu={() => setSideOpen(o => !o)}
          onImport={() => setImportOpen(true)}
          onExportVisible={flavor => void exportPrompts(items.map(i => i.id), flavor)}
        />

        {detail && selectedId ? (
          <>
            <PromptEditorPane
              ref={editorRef}
              detail={detail} content={content} rendered={rendered} dirty={dirty} variableCount={variables.length}
              collections={collections} allTags={tagCounts.map(t => t.name)} tab={tab}
              saving={saveMutation.isPending} restoring={restoreMutation.isPending}
              onTab={setTab}
              onContentChange={v => updateDraft({ content: v })}
              onUpdate={input => void updatePrompt(detail.prompt.id, input)}
              onSave={openSave}
              onDiscard={discardDraft}
              onRestore={n => {
                if (!dirty) { restoreMutation.mutate(n); return; }
                showConfirm(`Você tem um rascunho não salvo. Restaurar a v${n} descarta esse rascunho. Continuar?`,
                  () => restoreMutation.mutate(n));
              }}
              onDuplicate={() => void duplicate()}
              onExport={flavor => void exportPrompts([detail.prompt.id], flavor)}
              onDelete={remove}
              onCopySlashCommand={() => void copySlash()}
              onBack={() => setShowDetail(false)}
            />
            <VariablesPanel
              content={content} variables={variables} values={promptValues} copied={copied}
              onValue={setValue}
              onClear={() => { if (selectedId) { setValues(v => ({ ...v, [selectedId]: {} })); writeLocal(`ps:vals:${selectedId}`, null, 'session'); } }}
              onCopy={() => void copyRendered()}
              onVariableMeta={setVariableMeta}
            />
          </>
        ) : (
          <>
            <section className="ps-editor ps-pane" aria-label="Editor do prompt">
              <div className="ps-center">
                {selectedId && detailQ.isLoading ? (
                  <Loader2 className="animate-spin" />
                ) : (
                  <div className="flex flex-col items-center gap-3">
                    <FileText size={28} />
                    <p>Selecione um prompt na lista ou crie um novo.</p>
                    <button className="ps-btn primary" onClick={() => setNewOpen(true)}><Plus size={14} /> Novo prompt</button>
                    <p style={{ fontSize: 12 }}>Atalhos: <span className="ps-kbd">/</span> filtrar · <span className="ps-kbd">J</span><span className="ps-kbd">K</span> navegar · <span className="ps-kbd">N</span> novo</p>
                  </div>
                )}
              </div>
            </section>
            <aside className="ps-vars ps-pane" aria-hidden="true" />
          </>
        )}
      </div>

      <ImportDialog open={importOpen} onClose={() => setImportOpen(false)}
        onImported={report => {
          refreshLists();
          void qc.invalidateQueries({ queryKey: ['prompt'] });
          const first = report.items.find(i => i.promptId && (i.action === 'create' || i.action === 'newVersion'));
          if (first?.promptId) navigate(first.promptId);
        }} />

      <NewPromptDialog open={newOpen} collections={collections} defaultCollectionId={collectionId}
        creating={createMutation.isPending} onClose={() => setNewOpen(false)} onCreate={i => createMutation.mutate(i)} />

      <Dialog open={saveOpen} onOpenChange={setSaveOpen}>
        <DialogContent className="max-w-md bg-[#0D1F18] border-white/10 text-slate-200">
          <DialogHeader>
            <DialogTitle className="text-slate-50">Salvar v{(detail?.current.number ?? 0) + 1}</DialogTitle>
            <DialogDescription className="text-slate-400">O que mudou? É opcional, mas ajuda a achar a versão depois.</DialogDescription>
          </DialogHeader>
          <form className="flex flex-col gap-3" onSubmit={e => {
            e.preventDefault();
            if (draft) saveMutation.mutate({ base: draft.base, changeNote: note.trim() });
          }}>
            <label htmlFor="ps-note" className="sr-only">Nota da versão</label>
            <input id="ps-note" autoFocus maxLength={300} value={note} onChange={e => setNote(e.target.value)}
              placeholder="Ex.: mais exemplos de saída"
              className="h-10 rounded-lg border border-white/10 bg-[#0F1A14] px-3 text-sm text-slate-50 outline-none focus:border-emerald-500" />
            <div className="flex justify-end gap-2">
              <button type="button" className="h-9 rounded-lg px-3 text-sm text-slate-300 hover:bg-white/5" onClick={() => setSaveOpen(false)}>Cancelar</button>
              <button type="submit" disabled={saveMutation.isPending}
                className="inline-flex h-9 items-center gap-2 rounded-lg bg-emerald-500 px-4 text-sm font-semibold text-[#04140D] hover:bg-emerald-600 disabled:opacity-50">
                {saveMutation.isPending && <Loader2 size={14} className="animate-spin" />} Salvar versão
              </button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={conflict} onOpenChange={setConflict}>
        <DialogContent className="max-w-lg bg-[#0D1F18] border-white/10 text-slate-200">
          <DialogHeader>
            <DialogTitle className="text-slate-50">Esse prompt mudou em outro lugar</DialogTitle>
            <DialogDescription className="text-slate-400">
              Uma nova versão foi salva (talvez pelo Claude Code) enquanto você editava. Seu rascunho continua aqui.
            </DialogDescription>
          </DialogHeader>
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end [&>button]:whitespace-nowrap">
            <button className="h-9 rounded-lg px-3 text-sm text-slate-300 hover:bg-white/5" onClick={() => { setConflict(false); setTab('history'); }}>
              Ver histórico
            </button>
            <button className="h-9 rounded-lg border border-white/10 px-3 text-sm text-slate-200 hover:bg-white/5" onClick={discardDraft}>
              Descartar o meu
            </button>
            <button className="inline-flex h-9 items-center justify-center gap-2 rounded-lg bg-emerald-500 px-4 text-sm font-semibold text-[#04140D] hover:bg-emerald-600"
              onClick={() => void overwriteOnConflict()}>
              Salvar o meu como nova versão
            </button>
          </div>
        </DialogContent>
      </Dialog>

      {confirmDialogNode}
    </>
  );
}
