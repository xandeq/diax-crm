'use client';

import { useEffect, useState } from 'react';
import { me, MeResponse } from '@/services/auth';

export default function DashboardPage() {
  const [data, setData] = useState<MeResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const result = await me();
        setData(result);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Erro desconhecido');
      }
    })();
  }, []);

  return (
    <main className="card">
      <h1 style={{ marginTop: 0 }}>Dashboard</h1>

      {!data && !error && <p>Carregando…</p>}
      {error && (
        <div>
          <p className="error">Falha ao carregar /auth/me: {error}</p>
          <p>
            Dica: faça login em <a href="/login/">/login</a> primeiro.
          </p>
        </div>
      )}

      {data && (
        <div>
          <p>
            Logado como: <strong>{data.email}</strong>
          </p>
          <p>Roles: {data.roles.join(', ') || 'nenhuma'}</p>
        </div>
      )}
    </main>
  );
}
