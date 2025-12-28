'use client';

import { useState } from 'react';
import { login } from '@/services/auth';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await login(email, password);
      window.location.href = '/dashboard/';
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="card" style={{ maxWidth: 520 }}>
      <h1 style={{ marginTop: 0 }}>Login</h1>
      <p style={{ marginTop: 0 }}>
        Autenticação via API: <code>{process.env.NEXT_PUBLIC_API_BASE_URL ?? 'N/A'}</code>
      </p>

      <form onSubmit={onSubmit}>
        <label>Email</label>
        <input
          className="input"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="admin@seu-dominio.com"
          autoComplete="username"
        />

        <label style={{ display: 'block', marginTop: 12 }}>Senha</label>
        <input
          className="input"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="••••••••"
          autoComplete="current-password"
        />

        <div style={{ marginTop: 16 }} className="row">
          <button className="button" type="submit" disabled={loading}>
            {loading ? 'Entrando…' : 'Entrar'}
          </button>
          <a className="button secondary" href="/">Voltar</a>
        </div>

        {error && <p className="error">{error}</p>}
      </form>
    </main>
  );
}
