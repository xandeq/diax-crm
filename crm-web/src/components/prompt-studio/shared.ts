import { extractVariables } from '@/lib/promptTemplate';
import type { PromptListItem, PromptVariable } from '@/types/promptStudio';
import { formatDistanceToNowStrict } from 'date-fns';
import { ptBR } from 'date-fns/locale';

export type StudioView = 'all' | 'favorites' | 'recent' | 'templates' | 'mcp' | 'archived';
export type StudioTab = 'editor' | 'preview' | 'history';
export type StudioSort = 'updated' | 'used' | 'title';

export const VIEW_LABELS: Record<StudioView, string> = {
  all: 'Todos',
  favorites: 'Favoritos',
  recent: 'Recentes',
  templates: 'Templates',
  mcp: 'No Claude Code',
  archived: 'Arquivados',
};

export const COLLECTION_COLORS = ['#10B981', '#818CF8', '#F472B6', '#38BDF8', '#FBBF24', '#A3E635', '#F87171', '#C084FC'];

export const SOURCE_LABELS: Record<string, string> = {
  manual: 'Manual',
  generator: 'Gerador',
  snippet: 'Snippet',
  import: 'Import',
  mcp: 'Claude Code',
  git: 'Git',
};

const SEVEN_DAYS = 7 * 24 * 60 * 60 * 1000;

export function matchesView(p: PromptListItem, view: StudioView): boolean {
  switch (view) {
    case 'favorites': return p.isFavorite;
    case 'recent': return Date.now() - new Date(p.lastUsedAt ?? p.updatedAt).getTime() < SEVEN_DAYS;
    case 'templates': return p.kind === 'template';
    case 'mcp': return p.exposeToMcp;
    default: return true;
  }
}

export function relativeTime(iso: string | null | undefined): string {
  if (!iso) return '';
  try {
    return formatDistanceToNowStrict(new Date(iso), { addSuffix: true, locale: ptBR });
  } catch {
    return '';
  }
}

/** Variáveis efetivas: ordem do conteúdo; metadados vindos da versão/rascunho quando existirem. */
export function resolveVariables(content: string, declared: PromptVariable[] | undefined): PromptVariable[] {
  const byName = new Map((declared ?? []).map(v => [v.name, v]));
  return extractVariables(content).map(name => byName.get(name) ?? { name, type: 'text', required: true });
}

export function sameVariables(a: PromptVariable[], b: PromptVariable[]): boolean {
  return JSON.stringify(a.map(norm)) === JSON.stringify(b.map(norm));
}

function norm(v: PromptVariable) {
  return {
    n: v.name,
    l: v.label || null,
    t: v.type || 'text',
    d: v.default || null,
    r: v.required !== false,
    o: v.type === 'select' ? (v.options ?? []) : null,
  };
}

type StorageKind = 'local' | 'session';
const storageOf = (kind: StorageKind) => (kind === 'session' ? window.sessionStorage : window.localStorage);

/** Rascunhos usam localStorage (sobrevivem ao fechar a aba); valores de variáveis, sessionStorage. */
export function readLocal<T>(key: string, kind: StorageKind = 'local'): T | null {
  try {
    const raw = storageOf(kind).getItem(key);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

export function writeLocal(key: string, value: unknown, kind: StorageKind = 'local') {
  try {
    if (value === null) storageOf(kind).removeItem(key);
    else storageOf(kind).setItem(key, JSON.stringify(value));
  } catch {
    /* storage indisponível: fica só em memória */
  }
}

export function isTypingTarget(el: EventTarget | null): boolean {
  if (!(el instanceof HTMLElement)) return false;
  return el.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName);
}
