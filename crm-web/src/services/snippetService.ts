import { apiFetch } from './api';

export type SnippetLanguage = 'text' | 'csharp' | 'sql' | 'json' | 'markdown';

export interface CreateSnippetRequest {
  title: string;
  content: string;
  language: SnippetLanguage | string;
  isPublic: boolean;
  expiresAt?: string | null;
}

export interface SnippetResponse {
  id: string;
  title: string;
  content: string;
  language: string;
  isPublic: boolean;
  createdAt: string;
}

class SnippetService {
  async createSnippet(request: CreateSnippetRequest): Promise<{ id: string }> {
    return apiFetch<{ id: string }>('/snippets', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async getSnippets(): Promise<SnippetResponse[]> {
    return apiFetch<SnippetResponse[]>('/snippets');
  }

  async getSnippetById(id: string): Promise<SnippetResponse> {
    return apiFetch<SnippetResponse>(`/snippets/${id}`);
  }

  async getPublicSnippetById(id: string): Promise<SnippetResponse> {
    return apiFetch<SnippetResponse>(`/snippets/public/${id}`);
  }

  async deleteSnippet(id: string): Promise<void> {
    await apiFetch<void>(`/snippets/${id}`, { method: 'DELETE' });
  }
}

export const snippetService = new SnippetService();
