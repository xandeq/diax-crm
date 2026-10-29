// Sanitiza HTML vindo de fontes externas (IA, briefings por e-mail, APIs) antes
// de usar em dangerouslySetInnerHTML. Export estático: só roda no cliente.
import DOMPurify from 'dompurify';

let configured = false;

function configure(purify: typeof DOMPurify) {
  if (configured) return;
  configured = true;
  purify.addHook('afterSanitizeAttributes', (node) => {
    if (node.tagName === 'A') {
      node.setAttribute('target', '_blank');
      node.setAttribute('rel', 'noopener noreferrer');
    }
  });
}

/**
 * Sanitiza HTML externo (briefings, respostas de IA) removendo scripts,
 * handlers on* e URLs javascript:. Mantém formatação, imagens e tabelas (prévia de email).
 * Em SSR/build (sem `window`) retorna string vazia para não quebrar `next build`.
 */
export function sanitizeHtml(html: string): string {
  if (typeof window === 'undefined') {
    return '';
  }
  if (!html) {
    return '';
  }
  configure(DOMPurify);
  // Perfil HTML padrão do DOMPurify: mantém o que email/briefing usa (img, tabelas, style, width, align,
  // bgcolor) e remove script, iframe, handlers on* e URLs javascript:/data: perigosas.
  return DOMPurify.sanitize(html, {
    USE_PROFILES: { html: true },
    ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto|cid):|[^a-z]|[a-z+.-]+(?:[^a-z+.:-]|$))/i,
  });
}
