import { getAccessToken, getApiBaseUrl } from './api';

// ── Gateway de IAs gratuitas (/free-ai/v1) ───────────────────────────────────
// Fica fora de /api/v1 (mesmo padrão dos proxies /openrouter e /proxy). Apps externos usam a
// ProxyApiKey no header X-Api-Key; aqui no CRM a chamada vai com o login do usuário (Bearer).

export interface FreeAiAttempt {
  provider: string;
  model: string;
  status: string;
  error?: string | null;
  elapsedMs: number;
}

export interface FreeAiHealthItem {
  provider: string;
  model: string;
  ok: boolean;
  elapsedMs: number;
  error?: string | null;
}

export interface FreeAiHealth {
  ok: number;
  total: number;
  cached: boolean;
  checkedAt: string;
  items: FreeAiHealthItem[];
}

export interface FreeAiChatResponse {
  id: string;
  model: string;
  choices: { index: number; message: { role: string; content: string }; finish_reason: string }[];
  usage: { prompt_tokens: number; completion_tokens: number; total_tokens: number };
  x_diax: { provider: string; model: string; attempts: FreeAiAttempt[] };
}

/** https://api.../free-ai/v1 — derivado da mesma base da API do CRM. */
export function getFreeAiBaseUrl(): string {
  return `${getApiBaseUrl().replace(/\/api\/v1$/, '')}/free-ai/v1`;
}

async function freeAiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getAccessToken();
  const res = await fetch(`${getFreeAiBaseUrl()}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers ?? {}),
    },
  });
  const text = await res.text();
  let body: unknown = null;
  try { body = text ? JSON.parse(text) : null; } catch { /* corpo não-JSON */ }
  if (!res.ok) {
    const msg = (body as { error?: string } | null)?.error
      ?? (res.status === 404 ? 'Endpoint ainda não publicado (aguardando deploy da API).' : `HTTP ${res.status}`);
    const err = new Error(msg) as Error & { status?: number; body?: unknown };
    err.status = res.status;
    err.body = body;
    throw err;
  }
  return body as T;
}

export async function getFreeAiHealth(refresh = false): Promise<FreeAiHealth> {
  return freeAiFetch<FreeAiHealth>(`/health${refresh ? '?refresh=true' : ''}`, { method: 'GET' });
}

export async function listFreeAiModels(): Promise<string[]> {
  const r = await freeAiFetch<{ data: { id: string }[] }>('/models', { method: 'GET' });
  return r.data.map(m => m.id);
}

export async function testFreeAi(prompt: string, system?: string, model = 'auto'): Promise<FreeAiChatResponse> {
  const messages = [
    ...(system?.trim() ? [{ role: 'system', content: system.trim() }] : []),
    { role: 'user', content: prompt },
  ];
  return freeAiFetch<FreeAiChatResponse>('/chat/completions', {
    method: 'POST',
    body: JSON.stringify({ model, messages, max_tokens: 1200 }),
  });
}
