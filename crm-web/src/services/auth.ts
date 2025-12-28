import { apiFetch, setAccessToken } from './api';

export type LoginResponse = {
  accessToken: string;
  expiresAtUtc: string;
};

export async function login(email: string, password: string) {
  const data = await apiFetch<LoginResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  setAccessToken(data.accessToken);
  return data;
}

export type MeResponse = {
  email: string;
  roles: string[];
};

export async function me() {
  return apiFetch<MeResponse>('/api/v1/auth/me', {
    method: 'GET'
  });
}
