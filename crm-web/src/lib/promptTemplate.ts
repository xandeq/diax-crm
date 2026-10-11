/**
 * Regras de template do Prompt Studio — espelho de
 * api-core/src/Diax.Domain/PromptLibrary/PromptTemplate.cs. Mantenha os dois em sincronia
 * (vetores de teste em PromptTemplateTests.cs).
 */

// (\\)? = escape "\{{nome}}" → literal "{{nome}}". Nome ASCII, igual ao backend.
const VARIABLE_RE = /(\\)?\{\{\s*([A-Za-z_][A-Za-z0-9_.-]{0,63})\s*\}\}/g;

export function extractVariables(content: string | null | undefined): string[] {
  const names: string[] = [];
  if (!content) return names;
  for (const m of content.matchAll(VARIABLE_RE)) {
    if (m[1]) continue;
    if (!names.includes(m[2])) names.push(m[2]);
  }
  return names;
}

/**
 * Lookup só em propriedades próprias: nomes válidos como `constructor`/`__proto__`
 * não podem devolver o valor herdado do Object.prototype (o C# trata como ausente).
 */
export function ownValue<T>(obj: Record<string, T> | null | undefined, key: string): T | undefined {
  return obj && Object.prototype.hasOwnProperty.call(obj, key) ? obj[key] : undefined;
}

export interface RenderResult {
  rendered: string;
  missing: string[];
}

export function renderTemplate(
  content: string,
  values: Record<string, string | undefined> = {},
  defaults: Record<string, string | null | undefined> = {}
): RenderResult {
  const missing: string[] = [];
  const rendered = (content ?? '').replace(VARIABLE_RE, (full, esc: string | undefined, name: string) => {
    if (esc) return `{{${name}}}`;
    const value = ownValue(values, name) || ownValue(defaults, name) || '';
    if (!value) {
      if (!missing.includes(name)) missing.push(name);
      return full;
    }
    return value;
  });
  return { rendered, missing };
}

/** Segmentos para destacar variáveis (editor e preview). */
export type TemplateSegment =
  | { kind: 'text'; text: string }
  | { kind: 'var'; name: string; raw: string; escaped: boolean };

export function segmentTemplate(content: string): TemplateSegment[] {
  const out: TemplateSegment[] = [];
  let last = 0;
  for (const m of content.matchAll(VARIABLE_RE)) {
    const idx = m.index ?? 0;
    if (idx > last) out.push({ kind: 'text', text: content.slice(last, idx) });
    out.push({ kind: 'var', name: m[2], raw: m[0], escaped: Boolean(m[1]) });
    last = idx + m[0].length;
  }
  if (last < content.length) out.push({ kind: 'text', text: content.slice(last) });
  return out;
}

export function normalizeTag(tag: string): string | null {
  const s = tag
    .trim()
    .replace(/^#+/, '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/-{2,}/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 40)
    .replace(/-+$/g, '');
  return s.length ? s : null;
}

export function normalizeForSearch(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/** Estimativa grosseira (~4 chars/token em PT/EN). Só informativa. */
export function estimateTokens(text: string): number {
  return Math.ceil(text.length / 4);
}

const TAG_HUES = ['t0', 't1', 't2', 't3', 't4', 't5', 't6', 't7'] as const;
export function tagColorClass(tag: string): string {
  let h = 0;
  for (const c of tag) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  return TAG_HUES[h % TAG_HUES.length];
}

// ───────────────────────────── Diff ─────────────────────────────

export type DiffPart = { type: 'same' | 'add' | 'del'; text: string };

function lcsDiff(a: string[], b: string[]): DiffPart[] {
  const n = a.length, m = b.length;
  const L: Uint32Array[] = Array.from({ length: n + 1 }, () => new Uint32Array(m + 1));
  for (let i = n - 1; i >= 0; i--)
    for (let j = m - 1; j >= 0; j--)
      L[i][j] = a[i] === b[j] ? L[i + 1][j + 1] + 1 : Math.max(L[i + 1][j], L[i][j + 1]);
  const parts: DiffPart[] = [];
  const push = (type: DiffPart['type'], text: string) => {
    const prev = parts[parts.length - 1];
    if (prev && prev.type === type) prev.text += text;
    else parts.push({ type, text });
  };
  let i = 0, j = 0;
  while (i < n && j < m) {
    if (a[i] === b[j]) { push('same', a[i]); i++; j++; }
    else if (L[i + 1][j] >= L[i][j + 1]) { push('del', a[i]); i++; }
    else { push('add', b[j]); j++; }
  }
  while (i < n) push('del', a[i++]);
  while (j < m) push('add', b[j++]);
  return parts;
}

/** Diff por palavra (preserva espaços); cai para diff por linha em textos grandes. */
export function diffText(before: string, after: string): { parts: DiffPart[]; added: number; removed: number } {
  let a = before.split(/(\s+)/);
  let b = after.split(/(\s+)/);
  if (a.length * b.length > 2_500_000) {
    a = before.split(/(?<=\n)/);
    b = after.split(/(?<=\n)/);
  }
  const parts = a.length * b.length > 2_500_000
    ? [{ type: 'del' as const, text: before }, { type: 'add' as const, text: after }]
    : lcsDiff(a, b);
  const count = (t: DiffPart['type']) =>
    parts.filter(p => p.type === t).reduce((s, p) => s + p.text.split(/\s+/).filter(Boolean).length, 0);
  return { parts, added: count('add'), removed: count('del') };
}
