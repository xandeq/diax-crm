import { apiFetch } from './api';
import type {
  BlogPost,
  CreateBlogPostRequest,
  UpdateBlogPostRequest,
  BlogFilters,
  PagedBlogResponse
} from '@/types/blog';

export const blogService = {
  // Admin - Listagem
  getAll: async (filters: BlogFilters): Promise<PagedBlogResponse> => {
    const params = new URLSearchParams();
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());
    if (filters.search) params.append('search', filters.search);
    if (filters.status !== undefined) params.append('status', filters.status.toString());
    if (filters.category) params.append('category', filters.category);

    return apiFetch<PagedBlogResponse>(`/blog/admin?${params.toString()}`);
  },

  // Admin - CRUD
  getById: async (id: string): Promise<BlogPost> => {
    return apiFetch<BlogPost>(`/blog/admin/${id}`);
  },

  create: async (data: CreateBlogPostRequest): Promise<BlogPost> => {
    return apiFetch<BlogPost>('/blog/admin', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
  },

  update: async (id: string, data: UpdateBlogPostRequest): Promise<BlogPost> => {
    return apiFetch<BlogPost>(`/blog/admin/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
  },

  delete: async (id: string): Promise<void> => {
    return apiFetch<void>(`/blog/admin/${id}`, {
      method: 'DELETE'
    });
  },

  // Admin - Ações
  publish: async (id: string): Promise<BlogPost> => {
    return apiFetch<BlogPost>(`/blog/admin/${id}/publish`, {
      method: 'PATCH'
    });
  },

  archive: async (id: string): Promise<BlogPost> => {
    return apiFetch<BlogPost>(`/blog/admin/${id}/archive`, {
      method: 'PATCH'
    });
  },

  toggleFeatured: async (id: string): Promise<BlogPost> => {
    return apiFetch<BlogPost>(`/blog/admin/${id}/toggle-featured`, {
      method: 'PATCH'
    });
  }
};
