import { apiFetch } from './api';
import type {
  PagedResult,
  ConversationListItem,
  ConversationDetail,
  CreateConversationPayload,
  UpdateConversationPayload,
  MonthlyUsage,
} from '@/types/ai-chat';

/**
 * GET /api/v1/ai/conversations
 */
export async function listConversations(
  page = 1,
  pageSize = 30,
  includeArchived = false,
): Promise<PagedResult<ConversationListItem>> {
  return apiFetch<PagedResult<ConversationListItem>>(
    `/ai/conversations?page=${page}&pageSize=${pageSize}&includeArchived=${includeArchived}`,
  );
}

/**
 * GET /api/v1/ai/conversations/{id}
 */
export async function getConversation(id: string): Promise<ConversationDetail> {
  return apiFetch<ConversationDetail>(`/ai/conversations/${id}`);
}

/**
 * POST /api/v1/ai/conversations
 */
export async function createConversation(
  payload: CreateConversationPayload,
): Promise<ConversationDetail> {
  return apiFetch<ConversationDetail>('/ai/conversations', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

/**
 * PATCH /api/v1/ai/conversations/{id}
 */
export async function updateConversation(
  id: string,
  payload: UpdateConversationPayload,
): Promise<ConversationDetail> {
  return apiFetch<ConversationDetail>(`/ai/conversations/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  });
}

/**
 * DELETE /api/v1/ai/conversations/{id}
 * Soft-deletes via archive flag.
 */
export async function archiveConversation(id: string): Promise<void> {
  await apiFetch<unknown>(`/ai/conversations/${id}`, { method: 'DELETE' });
}

/**
 * GET /api/v1/ai/usage
 */
export async function getMonthlyUsage(): Promise<MonthlyUsage> {
  return apiFetch<MonthlyUsage>('/ai/usage');
}
