export function getApiBaseUrl(): string {
  const base = process.env.NEXT_PUBLIC_API_BASE_URL;
  if (!base) throw new Error('NEXT_PUBLIC_API_BASE_URL is not set');
  const cleanBase = base.replace(/\/$/, '');
  // Ensure the base URL ends with /api/v1
  if (!cleanBase.endsWith('/api/v1')) {
    return `${cleanBase}/api/v1`;
  }
  return cleanBase;
}

export function getAccessToken(): string | null {
  if (typeof window === 'undefined') return null;
  return sessionStorage.getItem('accessToken');
}

export function setAccessToken(token: string) {
  if (typeof window === 'undefined') return;
  sessionStorage.setItem('accessToken', token);
}

export class ApiError extends Error {
  status: number;
  code?: string;

  constructor(status: number, message: string, code?: string) {
    super(message);
    this.status = status;
    this.code = code;
    this.name = 'ApiError';
  }
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
    let errorMessage = `HTTP ${res.status}`;
    let errorCode: string | undefined;

    try {
      const json = JSON.parse(text);
      // Prioritize 'message' field from API error response
      if (json.message) errorMessage = json.message;
      if (json.code) errorCode = json.code;
    } catch {
      // If parsing fails, use the raw text if available
      if (text) errorMessage = text;
    }

    throw new ApiError(res.status, errorMessage, errorCode);
  }

  return (await res.json()) as T;
}
