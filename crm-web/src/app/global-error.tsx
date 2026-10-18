'use client';

// Último recurso: erro no próprio layout raiz (AppShell/providers). Substitui o <html> inteiro,
// então não pode depender de CSS/componentes do app.
export default function GlobalError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <html lang="pt-BR">
      <body style={{ margin: 0, minHeight: '100vh', display: 'grid', placeItems: 'center', background: '#0b0f0d', color: '#e4e4e7', fontFamily: 'system-ui, sans-serif' }}>
        <div style={{ textAlign: 'center', padding: 24, maxWidth: 420 }}>
          <h1 style={{ fontSize: 18, margin: '0 0 8px' }}>O CRM encontrou um erro</h1>
          <p style={{ fontSize: 14, color: '#a1a1aa', margin: '0 0 20px' }}>Recarregue a página. Os dados não foram alterados.</p>
          <button onClick={reset} style={{ padding: '8px 16px', borderRadius: 8, border: '1px solid #3f3f46', background: 'transparent', color: '#e4e4e7', cursor: 'pointer', marginRight: 8 }}>
            Tentar de novo
          </button>
          <a href="/dashboard/" style={{ color: '#2dd4bf', fontSize: 14 }}>Ir ao dashboard</a>
        </div>
      </body>
    </html>
  );
}
