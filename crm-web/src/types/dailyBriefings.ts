export type BriefingSource = 'claude-ai' | 'codex-chatgpt' | 'tarefas-ias' | string;
export type BriefingFormat = 'html' | 'markdown';

export interface BriefingCard {
  id: string;
  source: BriefingSource;
  title: string;
  date: string;       // "YYYY-MM-DD" (DateOnly)
  createdAt: string;  // ISO datetime
}

export interface BriefingDetail extends BriefingCard {
  content: string;
  format: BriefingFormat;
}

/** Rótulo + cor por gerador, para os badges. */
export const BRIEFING_SOURCE_META: Record<string, { label: string; badge: string }> = {
  'claude-ai': { label: 'Claude AI', badge: 'bg-blue-100 text-blue-700' },
  'codex-chatgpt': { label: 'Codex / ChatGPT', badge: 'bg-emerald-100 text-emerald-700' },
  'tarefas-ias': { label: 'Tarefas para as IAs', badge: 'bg-amber-100 text-amber-700' },
};

export function sourceMeta(source: string) {
  return BRIEFING_SOURCE_META[source] ?? { label: source, badge: 'bg-slate-100 text-slate-700' };
}
