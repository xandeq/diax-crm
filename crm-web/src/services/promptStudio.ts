import type {
  AddVersionInput,
  CreatePromptInput,
  PromptCollection,
  PromptDetail,
  PromptListItem,
  PromptTagCount,
  PromptVersion,
  PromptVersionSummary,
  UpdatePromptInput,
} from '@/types/promptStudio';
import { apiFetch } from './api';

const json = (body: unknown): RequestInit => ({ body: JSON.stringify(body) });

export const promptStudioService = {
  /** Índice completo (não arquivados por padrão) — o filtro fino acontece no cliente. */
  list(params: { archived?: boolean; q?: string } = {}): Promise<PromptListItem[]> {
    const qs = new URLSearchParams();
    if (params.archived) qs.set('archived', 'true');
    if (params.q) qs.set('q', params.q);
    const s = qs.toString();
    return apiFetch<PromptListItem[]>(`/prompts${s ? `?${s}` : ''}`);
  },

  get(id: string): Promise<PromptDetail> {
    return apiFetch<PromptDetail>(`/prompts/${id}`);
  },

  create(input: CreatePromptInput): Promise<PromptDetail> {
    return apiFetch<PromptDetail>('/prompts', { method: 'POST', ...json(input) });
  },

  update(id: string, input: UpdatePromptInput): Promise<PromptDetail> {
    return apiFetch<PromptDetail>(`/prompts/${id}`, { method: 'PATCH', ...json(input) });
  },

  remove(id: string): Promise<void> {
    return apiFetch<void>(`/prompts/${id}`, { method: 'DELETE' });
  },

  duplicate(id: string): Promise<PromptDetail> {
    return apiFetch<PromptDetail>(`/prompts/${id}/duplicate`, { method: 'POST' });
  },

  registerUse(id: string): Promise<void> {
    return apiFetch<void>(`/prompts/${id}/use`, { method: 'POST' });
  },

  versions(id: string): Promise<PromptVersionSummary[]> {
    return apiFetch<PromptVersionSummary[]>(`/prompts/${id}/versions`);
  },

  version(id: string, number: number): Promise<PromptVersion> {
    return apiFetch<PromptVersion>(`/prompts/${id}/versions/${number}`);
  },

  /** 201 = nova versão; 200 = conteúdo idêntico (nada criado); 409 = conflito (ApiError.status). */
  addVersion(id: string, input: AddVersionInput): Promise<PromptVersion> {
    return apiFetch<PromptVersion>(`/prompts/${id}/versions`, { method: 'POST', ...json(input) });
  },

  restore(id: string, number: number): Promise<PromptVersion> {
    return apiFetch<PromptVersion>(`/prompts/${id}/versions/${number}/restore`, { method: 'POST' });
  },

  tags(): Promise<PromptTagCount[]> {
    return apiFetch<PromptTagCount[]>('/prompts/tags');
  },

  fromGenerator(userPromptId: string): Promise<PromptDetail> {
    return apiFetch<PromptDetail>(`/prompts/from-generator/${userPromptId}`, { method: 'POST' });
  },

  fromSnippet(snippetId: string): Promise<PromptDetail> {
    return apiFetch<PromptDetail>(`/prompts/from-snippet/${snippetId}`, { method: 'POST' });
  },

  collections(): Promise<PromptCollection[]> {
    return apiFetch<PromptCollection[]>('/prompt-collections');
  },

  createCollection(input: { name: string; icon?: string; color?: string }): Promise<PromptCollection> {
    return apiFetch<PromptCollection>('/prompt-collections', { method: 'POST', ...json(input) });
  },

  updateCollection(id: string, input: { name: string; icon?: string | null; color?: string | null }): Promise<PromptCollection> {
    return apiFetch<PromptCollection>(`/prompt-collections/${id}`, { method: 'PATCH', ...json(input) });
  },

  deleteCollection(id: string): Promise<void> {
    return apiFetch<void>(`/prompt-collections/${id}`, { method: 'DELETE' });
  },
};
