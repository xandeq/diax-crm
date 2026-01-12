'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AppLogListItemResponse, AppLogResponse, LogLevel, logLevelLabels, logsService } from '@/services/logs';
import { ChevronDown, ChevronRight, Loader2 } from 'lucide-react';
import { useState } from 'react';

interface LogsTableProps {
  logs: AppLogListItemResponse[];
  loading?: boolean;
}

const logCategoryLabels: Record<number, string> = {
  0: 'Sistema',
  1: 'Segurança',
  2: 'Auditoria',
  3: 'Negócio',
  4: 'Performance',
  5: 'Integração'
};

const levelColorClasses: Record<LogLevel, string> = {
  [LogLevel.Debug]: 'bg-gray-100 text-gray-700',
  [LogLevel.Information]: 'bg-blue-100 text-blue-700',
  [LogLevel.Warning]: 'bg-yellow-100 text-yellow-700',
  [LogLevel.Error]: 'bg-red-100 text-red-700',
  [LogLevel.Critical]: 'bg-purple-100 text-purple-700'
};

function LogLevelBadge({ level }: { level: LogLevel }) {
  const colors = levelColorClasses[level] || 'bg-gray-100 text-gray-800';
  return (
    <Badge className={colors}>
      {logLevelLabels[level]}
    </Badge>
  );
}

function StatusCodeBadge({ code }: { code?: number }) {
  if (!code) return <span className="text-gray-400">-</span>;

  let color = 'bg-gray-100 text-gray-800';
  if (code >= 200 && code < 300) color = 'bg-green-100 text-green-800';
  else if (code >= 300 && code < 400) color = 'bg-blue-100 text-blue-800';
  else if (code >= 400 && code < 500) color = 'bg-yellow-100 text-yellow-800';
  else if (code >= 500) color = 'bg-red-100 text-red-800';

  return <Badge className={color}>{code}</Badge>;
}

