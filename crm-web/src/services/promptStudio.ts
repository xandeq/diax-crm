import type {
  AddVersionInput,
  CreatePromptInput,
  ExportFlavor,
  ImportReport,
  ImportStrategy,
  PromptCollection,
  PromptDetail,
  PromptListItem,
  PromptTagCount,
  PromptVersion,
  PromptVersionSummary,
  UpdatePromptInput,
} from '@/types/promptStudio';
import { ApiError, apiFetch, apiFetchRaw } from './api';

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

  /** Baixa .md (1 prompt + format=md) ou .zip. Nome do arquivo vem do Content-Disposition. */
  async exportPrompts(opts: { ids?: string[]; format: 'md' | 'zip'; flavor: ExportFlavor }): Promise<string> {
    // POST com ids no corpo: muitos GUIDs na query string estouram o maxQueryString do IIS.
    const res = await apiFetchRaw('/prompts/export', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids: opts.ids ?? [], format: opts.format, flavor: opts.flavor }),
    });
    if (!res.ok) {
      let message = `Falha ao exportar (HTTP ${res.status})`;
      try { message = (await res.json()).message ?? message; } catch { /* corpo não-JSON */ }
      throw new ApiError(res.status, message);
    }
    const disposition = res.headers.get('Content-Disposition') ?? '';
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
    const fileName = match ? decodeURIComponent(match[1]) : opts.format === 'md' ? 'prompt.md' : 'prompt-studio.zip';
    const url = URL.createObjectURL(await res.blob());
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    return fileName;
  },

  importPrompts(files: File[], opts: { dryRun: boolean; strategy: ImportStrategy }): Promise<ImportReport> {
    const form = new FormData();
    files.forEach(f => form.append('files', f, f.name));
    form.append('dryRun', String(opts.dryRun));
    form.append('strategy', opts.strategy);
    return apiFetch<ImportReport>('/prompts/import', { method: 'POST', body: form });
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
