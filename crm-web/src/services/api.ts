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
  const token = sessionStorage.getItem('accessToken');
  if (!token || token === 'undefined' || token === 'null') return null;
  return token;
}

export function setAccessToken(token: string) {
  if (typeof window === 'undefined') return;
  if (!token || token === 'undefined' || token === 'null') return;
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

  // Only set Content-Type to application/json if it's not already set
  // and we are not sending FormData (browser handles FormData boundary automatically)
  if (!headers.has('Content-Type') && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;

  try {
    res = await fetch(url, {
      ...init,
      headers,
      credentials: 'include'
    });
  } catch (error) {
    // Network-level failure (CORS, connection refused, DNS failure, etc.)
    const message = error instanceof Error ? error.message : 'Network error';
    console.error('Network request failed:', error);

    // Provide user-friendly error message
    if (message.includes('Failed to fetch') || message.includes('NetworkError')) {
      throw new ApiError(0, 'Não foi possível conectar ao servidor. Verifique sua conexão ou tente novamente.');
    }
    throw new ApiError(0, `Erro de rede: ${message}`);
  }

  if (!res.ok) {
    // Intercept 401 Unauthorized globally
    if (res.status === 401 && typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent('auth:expired'));
    }

    let text = '';
    try {
      text = await res.text();
    } catch {
      // If we can't even read the error response, throw a generic error
      throw new ApiError(res.status, `HTTP ${res.status}`);
    }

    let errorMessage = `HTTP ${res.status}`;
    let errorCode: string | undefined;

    if (text && text.trim() !== '') {
      try {
        const json = JSON.parse(text);
        // Prioritize 'message' field from API error response
        if (json.message) errorMessage = json.message;
        if (json.code) errorCode = json.code;
      } catch {
        // If parsing fails, use the raw text if available
        errorMessage = text;
      }
    }

    throw new ApiError(res.status, errorMessage, errorCode);
  }

  // Handle empty responses gracefully
  const contentLength = res.headers.get('content-length');
  if (contentLength === '0' || res.status === 204) {
    return {} as T;
  }

  // Read as text first to handle potential empty body
  let text = '';
  try {
    text = await res.text();
  } catch (error) {
    console.error('Failed to read response body:', error);
    throw new ApiError(res.status, 'Erro ao ler resposta do servidor');
  }

  if (!text || text.trim() === '') {
    return {} as T;
  }

  try {
    return JSON.parse(text) as T;
  } catch (e) {
    console.error('Failed to parse JSON response:', text);
    throw new ApiError(res.status, `Resposta inválida do servidor: ${text.substring(0, 100)}`);
  }
}

export async function apiRequest<T>(url: string, method: string, data?: any): Promise<T> {
  const init: RequestInit = { method };
  if (data !== undefined) {
    init.body = JSON.stringify(data);
  }
  return await apiFetch<T>(url, init);
}

