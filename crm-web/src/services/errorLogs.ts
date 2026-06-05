import { apiFetch } from './api';

// ─── Tipos derivados do contrato da API ───────────────────────────────────────

export type ErrorLogLevel = 'Warning' | 'Error' | 'Critical';

export interface ErrorLogResponse {
  id: string;
  appName: string;
  environment: string;
  level: ErrorLogLevel;
  message: string;
  exceptionType?: string;
  stackTrace?: string;
  source?: string;
  lineNumber?: number;
  requestMethod?: string;
  requestPath?: string;
  userId?: string;
  additionalData?: string; // JSON string
  fingerprint?: string;
  occurrenceCount: number;
  occurredAt: string; // ISO 8601 UTC
  firstSeenAt: string;
  lastSeenAt: string;
  isResolved: boolean;
  resolvedAt?: string;
  resolutionNote?: string;
}

export interface ErrorLogPagedResponse {
  items: ErrorLogResponse[];
  totalCount: number;
  nextCursor?: string | null;
}

export interface ErrorLogStatsResponse {
  totalToday: number;
  criticalToday: number;
  unresolvedTotal: number;
  byApp: AppErrorCount[];
}

export interface AppErrorCount {
  appName: string;
  count: number;
}

export interface ErrorLogFilters {
  appName?: string;
  level?: ErrorLogLevel;
  isResolved?: boolean;
  from?: string;
  to?: string;
  search?: string;
  cursor?: string;
  limit?: number;
}

export interface ResolveErrorLogRequest {
  note?: string;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

export const LEVEL_CONFIG: Record<ErrorLogLevel, { label: string; bg: string; text: string; dot: string }> = {
  Warning: { label: 'Aviso',   bg: 'bg-yellow-500/15', text: 'text-yellow-400', dot: '#EAB308' },
  Error:   { label: 'Erro',    bg: 'bg-red-500/15',    text: 'text-red-400',    dot: '#EF4444' },
  Critical:{ label: 'Crítico', bg: 'bg-purple-500/15', text: 'text-purple-400', dot: '#A855F7' },
};

// ─── Service ──────────────────────────────────────────────────────────────────

class ErrorLogsService {
  private buildParams(filters: ErrorLogFilters): URLSearchParams {
    const p = new URLSearchParams();
    if (filters.appName)            p.set('appName', filters.appName);
    if (filters.level)              p.set('level', filters.level);
    if (filters.isResolved != null) p.set('isResolved', String(filters.isResolved));
    if (filters.from)               p.set('from', filters.from);
    if (filters.to)                 p.set('to', filters.to);
    if (filters.search)             p.set('search', filters.search);
    if (filters.cursor)             p.set('cursor', filters.cursor);
    if (filters.limit)              p.set('limit', String(filters.limit));
    return p;
  }

  async getFiltered(filters: ErrorLogFilters = {}): Promise<ErrorLogPagedResponse> {
    const p = this.buildParams({ limit: 50, ...filters });
    const qs = p.toString();
    return apiFetch<ErrorLogPagedResponse>(`/error-logs${qs ? `?${qs}` : ''}`);
  }

  async getById(id: string): Promise<ErrorLogResponse> {
    return apiFetch<ErrorLogResponse>(`/error-logs/${id}`);
  }

  async getStats(): Promise<ErrorLogStatsResponse> {
    return apiFetch<ErrorLogStatsResponse>('/error-logs/aggregate/stats');
  }

  async resolve(id: string, note?: string): Promise<ErrorLogResponse> {
    return apiFetch<ErrorLogResponse>(`/error-logs/${id}/resolve`, {
      method: 'PUT',
      body: JSON.stringify({ note } satisfies ResolveErrorLogRequest),
    });
  }

  async cleanup(olderThanDays = 90): Promise<{ deletedCount: number; message: string }> {
    return apiFetch(`/error-logs/cleanup?olderThanDays=${olderThanDays}`, { method: 'DELETE' });
  }
}

export const errorLogsService = new ErrorLogsService();
