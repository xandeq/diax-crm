import { describe, expect, it } from 'vitest';
import {
  htmlToWhatsApp,
  normalizeUrl,
  decodeEntities,
  fixBareUrls,
  splitBriefingSections,
  sectionToWhatsApp,
  briefingToWhatsApp,
} from '../briefingWhatsapp';

describe('normalizeUrl', () => {
  it('mantém URLs com esquema', () => {
    expect(normalizeUrl('https://x.com')).toBe('https://x.com');
    expect(normalizeUrl('http://x.com')).toBe('http://x.com');
  });
  it('prefixa https em www e domínio nu', () => {
    expect(normalizeUrl('www.x.com')).toBe('https://www.x.com');
    expect(normalizeUrl('example.com/path')).toBe('https://example.com/path');
  });
  it('protocol-relative vira https', () => {
    expect(normalizeUrl('//cdn.x.com/a.png')).toBe('https://cdn.x.com/a.png');
  });
  it('mantém mailto/tel; descarta js/âncora', () => {
    expect(normalizeUrl('mailto:a@b.com')).toBe('mailto:a@b.com');
    expect(normalizeUrl('tel:+55')).toBe('tel:+55');
    expect(normalizeUrl('#top')).toBe('');
    expect(normalizeUrl('javascript:alert(1)')).toBe('');
  });
  it('nunca devolve URL sem https/http/www quando é link real', () => {
    for (const u of ['x.com', 'sub.dom.dev/a', 'www.a.b']) {
      const out = normalizeUrl(u);
      expect(/^(https?:\/\/|www\.)/.test(out)).toBe(true);
    }
  });
});

describe('decodeEntities', () => {
  it('nomeadas e numéricas', () => {
    expect(decodeEntities('a &amp; b')).toBe('a & b');
    expect(decodeEntities('&lt;tag&gt;')).toBe('<tag>');
    expect(decodeEntities('cost&nbsp;$5')).toBe('cost $5');
    expect(decodeEntities('&#39;x&#39;')).toBe("'x'");
    expect(decodeEntities('&#x2192;')).toBe('→');
    expect(decodeEntities('&mdash;')).toBe('—');
  });
  it('deixa entidade desconhecida intacta', () => {
    expect(decodeEntities('&unknownthing;')).toBe('&unknownthing;');
  });
});

describe('fixBareUrls', () => {
  it('prefixa https em URL nua de texto puro', () => {
    expect(fixBareUrls('Fonte: anthropic.com/news')).toBe('Fonte: https://anthropic.com/news');
    expect(fixBareUrls('veja claude.ai hoje')).toBe('veja https://claude.ai hoje');
    expect(fixBareUrls('docs.anthropic.com/en/api')).toBe('https://docs.anthropic.com/en/api');
  });
  it('não duplica esquema em URL já completa', () => {
    expect(fixBareUrls('https://anthropic.com/news')).toBe('https://anthropic.com/news');
    expect(fixBareUrls('www.anthropic.com/x')).toBe('https://www.anthropic.com/x');
  });
  it('não toca emails', () => {
    expect(fixBareUrls('contato@alexandrequeiroz.com.br')).toBe('contato@alexandrequeiroz.com.br');
  });
  it('não toca nomes de arquivo (TLD desconhecido)', () => {
    expect(fixBareUrls('rode app.py e README.md')).toBe('rode app.py e README.md');
  });
  it('pontuação final fica fora do link', () => {
    expect(fixBareUrls('em anthropic.com/news.')).toBe('em https://anthropic.com/news.');
    expect(fixBareUrls('fonte: anthropic.com/news:')).toBe('fonte: https://anthropic.com/news:');
  });
});

