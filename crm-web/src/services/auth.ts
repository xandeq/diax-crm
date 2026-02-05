import { LoginResponse, MeResponse } from '@/types/auth';
import { apiFetch, setAccessToken } from './api';

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

export async function me() {
  return apiFetch<MeResponse>('/auth/me', {
    method: 'GET'
  });
}
