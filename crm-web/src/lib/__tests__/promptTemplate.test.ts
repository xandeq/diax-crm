import { describe, expect, it } from 'vitest';
import { diffText, extractVariables, normalizeTag, ownValue, renderTemplate, segmentTemplate } from '../promptTemplate';

/**
 * Mesmos vetores de api-core/tests/Diax.Tests/Application/PromptLibrary/PromptTemplateTests.cs.
 * O render do cliente (preview/copiar) tem que bater com o do servidor (MCP/render).
 */
const RENDER_VECTORS: Array<[content: string, values: Record<string, string>, expected: string, missing: string[]]> = [
  ['Olá {{nome}}', { nome: 'Ana' }, 'Olá Ana', []],
  ['{{ nome }} e {{nome}}', { nome: 'Ana' }, 'Ana e Ana', []],
  ['Oi {{nome}}', {}, 'Oi {{nome}}', ['nome']],
  ['Literal \\{{nome}}', { nome: 'Ana' }, 'Literal {{nome}}', []],
  ['{{a}}{{b.c}}{{d-e}}', { a: '1', 'b.c': '2', 'd-e': '3' }, '123', []],
  ['{{1x}} fica', {}, '{{1x}} fica', []],
  ['Ação: {{ação}}', {}, 'Ação: {{ação}}', []],
  ['Emoji {{x}} 🚀', { x: '✓' }, 'Emoji ✓ 🚀', []],
  // Nomes que existem no Object.prototype: ausentes, nunca o valor herdado.
  ['{{constructor}} {{toString}} {{__proto__}}', {}, '{{constructor}} {{toString}} {{__proto__}}', ['constructor', 'toString', '__proto__']],
];

describe('renderTemplate (espelho do PromptTemplate.cs)', () => {
  it.each(RENDER_VECTORS)('%s', (content, values, expected, missing) => {
    const r = renderTemplate(content, values);
    expect(r.rendered).toBe(expected);
    expect(r.missing).toEqual(missing);
  });

  it('usa o default quando o valor está vazio', () => {
    expect(renderTemplate('Linguagem: {{lang}}', { lang: '' }, { lang: 'TypeScript' }).rendered).toBe('Linguagem: TypeScript');
  });

  it('preenche variável chamada constructor quando o valor é próprio', () => {
    const values: Record<string, string> = {};
    values['constructor'] = 'X';
    expect(renderTemplate('{{constructor}}', values).rendered).toBe('X');
  });
});

describe('extractVariables', () => {
  it('mantém a ordem da 1ª aparição e ignora escapadas', () => {
    expect(extractVariables('{{b}} {{a}} {{b}} \\{{c}}')).toEqual(['b', 'a']);
  });
});

describe('segmentTemplate', () => {
  it('reconstrói o texto original', () => {
    const text = 'A {{x}} B \\{{y}} C';
    expect(segmentTemplate(text).map(s => (s.kind === 'text' ? s.text : s.raw)).join('')).toBe(text);
  });
});

describe('normalizeTag', () => {
  it.each([
    [' Cold Email ', 'cold-email'],
    ['#Dev', 'dev'],
    ['  a   b  ', 'a-b'],
    ['   ', null],
    ['#', null],
  ])('%s → %s', (input, expected) => expect(normalizeTag(input)).toBe(expected));
});

describe('ownValue', () => {
  it('não devolve propriedades herdadas', () => {
    expect(ownValue({}, 'constructor')).toBeUndefined();
    expect(ownValue({ a: '1' }, 'a')).toBe('1');
  });
});

describe('diffText', () => {
  it('conta palavras adicionadas e removidas', () => {
    const d = diffText('um dois três', 'um três quatro');
    expect(d.added).toBe(1);
    expect(d.removed).toBe(1);
    expect(d.parts.map(p => p.text).join('').length).toBeGreaterThan(0);
  });
});
