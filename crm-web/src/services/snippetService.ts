import { apiFetch, apiFetchRaw } from './api';

export type SnippetLanguage = 'text' | 'csharp' | 'sql' | 'json' | 'markdown';

export interface SnippetAttachment {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface CreateSnippetRequest {
  title: string;
  content?: string;
  language: SnippetLanguage | string;
  isPublic: boolean;
  expiresAt?: string | null;
  files?: File[];
}

export interface SnippetResponse {
  id: string;
  title: string;
  content: string;
  language: string;
  isPublic: boolean;
  createdAt: string;
  attachments: SnippetAttachment[];
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

class SnippetService {
  async createSnippet(request: CreateSnippetRequest): Promise<{ id: string }> {
    const hasFiles = request.files && request.files.length > 0;

    if (hasFiles) {
      const form = new FormData();
      form.append('title', request.title);
      if (request.content) form.append('content', request.content);
      form.append('language', request.language);
      form.append('isPublic', String(request.isPublic));
      if (request.expiresAt) form.append('expiresAt', request.expiresAt);
      for (const file of request.files!) {
        form.append('files', file);
      }
      return apiFetch<{ id: string }>('/snippets', { method: 'POST', body: form });
    }

    const form = new FormData();
    form.append('title', request.title);
    if (request.content) form.append('content', request.content);
    form.append('language', request.language);
    form.append('isPublic', String(request.isPublic));
    if (request.expiresAt) form.append('expiresAt', request.expiresAt);
    return apiFetch<{ id: string }>('/snippets', { method: 'POST', body: form });
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

  async addAttachment(snippetId: string, file: File): Promise<{ id: string }> {
    const form = new FormData();
    form.append('file', file);
    return apiFetch<{ id: string }>(`/snippets/${snippetId}/attachments`, {
      method: 'POST',
      body: form
    });
  }

  async deleteAttachment(snippetId: string, attachmentId: string): Promise<void> {
    await apiFetch<void>(`/snippets/${snippetId}/attachments/${attachmentId}`, {
      method: 'DELETE'
    });
  }

  buildDownloadUrl(snippetId: string, attachmentId: string, isPublic = false): string {
    const base = process.env.NEXT_PUBLIC_API_BASE_URL ?? '';
    if (isPublic) {
      return `${base}/api/v1/snippets/public/${snippetId}/attachments/${attachmentId}/download`;
    }
    return `${base}/api/v1/snippets/${snippetId}/attachments/${attachmentId}/download`;
  }

  async downloadAttachment(snippetId: string, attachmentId: string, fileName: string, isPublic = false): Promise<void> {
    if (isPublic) {
      const url = this.buildDownloadUrl(snippetId, attachmentId, true);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
      return;
    }

    const res = await apiFetchRaw(`/snippets/${snippetId}/attachments/${attachmentId}/download`);
    if (!res.ok) throw new Error(`Erro ao baixar arquivo: HTTP ${res.status}`);

    const blob = await res.blob();
    const objectUrl = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = objectUrl;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(objectUrl);
  }
}

export const snippetService = new SnippetService();
export { formatBytes };
