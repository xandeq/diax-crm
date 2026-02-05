import { apiFetch, setAccessToken } from './api';

export type LoginResponse = {
  accessToken: string;
  expiresAtUtc: string;
};

// Claim key emitida pelo .NET com ClaimTypes.Role
const JWT_ROLE_CLAIM = 'http://schemas.microsoft.com/identity/claims/roles';

/**
 * Decodifica o payload do JWT e extrai os roles.
 * Não valida assinatura — isso é responsabilidade do backend.
 */
export function decodeRoles(token: string): string[] {
  try {
    const payload = token.split('.')[1];
    if (!payload) return [];
    const decoded = JSON.parse(atob(payload));
    const role = decoded[JWT_ROLE_CLAIM];
    if (!role) return [];
    return Array.isArray(role) ? role : [role];
  } catch {
    return [];
  }
}

export async function login(email: string, password: string) {
  const data = await apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  const token =
    data?.accessToken ||
    (data as any)?.AccessToken ||
    (data as any)?.token ||
    (data as any)?.access_token;

  if (!token) {
    throw new Error('Token não retornado pelo login.');
  }

  setAccessToken(token);
  return { ...data, accessToken: token };
}

export type MeResponse = {
  email: string;
  roles: string[];
};

export async function me() {
  return apiFetch<MeResponse>('/auth/me', {
    method: 'GET'
  });
}
