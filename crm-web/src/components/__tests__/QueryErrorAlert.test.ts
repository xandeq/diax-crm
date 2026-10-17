import { createElement } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { QueryErrorAlert } from '../QueryErrorAlert';

describe('QueryErrorAlert', () => {
  it('mostra a mensagem do erro e o título', () => {
    const html = renderToStaticMarkup(
      createElement(QueryErrorAlert, { error: new Error('HTTP 500'), title: 'Falhou clientes' }),
    );

    expect(html).toContain('role="alert"');
    expect(html).toContain('Falhou clientes');
    expect(html).toContain('HTTP 500');
    expect(html).not.toContain('Tentar de novo');
  });

  it('mostra botão de retry só quando há onRetry', () => {
    const html = renderToStaticMarkup(
      createElement(QueryErrorAlert, { error: 'x', onRetry: () => {} }),
    );

    expect(html).toContain('Tentar de novo');
    expect(html).toContain('Erro desconhecido');
  });
});
