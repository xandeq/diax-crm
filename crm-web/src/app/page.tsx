export default function HomePage() {
  return (
    <main className="card">
      <h1 style={{ marginTop: 0 }}>Painel CRM</h1>
      <p>
        Este é o frontend do CRM (Next.js export estático) consumindo a API em{' '}
        <code>{process.env.NEXT_PUBLIC_API_BASE_URL ?? 'N/A'}</code>.
      </p>
      <div className="row">
        <a className="button" href="/login/">Entrar</a>
        <a className="button secondary" href="/dashboard/">Ir ao dashboard</a>
      </div>
    </main>
  );
}
