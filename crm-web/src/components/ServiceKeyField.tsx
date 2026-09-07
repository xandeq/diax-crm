'use client';

import { useEffect, useState } from 'react';

/**
 * Campo da Service API Key usado pelas páginas de documentação dos proxies de IA
 * (/tools/anthropic-proxy e /tools/openrouter-proxy).
 *
 * Por que isto existe: a chave dá acesso aos dois proxies e ambos gastam crédito real. Escrevê-la
 * no código das páginas — como era feito antes — publica a credencial em duas frentes ao mesmo
 * tempo, já que este repositório é público e as páginas também são servidas publicamente.
 *
 * O usuário cola a chave uma vez e ela fica apenas no localStorage do navegador dele. As duas
 * páginas compartilham a mesma entrada de armazenamento, então colar em uma vale para a outra.
 */
const KEY_STORAGE = 'diax-proxy-service-key';

/** Marcador exibido nos exemplos enquanto nenhuma chave foi informada. */
export const SERVICE_KEY_PLACEHOLDER = 'SUA_SERVICE_API_KEY';

export function useServiceKey() {
  const [serviceKey, setServiceKey] = useState('');
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    try {
      const saved = localStorage.getItem(KEY_STORAGE);
      if (saved) setServiceKey(saved);
    } catch {
      // modo privado ou storage bloqueado — a página funciona com o marcador
    }
    setLoaded(true);
  }, []);

  useEffect(() => {
    if (!loaded) return;
    try {
      if (serviceKey) localStorage.setItem(KEY_STORAGE, serviceKey);
      else localStorage.removeItem(KEY_STORAGE);
    } catch {
      // idem
    }
  }, [serviceKey, loaded]);

  return {
    serviceKey,
    setServiceKey,
    /** Valor a interpolar nos exemplos: a chave real, ou o marcador se ainda não houver. */
    keyOrPlaceholder: serviceKey || SERVICE_KEY_PLACEHOLDER,
  };
}

export function ServiceKeyField({
  serviceKey,
  setServiceKey,
  hint,
}: {
  serviceKey: string;
  setServiceKey: (v: string) => void;
  /** Texto exibido quando nenhuma chave foi informada. Específico de cada proxy. */
  hint: string;
}) {
  return (
    <>
      <p style={{ margin: '0 0 12px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
        Cole a chave uma vez. Ela fica salva <strong style={{ color: '#E5E7EB' }}>apenas no seu navegador</strong> e
        todos os exemplos desta página passam a mostrar a chave real, prontos para copiar.
      </p>
      <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 10 }}>
        <input
          type="password"
          value={serviceKey}
          onChange={(e) => setServiceKey(e.target.value.trim())}
          placeholder="Cole aqui a Service API Key do CRM"
          style={{
            flex: 1, padding: '10px 14px', borderRadius: 8,
            background: 'rgba(255,255,255,0.03)',
            border: `1px solid ${serviceKey ? 'rgba(16,185,129,0.4)' : 'rgba(255,255,255,0.1)'}`,
            color: '#E5E7EB', fontSize: 13, fontFamily: 'monospace', outline: 'none',
          }}
        />
        {serviceKey && (
          <button onClick={() => setServiceKey('')} style={{
            padding: '10px 14px', borderRadius: 8, cursor: 'pointer',
            background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)',
            color: '#9CA3AF', fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap',
          }}>Limpar</button>
        )}
      </div>
      <div style={{
        padding: '10px 14px', borderRadius: 8,
        background: serviceKey ? 'rgba(16,185,129,0.08)' : 'rgba(245,158,11,0.08)',
        border: `1px solid ${serviceKey ? 'rgba(16,185,129,0.2)' : 'rgba(245,158,11,0.2)'}`,
        fontSize: 12, color: serviceKey ? '#6EE7B7' : '#FCD34D', lineHeight: 1.6,
      }}>
        {serviceKey
          ? 'Chave salva no navegador. Os exemplos abaixo já estão prontos para copiar e usar. Vale também para a outra página de proxy.'
          : hint}
      </div>
    </>
  );
}