function LogRow({ log }: { log: AppLogListItemResponse }) {
  const [expanded, setExpanded] = useState(false);
  const [details, setDetails] = useState<AppLogResponse | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);

  const handleExpand = async () => {
    if (!expanded && !details) {
      setLoadingDetails(true);
      try {
        const data = await logsService.getLogById(log.id);
        setDetails(data);
      } catch (err) {
        console.error('Error loading log details:', err);
      } finally {
        setLoadingDetails(false);
      }
    }
    setExpanded(!expanded);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  };

  const formatJson = (json?: string) => {
    if (!json) return null;
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  };

  return (
    <>
      <tr
        className="hover:bg-gray-50 cursor-pointer border-b"
        onClick={handleExpand}
      >
        <td className="px-3 py-2">
          <Button variant="ghost" size="sm" className="p-0 h-6 w-6">
            {loadingDetails ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : expanded ? (
              <ChevronDown className="h-4 w-4" />
            ) : (
              <ChevronRight className="h-4 w-4" />
            )}
          </Button>
        </td>
        <td className="px-3 py-2 text-xs text-gray-500 whitespace-nowrap">
          {formatDate(log.timestampUtc)}
        </td>
        <td className="px-3 py-2">
          <LogLevelBadge level={log.level} />
        </td>
        <td className="px-3 py-2 text-xs text-gray-600">
          {logCategoryLabels[log.category]}
        </td>
        <td className="px-3 py-2 text-sm max-w-md truncate" title={log.message}>
          {log.message}
        </td>
        <td className="px-3 py-2 text-xs text-gray-500 font-mono">
          {log.httpMethod && log.requestPath ? (
            <span>
              <span className="font-semibold">{log.httpMethod}</span> {log.requestPath}
            </span>
          ) : (
            '-'
          )}
        </td>
        <td className="px-3 py-2">
          <StatusCodeBadge code={log.statusCode} />
        </td>
      </tr>

      {expanded && details && (
        <tr className="bg-gray-50">
          <td colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
              {/* Informações básicas */}
              <div className="space-y-2">
                <h4 className="font-semibold text-gray-700">Informações Gerais</h4>
                <div className="bg-white p-3 rounded border space-y-1">
                  <p><span className="text-gray-500">ID:</span> <span className="font-mono text-xs">{details.id}</span></p>
                  <p><span className="text-gray-500">Correlation ID:</span> <span className="font-mono text-xs">{details.correlationId || '-'}</span></p>
                  <p><span className="text-gray-500">Request ID:</span> <span className="font-mono text-xs">{details.requestId || '-'}</span></p>
                  <p><span className="text-gray-500">User ID:</span> <span className="font-mono text-xs">{details.userId || '-'}</span></p>
                  <p><span className="text-gray-500">Machine:</span> {details.machineName || '-'}</p>
                  <p><span className="text-gray-500">Environment:</span> {details.environment || '-'}</p>
                  {details.responseTimeMs && (
                    <p><span className="text-gray-500">Response Time:</span> {details.responseTimeMs}ms</p>
                  )}
                </div>
              </div>

              {/* Requisição HTTP */}
              {details.requestPath && (
                <div className="space-y-2">
                  <h4 className="font-semibold text-gray-700">Requisição HTTP</h4>
                  <div className="bg-white p-3 rounded border space-y-1">
                    <p><span className="text-gray-500">Método:</span> {details.httpMethod}</p>
                    <p><span className="text-gray-500">Path:</span> <span className="font-mono text-xs">{details.requestPath}</span></p>
                    {details.queryString && (
                      <p><span className="text-gray-500">Query:</span> <span className="font-mono text-xs">{details.queryString}</span></p>
                    )}
                    <p><span className="text-gray-500">Status:</span> <StatusCodeBadge code={details.statusCode} /></p>
                    <p><span className="text-gray-500">Client IP:</span> {details.clientIp || '-'}</p>
                    <p><span className="text-gray-500">User Agent:</span> <span className="text-xs">{details.userAgent || '-'}</span></p>
                  </div>
                </div>
              )}

              {/* Mensagem completa */}
              <div className="md:col-span-2 space-y-2">
                <h4 className="font-semibold text-gray-700">Mensagem</h4>
                <div className="bg-white p-3 rounded border">
                  <pre className="whitespace-pre-wrap text-xs">{details.message}</pre>
                </div>
              </div>

              {/* Headers */}
              {details.headersJson && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-semibold text-gray-700">Headers</h4>
                  <div className="bg-gray-800 text-green-400 p-3 rounded overflow-x-auto">
                    <pre className="text-xs">{formatJson(details.headersJson)}</pre>
                  </div>
                </div>
              )}

              {/* Exception */}
              {details.exceptionType && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-semibold text-red-700">Exceção</h4>
                  <div className="bg-red-50 border border-red-200 p-3 rounded space-y-2">
                    <p><span className="text-gray-500">Tipo:</span> <span className="font-mono text-red-700">{details.exceptionType}</span></p>
                    <p><span className="text-gray-500">Mensagem:</span> {details.exceptionMessage}</p>
                    {details.targetSite && (
                      <p><span className="text-gray-500">Target:</span> <span className="font-mono text-xs">{details.targetSite}</span></p>
                    )}
                    {details.stackTrace && (
                      <div className="mt-2">
                        <p className="text-gray-500 mb-1">Stack Trace:</p>
                        <pre className="bg-gray-800 text-red-400 p-2 rounded text-xs overflow-x-auto whitespace-pre-wrap">
                          {details.stackTrace}
                        </pre>
                      </div>
                    )}
                    {details.innerException && (
                      <div className="mt-2">
                        <p className="text-gray-500 mb-1">Inner Exception:</p>
                        <pre className="bg-gray-800 text-yellow-400 p-2 rounded text-xs overflow-x-auto whitespace-pre-wrap">
                          {details.innerException}
                        </pre>
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* Additional Data */}
              {details.additionalData && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-semibold text-gray-700">Dados Adicionais</h4>
                  <div className="bg-gray-800 text-blue-400 p-3 rounded overflow-x-auto">
                    <pre className="text-xs">{formatJson(details.additionalData)}</pre>
                  </div>
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

export function LogsTable({ logs, loading }: LogsTableProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        Nenhum log encontrado com os filtros selecionados.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left">
        <thead className="bg-gray-100 text-xs uppercase text-gray-600">
          <tr>
            <th className="px-3 py-2 w-10"></th>
            <th className="px-3 py-2">Data/Hora</th>
            <th className="px-3 py-2">Nível</th>
            <th className="px-3 py-2">Categoria</th>
            <th className="px-3 py-2">Mensagem</th>
            <th className="px-3 py-2">Requisição</th>
            <th className="px-3 py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((log) => (
            <LogRow key={log.id} log={log} />
          ))}
        </tbody>
      </table>
    </div>
  );
}
