export type UserRole = 'Admin' | 'User';

export interface User {
  email: string;
  role: UserRole;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  token?: string; // fallback
}

export interface MeResponse {
  email: string;
  roles: string[];
}
