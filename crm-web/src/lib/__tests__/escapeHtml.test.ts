import { describe, expect, it } from 'vitest';
import { escapeHtml } from '../utils';

describe('escapeHtml', () => {
  it('neutraliza tag injetada em dado de lead', () => {
    expect(escapeHtml('<img src=x onerror=alert(1)>')).toBe('&lt;img src=x onerror=alert(1)&gt;');
  });
  it('mantém acentos e escapa aspas/&', () => {
    expect(escapeHtml(`José & "Filhos" 'Ltda'`)).toBe('José &amp; &quot;Filhos&quot; &#39;Ltda&#39;');
  });
});
