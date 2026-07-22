/**
 * Converte o HTML de um briefing (email-safe, tabelas + inline styles) em texto
 * pronto para colar no WhatsApp, e quebra o briefing em blocos copiáveis.
 *
 * Puro string/regex — SEM DOM (roda igual no server e no client, testável em node).
 *
 * Formatação WhatsApp:
 *   *negrito*  _itálico_  ~riscado~  • lista
 * URLs sempre com esquema (https:// | http:// | www.) para virarem link clicável.
 */

export interface BriefingSection {
  /** Título do bloco (atributo data-wa). Vazio quando o briefing não tem marcação. */
  title: string;
  /** HTML bruto daquele bloco. */
  html: string;
}

/* ─────────────────────────── entidades HTML ─────────────────────────── */

const NAMED_ENTITIES: Record<string, string> = {
  amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", nbsp: ' ',
  mdash: '—', ndash: '–', hellip: '…', bull: '•', middot: '·',
  rarr: '→', larr: '←', copy: '©', reg: '®', trade: '™',
  laquo: '«', raquo: '»', deg: '°', euro: '€', pound: '£', cent: '¢',
  ldquo: '“', rdquo: '”', lsquo: '‘', rsquo: '’', times: '×', check: '✓',
};

export function decodeEntities(input: string): string {
  return input
    .replace(/&#x([0-9a-f]+);/gi, (_m, h) => safeCodePoint(parseInt(h, 16)))
    .replace(/&#(\d+);/g, (_m, d) => safeCodePoint(parseInt(d, 10)))
    .replace(/&([a-z][a-z0-9]*);/gi, (m, name) => {
      const key = String(name).toLowerCase();
      return Object.prototype.hasOwnProperty.call(NAMED_ENTITIES, key) ? NAMED_ENTITIES[key] : m;
    });
}

function safeCodePoint(code: number): string {
  if (!Number.isFinite(code) || code < 0 || code > 0x10ffff) return '';
  try { return String.fromCodePoint(code); } catch { return ''; }
}

/* ─────────────────────────────── URLs ──────────────────────────────── */

/**
 * Garante que uma URL comece com https:// | http:// | www. (WhatsApp linkifica).
 * Vazio/ancora/js → string vazia (ignorar).
 */
export function normalizeUrl(raw: string): string {
  const href = (raw || '').trim();
  if (!href) return '';
  if (/^(mailto:|tel:)/i.test(href)) return href;
  if (/^(javascript:|#)/i.test(href)) return '';
  if (/^https?:\/\//i.test(href)) return href;      // já tem esquema
  if (/^\/\//.test(href)) return 'https:' + href;   // protocol-relative
  if (/^www\./i.test(href)) return 'https://' + href;
  // domínio/caminho nu → assume https
  return 'https://' + href.replace(/^\/+/, '');
}

function stripTags(s: string): string {
  return s.replace(/<[^>]+>/g, '');
}

// TLDs "reais" — evita transformar nomes de arquivo (app.py, README.md) em URL.
const URL_TLDS = 'com|com\\.br|br|dev|ai|io|net|org|app|co|gov|edu|me|xyz|tech|info|site|online';

/**
 * Prefixa `https://` em URLs "nuas" em texto puro (ex.: `anthropic.com/news` que a
 * rotina imprime "por extenso"). Não toca em emails, em URLs já com esquema, nem em
 * tokens que não terminem num TLD conhecido.
 */
export function fixBareUrls(s: string): string {
  const re = new RegExp(
    `(?<![\\w/@.])((?:[a-z0-9][a-z0-9-]*\\.)+(?:${URL_TLDS}))(/[^\\s)\\]]*)?`,
    'gi',
  );
  return s.replace(re, (m) => {
    const trail = m.match(/[.,;:!?]+$/)?.[0] ?? '';
    const core = trail ? m.slice(0, -trail.length) : m;
    return 'https://' + core + trail;
  });
}

/* ──────────────────────────── HTML → WhatsApp ───────────────────────── */

export function htmlToWhatsApp(html: string): string {
  if (!html) return '';
  let s = html;

  // 0. remove ruído invisível
  s = s.replace(/<!--[\s\S]*?-->/g, '');
  s = s.replace(/<(style|script|head)\b[\s\S]*?<\/\1>/gi, '');

  // 1. links PRIMEIRO — "texto: URL" (ou só a URL quando não há texto útil)
  s = s.replace(
    /<a\b[^>]*?href=(["'])(.*?)\1[^>]*>([\s\S]*?)<\/a>/gi,
    (_m, _q, href, inner) => {
      const url = normalizeUrl(decodeEntities(String(href)));
      const text = decodeEntities(stripTags(String(inner))).replace(/\s+/g, ' ').trim();
      if (!url) return text;
      if (!text || text === url) return url;
      // texto que já É a própria URL (com ou sem esquema) → não duplicar
      if (!/\s/.test(text) && normalizeUrl(text) === url) return url;
      if (text.includes(url)) return text;
      return `${text}: ${url}`;
    },
  );

  // 2. negrito (strong/b/th) → *texto*
  s = s.replace(/<(strong|b|th)\b[^>]*>([\s\S]*?)<\/\1>/gi, (_m, _t, inner) => {
    const t = stripTags(String(inner)).replace(/\s+/g, ' ').trim();
    return t ? `*${t}*` : '';
  });

  // 3. itálico (em/i) → _texto_
  s = s.replace(/<(em|i)\b[^>]*>([\s\S]*?)<\/\1>/gi, (_m, _t, inner) => {
    const t = stripTags(String(inner)).replace(/\s+/g, ' ').trim();
    return t ? `_${t}_` : '';
  });

  // 4. riscado (s/del/strike) → ~texto~
  s = s.replace(/<(s|del|strike)\b[^>]*>([\s\S]*?)<\/\1>/gi, (_m, _t, inner) => {
    const t = stripTags(String(inner)).replace(/\s+/g, ' ').trim();
    return t ? `~${t}~` : '';
  });

  // 5. títulos → \n*TÍTULO*\n
  s = s.replace(/<h[1-6]\b[^>]*>([\s\S]*?)<\/h[1-6]>/gi, (_m, inner) => {
    const t = stripTags(String(inner)).replace(/\s+/g, ' ').trim();
    return t ? `\n*${t}*\n` : '\n';
  });

  // 6. itens de lista → "• item"
  s = s.replace(/<li\b[^>]*>([\s\S]*?)<\/li>/gi, (_m, inner) => {
    const t = stripTags(String(inner)).replace(/\s+/g, ' ').trim();
    return t ? `\n• ${t}` : '';
  });

  // 7. fronteiras de bloco → quebra de linha
  s = s.replace(/<br\s*\/?>/gi, '\n');
  s = s.replace(/<\/(td|th)>/gi, ' ');                                   // células na mesma linha
  s = s.replace(/<\/(p|div|tr|table|ul|ol|h[1-6]|section|blockquote|article)>/gi, '\n');

  // 8. remove qualquer tag restante
  s = stripTags(s);

  // 9. decodifica entidades
  s = decodeEntities(s);

  // 10. normaliza espaços: por linha colapsa espaços; no máx. 1 linha em branco
  s = s
    .split('\n')
    .map((line) => line.replace(/[ \t ]+/g, ' ').trim())
    .join('\n')
    .replace(/\n{3,}/g, '\n\n')
    .replace(/\n{2,}(?=• )/g, '\n') // bullets sempre coladas (lista compacta)
    .trim();

  // 11. URLs "nuas" em texto puro ganham esquema (https/http/www)
  s = fixBareUrls(s);

  return s;
}

/* ─────────────────────── split em blocos (data-wa) ─────────────────── */

const SECTION_RE = /<section\b[^>]*\bdata-wa=(["'])([\s\S]*?)\1[^>]*>([\s\S]*?)<\/section>/gi;

/**
 * Quebra o HTML por `<section data-wa="Título">…</section>`.
 * Sem marcação (briefings antigos) → 1 bloco único com todo o conteúdo.
 */
export function splitBriefingSections(html: string): BriefingSection[] {
  if (!html) return [];
  const out: BriefingSection[] = [];
  SECTION_RE.lastIndex = 0;
  let m: RegExpExecArray | null;
  while ((m = SECTION_RE.exec(html)) !== null) {
    out.push({ title: decodeEntities(m[2]).trim(), html: m[3] });
  }
  if (out.length === 0) return [{ title: '', html }];
  return out;
}

/** Texto WhatsApp de um bloco, prefixado pelo título quando houver. */
export function sectionToWhatsApp(section: BriefingSection): string {
  const body = htmlToWhatsApp(section.html);
  const title = (section.title || '').trim();
  if (!title) return body;
  // evita duplicar o título quando o corpo já começa por ele
  const firstLine = body.split('\n', 1)[0]?.replace(/[*_~]/g, '').trim().toLowerCase();
  if (firstLine && firstLine === title.toLowerCase()) return body;
  return `*${title}*\n${body}`.trim();
}

/** Briefing inteiro em texto WhatsApp (todos os blocos, separados por linha em branco). */
export function briefingToWhatsApp(html: string): string {
  return splitBriefingSections(html)
    .map(sectionToWhatsApp)
    .filter(Boolean)
    .join('\n\n')
    .replace(/\n{3,}/g, '\n\n')
    .trim();
}
