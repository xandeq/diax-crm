// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { sanitizeHtml } from '../sanitizeHtml';

describe('sanitizeHtml', () => {
  it('remove <script>', () => {
    const out = sanitizeHtml('<p>oi</p><script>alert(1)</script>');
    expect(out).not.toContain('<script');
    expect(out).not.toContain('alert');
    expect(out).toContain('<p>oi</p>');
  });

  it('remove <img onerror>', () => {
    const out = sanitizeHtml('<img src="x" onerror="alert(1)">');
    expect(out).not.toContain('onerror');
  });

  it('mantem imagem e tabela de email (prévia do composer)', () => {
    const out = sanitizeHtml('<table width="600" align="center" bgcolor="#fff"><tr><td><img src="https://api.alexandrequeiroz.com.br/x.jpg" width="600" alt="nicho"></td></tr></table>');
    expect(out).toContain('src="https://api.alexandrequeiroz.com.br/x.jpg"');
    expect(out).toContain('width="600"');
    expect(out).toContain('<table');
  });

  it('remove <a href="javascript:...">', () => {
    const out = sanitizeHtml('<a href="javascript:alert(1)">clique</a>');
    expect(out).not.toContain('javascript:');
  });

  it('remove <iframe>', () => {
    const out = sanitizeHtml('<iframe src="https://evil.com"></iframe>');
    expect(out).not.toContain('<iframe');
  });

  it('mantem <b>', () => {
    const out = sanitizeHtml('<b>negrito</b>');
    expect(out).toContain('<b>negrito</b>');
  });

  it('mantem <a href> https com rel noopener', () => {
    const out = sanitizeHtml('<a href="https://exemplo.com">link</a>');
    expect(out).toContain('href="https://exemplo.com"');
    expect(out).toContain('rel="noopener noreferrer"');
    expect(out).toContain('target="_blank"');
  });
});
