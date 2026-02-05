import { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/types/users';
import { apiFetch } from './api';

export const userService = {
  async getAll(): Promise<UserResponse[]> {
    return apiFetch<UserResponse[]>('/users');
  },

  async getById(id: string): Promise<UserResponse> {
    return apiFetch<UserResponse>(`/users/${id}`);
  },

  async create(data: CreateUserRequest): Promise<UserResponse> {
    return apiFetch<UserResponse>('/users', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  },

  async update(id: string, data: UpdateUserRequest): Promise<UserResponse> {
    return apiFetch<UserResponse>(`/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    });
  },

  async delete(id: string): Promise<void> {
    await apiFetch(`/users/${id}`, {
      method: 'DELETE'
    });
  }
};
