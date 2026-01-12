import { apiFetch } from './api';

export enum LogLevel {
  Debug = 0,
  Information = 1,
  Warning = 2,
  Error = 3,
  Critical = 4
}

export enum LogCategory {
  System = 0,
  Security = 1,
  Audit = 2,
  Business = 3,
  Performance = 4,
  Integration = 5
}

export interface AppLogResponse {
  id: string;
  timestampUtc: string;
  level: LogLevel;
  category: LogCategory;
  message: string;
  requestId?: string;
  correlationId?: string;
  userId?: string;
  requestPath?: string;
  queryString?: string;
  httpMethod?: string;
  statusCode?: number;
  headersJson?: string;
  clientIp?: string;
  userAgent?: string;
  exceptionType?: string;
  exceptionMessage?: string;
  stackTrace?: string;
  innerException?: string;
  targetSite?: string;
  machineName?: string;
  environment?: string;
  additionalData?: string;
  responseTimeMs?: number;
}

export interface AppLogListItemResponse {
  id: string;
  timestampUtc: string;
  level: LogLevel;
  category: LogCategory;
  message: string;
  userId?: string;
  requestPath?: string;
  httpMethod?: string;
  statusCode?: number;
  exceptionType?: string;
  correlationId?: string;
}

export interface AppLogFilterRequest {
  startDate?: string;
  endDate?: string;
  level?: LogLevel;
  category?: LogCategory;
  searchTerm?: string;
  userId?: string;
  correlationId?: string;
  requestPath?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface AppLogStatsResponse {
  debugCount: number;
  informationCount: number;
  warningCount: number;
  errorCount: number;
  criticalCount: number;
  totalCount: number;
}

export interface AppLogPagedResponse {
  items: AppLogListItemResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const logLevelLabels: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'Debug',
  [LogLevel.Information]: 'Informação',
  [LogLevel.Warning]: 'Aviso',
  [LogLevel.Error]: 'Erro',
  [LogLevel.Critical]: 'Crítico'
};

export const logCategoryLabels: Record<LogCategory, string> = {
  [LogCategory.System]: 'Sistema',
  [LogCategory.Security]: 'Segurança',
  [LogCategory.Audit]: 'Auditoria',
  [LogCategory.Business]: 'Negócio',
  [LogCategory.Performance]: 'Performance',
  [LogCategory.Integration]: 'Integração'
};

export const logLevelColors: Record<LogLevel, { bg: string; text: string; border: string }> = {
  [LogLevel.Debug]: { bg: 'bg-gray-100', text: 'text-gray-700', border: 'border-gray-300' },
  [LogLevel.Information]: { bg: 'bg-blue-100', text: 'text-blue-700', border: 'border-blue-300' },
  [LogLevel.Warning]: { bg: 'bg-yellow-100', text: 'text-yellow-700', border: 'border-yellow-300' },
  [LogLevel.Error]: { bg: 'bg-red-100', text: 'text-red-700', border: 'border-red-300' },
  [LogLevel.Critical]: { bg: 'bg-purple-100', text: 'text-purple-700', border: 'border-purple-300' }
};

class LogsService {
  async getFilteredLogs(filter: AppLogFilterRequest = {}): Promise<AppLogPagedResponse> {
    const params = new URLSearchParams();
    
    // Backend espera: fromDate, toDate, search, page, pageSize
    if (filter.startDate) params.append('fromDate', filter.startDate);
    if (filter.endDate) params.append('toDate', filter.endDate);
    if (filter.level !== undefined) params.append('level', filter.level.toString());
    if (filter.category !== undefined) params.append('category', filter.category.toString());
    if (filter.searchTerm) params.append('search', filter.searchTerm);
    if (filter.userId) params.append('userId', filter.userId);
    if (filter.correlationId) params.append('correlationId', filter.correlationId);
    if (filter.requestPath) params.append('path', filter.requestPath);
    if (filter.pageNumber) params.append('page', filter.pageNumber.toString());
    if (filter.pageSize) params.append('pageSize', filter.pageSize.toString());

    const queryString = params.toString();
    const path = queryString ? `/logs?${queryString}` : '/logs';
    
    return apiFetch<AppLogPagedResponse>(path);
  }

  async getLogById(id: string): Promise<AppLogResponse> {
    return apiFetch<AppLogResponse>(`/logs/${id}`);
  }

  async getStats(): Promise<AppLogStatsResponse> {
    return apiFetch<AppLogStatsResponse>('/logs/stats');
  }

  async cleanup(daysToKeep: number): Promise<{ deletedCount: number }> {
    return apiFetch<{ deletedCount: number }>(`/logs/cleanup?daysToKeep=${daysToKeep}`, {
      method: 'DELETE'
    });
  }
}

export const logsService = new LogsService();
