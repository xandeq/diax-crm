export function getApiBaseUrl(): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!base) throw new Error('NEXT_PUBLIC_API_BASE_URL is not set');
  return base.replace(/\/$/, '');
}

export function getAccessToken(): string | null {
  if (typeof window === 'undefined') return null;
  return sessionStorage.getItem('accessToken');
}

export function setAccessToken(token: string) {
  if (typeof window === 'undefined') return;
  sessionStorage.setItem('accessToken', token);
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const url = `${getApiBaseUrl()}${path.startsWith('/') ? '' : '/'}${path}`;

  const headers = new Headers(init.headers);
  headers.set('Content-Type', 'application/json');

  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const res = await fetch(url, {
    ...init,
    headers,
    credentials: 'include'
  });

  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(`HTTP ${res.status}: ${text}`);
  }

  return (await res.json()) as T;
}
