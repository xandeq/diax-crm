// ===== STREAM CHUNKS (SSE) =====

export const CHUNK_TYPE = {
  ConversationStarted: 'conversation_started',
  ContentDelta: 'content_delta',
  MessageStop: 'message_stop',
  Usage: 'usage',
  Title: 'title',
  Error: 'error',
} as const;

export type ChunkType = (typeof CHUNK_TYPE)[keyof typeof CHUNK_TYPE];

export interface UsageInfo {
  inputTokens: number;
  outputTokens: number;
  cacheReadTokens: number;
  cacheCreationTokens: number;
  costUsd: number;
}

export interface ChatStreamChunk {
  type: string;
  conversationId?: string;
  messageId?: string;
  delta?: string;
  title?: string;
  stopReason?: string;
  usage?: UsageInfo;
  error?: string;
}

// ===== REQUEST PAYLOAD =====

export interface AttachmentPayload {
  fileName: string;
  contentType: string;
  sizeBytes: number;
  /** Text content extracted client-side from the file */
  content: string;
}

export interface ChatRequest {
  conversationId?: string | null;
  /** Anthropic model ID, e.g. "claude-sonnet-4-5" */
  model: string;
  systemPrompt?: string | null;
  message: string;
  attachments?: AttachmentPayload[];
}

// ===== CONVERSATION CRUD =====

export interface ConversationListItem {
  id: string;
  title: string;
  model: string;
  createdAt: string;
  updatedAt?: string | null;
  isArchived: boolean;
  messageCount: number;
}

export interface AttachmentMeta {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  inputTokens: number;
  outputTokens: number;
  cacheReadTokens: number;
  cacheCreationTokens: number;
  costUsd: number;
  createdAt: string;
  attachments: AttachmentMeta[];
}

export interface ConversationDetail {
  id: string;
  title: string;
  model: string;
  systemPrompt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  isArchived: boolean;
  messages: ChatMessage[];
}

export interface CreateConversationPayload {
  title?: string | null;
  model: string;
  systemPrompt?: string | null;
}

export interface UpdateConversationPayload {
  title?: string | null;
  systemPrompt?: string | null;
  isArchived?: boolean | null;
}

// ===== PAGED RESULT =====

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ===== USAGE SUMMARY =====

export interface MonthlyUsage {
  year: number;
  month: number;
  inputTokens: number;
  outputTokens: number;
  cacheReadTokens: number;
  cacheCreationTokens: number;
  totalTokens: number;
  costUsd: number;
}

// ===== MODEL CATALOGUE =====

export type AnthropicModelId =
  | 'claude-opus-4-5'
  | 'claude-sonnet-4-5'
  | 'claude-haiku-4-5';

export interface ModelOption {
  id: AnthropicModelId;
  label: string;
  description: string;
  badge?: string;
}

export const MODEL_OPTIONS: ModelOption[] = [
  {
    id: 'claude-opus-4-5',
    label: 'Opus',
    description: 'Máximo raciocínio e precisão',
    badge: 'Mais capaz',
  },
  {
    id: 'claude-sonnet-4-5',
    label: 'Sonnet',
    description: 'Equilíbrio entre velocidade e qualidade',
    badge: 'Recomendado',
  },
  {
    id: 'claude-haiku-4-5',
    label: 'Haiku',
    description: 'Respostas rápidas e econômicas',
    badge: 'Mais rápido',
  },
];

export const DEFAULT_MODEL: AnthropicModelId = 'claude-sonnet-4-5';