describe('htmlToWhatsApp — formatação', () => {
  it('strong/b → *negrito*', () => {
    expect(htmlToWhatsApp('<strong>Oi</strong>')).toBe('*Oi*');
    expect(htmlToWhatsApp('<b>Oi</b>')).toBe('*Oi*');
  });
  it('em/i → _itálico_', () => {
    expect(htmlToWhatsApp('<em>x</em>')).toBe('_x_');
  });
  it('heading → *TÍTULO* com quebra', () => {
    expect(htmlToWhatsApp('<h2>Notícias</h2>')).toBe('*Notícias*');
  });
  it('li → bullets', () => {
    const out = htmlToWhatsApp('<ul><li>Um</li><li>Dois</li></ul>');
    expect(out).toContain('• Um');
    expect(out).toContain('• Dois');
  });
  it('link → "texto: URL" com esquema', () => {
    expect(htmlToWhatsApp('<a href="www.vaga.dev">Vaga</a>')).toBe('Vaga: https://www.vaga.dev');
    expect(htmlToWhatsApp('<a href="https://a.com">https://a.com</a>')).toBe('https://a.com');
  });
  it('link sem esquema no href ganha https', () => {
    expect(htmlToWhatsApp('<a href="anthropic.com/news">News</a>')).toBe(
      'News: https://anthropic.com/news',
    );
  });
  it('link cujo texto já É a URL não duplica', () => {
    expect(htmlToWhatsApp('<a href="anthropic.com/x">anthropic.com/x</a>')).toBe(
      'https://anthropic.com/x',
    );
  });
  it('bullets ficam coladas (sem linha em branco entre elas)', () => {
    const out = htmlToWhatsApp('<ul>\n  <li>Um</li>\n  <li>Dois</li>\n</ul>');
    expect(out).toBe('• Um\n• Dois');
  });
  it('tabela: células viram linha única, linhas separadas', () => {
    const html = '<table><tr><td>Selic</td><td>15%</td></tr><tr><td>CDI</td><td>14,9%</td></tr></table>';
    const out = htmlToWhatsApp(html);
    expect(out).toContain('Selic 15%');
    expect(out).toContain('CDI 14,9%');
  });
  it('remove style/script e tags soltas', () => {
    const html = '<style>.x{color:red}</style><p>Olá <span style="x">mundo</span></p><script>evil()</script>';
    expect(htmlToWhatsApp(html)).toBe('Olá mundo');
  });
  it('colapsa múltiplas linhas em branco', () => {
    expect(htmlToWhatsApp('<p>A</p><p></p><p></p><p>B</p>')).toBe('A\n\nB');
  });
  it('não deixa URL de link perder o esquema', () => {
    const out = htmlToWhatsApp('<a href="cursor.com">Cursor</a> e <a href="www.jules.dev">Jules</a>');
    const urls = out.match(/\bhttps?:\/\/\S+|\bwww\.\S+/g) ?? [];
    expect(urls.length).toBeGreaterThan(0);
    for (const u of urls) expect(/^(https?:\/\/|www\.)/.test(u)).toBe(true);
  });
});

describe('splitBriefingSections', () => {
  it('quebra por data-wa', () => {
    const html =
      '<section data-wa="TL;DR"><p>a</p></section><section data-wa="Notícias"><p>b</p></section>';
    const secs = splitBriefingSections(html);
    expect(secs).toHaveLength(2);
    expect(secs[0].title).toBe('TL;DR');
    expect(secs[1].title).toBe('Notícias');
    expect(secs[0].html).toContain('<p>a</p>');
  });
  it('fallback: sem data-wa → 1 bloco com todo o conteúdo', () => {
    const html = '<div><p>legado</p></div>';
    const secs = splitBriefingSections(html);
    expect(secs).toHaveLength(1);
    expect(secs[0].title).toBe('');
    expect(secs[0].html).toBe(html);
  });
  it('title com entidade é decodificado', () => {
    const secs = splitBriefingSections('<section data-wa="Papers &amp; Modelos"><p>x</p></section>');
    expect(secs[0].title).toBe('Papers & Modelos');
  });
  it('string vazia → []', () => {
    expect(splitBriefingSections('')).toEqual([]);
  });
});

describe('sectionToWhatsApp / briefingToWhatsApp', () => {
  it('prefixa título quando o corpo não começa por ele', () => {
    const out = sectionToWhatsApp({ title: 'Notícias', html: '<p>Claude lançou X</p>' });
    expect(out).toBe('*Notícias*\nClaude lançou X');
  });
  it('não duplica título quando o corpo já o traz', () => {
    const out = sectionToWhatsApp({ title: 'Notícias', html: '<h2>Notícias</h2><p>corpo</p>' });
    expect(out.match(/Notícias/g)?.length).toBe(1);
  });
  it('briefing inteiro junta blocos com linha em branco', () => {
    const html =
      '<section data-wa="A"><p>um</p></section><section data-wa="B"><p>dois</p></section>';
    const out = briefingToWhatsApp(html);
    expect(out).toBe('*A*\num\n\n*B*\ndois');
  });
});
