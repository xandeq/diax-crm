import { apiFetch, setAccessToken } from './api';

export type LoginResponse = {
  accessToken: string;
  expiresAtUtc: string;
};

export async function login(email: string, password: string) {
  const data = await apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });

  if (!data?.accessToken) {
    throw new Error('Token não retornado pelo login.');
  }

  setAccessToken(data.accessToken);
  return data;
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
