'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { AppLogListItemResponse, AppLogResponse, LogLevel, logLevelLabels, logCategoryLabels as serviceCategoryLabels, logsService } from '@/services/logs';
import { ChevronDown, ChevronRight, Copy, Loader2, Search, Wrench, RefreshCw } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

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
  if (!code) return <span style={{ color: '#6B7280' }}>-</span>;

  let color = 'bg-gray-100 text-gray-800';
  if (code >= 200 && code < 300) color = 'bg-green-100 text-green-800';
  else if (code >= 300 && code < 400) color = 'bg-blue-100 text-blue-800';
  else if (code >= 400 && code < 500) color = 'bg-yellow-100 text-yellow-800';
  else if (code >= 500) color = 'bg-red-100 text-red-800';

  return <Badge className={color}>{code}</Badge>;
}

function buildLogContext(details: AppLogResponse): string {
  const lines: string[] = [];
  lines.push(`## Log Entry Details`);
  lines.push(`- **Level:** ${logLevelLabels[details.level]}`);
  lines.push(`- **Category:** ${serviceCategoryLabels[details.category]}`);
  lines.push(`- **Timestamp:** ${details.timestampUtc}`);
  if (details.environment) lines.push(`- **Environment:** ${details.environment}`);
  if (details.machineName) lines.push(`- **Machine:** ${details.machineName}`);
  if (details.requestPath) {
    lines.push(`\n### HTTP Request`);
    lines.push(`- **Method:** ${details.httpMethod}`);
    lines.push(`- **Path:** ${details.requestPath}`);
    if (details.queryString) lines.push(`- **Query:** ${details.queryString}`);
    if (details.statusCode) lines.push(`- **Status Code:** ${details.statusCode}`);
    if (details.responseTimeMs) lines.push(`- **Response Time:** ${details.responseTimeMs}ms`);
  }
  lines.push(`\n### Message`);
  lines.push('```');
  lines.push(details.message);
  lines.push('```');
  if (details.exceptionType) {
    lines.push(`\n### Exception`);
    lines.push(`- **Type:** ${details.exceptionType}`);
    lines.push(`- **Message:** ${details.exceptionMessage}`);
    if (details.targetSite) lines.push(`- **Target Site:** ${details.targetSite}`);
    if (details.stackTrace) {
      lines.push(`\n**Stack Trace:**`);
      lines.push('```');
      lines.push(details.stackTrace);
      lines.push('```');
    }
    if (details.innerException) {
      lines.push(`\n**Inner Exception:**`);
      lines.push('```');
      lines.push(details.innerException);
      lines.push('```');
    }
  }
  if (details.additionalData) {
    lines.push(`\n### Additional Data`);
    lines.push('```json');
    try {
      lines.push(JSON.stringify(JSON.parse(details.additionalData), null, 2));
    } catch {
      lines.push(details.additionalData);
    }
    lines.push('```');
  }
  return lines.join('\n');
}

function buildPrompt(action: 'analyze' | 'fix' | 'refactor', details: AppLogResponse): string {
  const context = buildLogContext(details);
  const projectInfo = `This is a .NET 8 (ASP.NET Core) backend API with Clean Architecture (Controllers → Application Services → Domain → Infrastructure/EF Core). Database: SQL Server. Frontend: Next.js 14 with TypeScript.`;

  if (action === 'analyze') {
    return `Analyze the following application log entry and provide a detailed diagnosis. Explain what happened, the root cause, severity assessment, and potential impact on the system.

**Project context:** ${projectInfo}

${context}

Please provide:
1. **Summary** of what happened
2. **Root cause** analysis
3. **Severity** assessment (Critical / High / Medium / Low)
4. **Impact** on users and system
5. **Recommended actions** to investigate further`;
  }

  if (action === 'fix') {
    return `Fix the issue described in the following application log entry. Provide the exact code changes needed to resolve this error/warning.

**Project context:** ${projectInfo}

${context}

Please provide:
1. **Root cause** of the issue
2. **Exact code fix** with before/after code snippets
3. **File paths** where changes should be made
4. **Testing steps** to verify the fix
5. **Prevention** measures to avoid recurrence`;
  }

  // refactor
  return `Refactor the code related to the following application log entry. The goal is to improve code quality, error handling, and resilience in the area that generated this log.

**Project context:** ${projectInfo}

${context}

Please provide:
1. **Current issues** identified from the log
2. **Refactoring plan** with specific improvements
3. **Code changes** with before/after snippets
4. **Better error handling** patterns to apply
5. **Testing recommendations** for the refactored code`;
}

async function copyPromptToClipboard(action: 'analyze' | 'fix' | 'refactor', details: AppLogResponse) {
  const prompt = buildPrompt(action, details);
  try {
    await navigator.clipboard.writeText(prompt);
    const labels = { analyze: 'Análise', fix: 'Correção', refactor: 'Refatoração' };
    toast.success(`Prompt de ${labels[action]} copiado!`, { description: 'Cole na sua IA/LLM preferida' });
  } catch {
    toast.error('Falha ao copiar para área de transferência');
  }
}

function PromptButtons({ details }: { details: AppLogResponse }) {
  return (
    <div className="flex items-center gap-2 mt-4 pt-3" style={{ borderTop: '1px solid rgba(255,255,255,0.07)' }}>
      <span className="text-xs mr-1" style={{ color: '#9CA3AF' }}>Copiar prompt para IA:</span>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-blue-400 border-blue-800 hover:bg-blue-950"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('analyze', details); }}
      >
        <Search className="h-3.5 w-3.5" />
        Analisar
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-red-400 border-red-800 hover:bg-red-950"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('fix', details); }}
      >
        <Wrench className="h-3.5 w-3.5" />
        Corrigir
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-amber-400 border-amber-800 hover:bg-amber-950"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('refactor', details); }}
      >
        <RefreshCw className="h-3.5 w-3.5" />
        Refatorar
      </Button>
    </div>
  );
}

const INFO_BOX = { background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)', borderRadius: '0.375rem', padding: '0.75rem' };
const LABEL_COLOR = { color: '#9CA3AF' };
const VALUE_COLOR = { color: '#D1D5DB' };
const HEADING_COLOR = { color: '#F9FAFB' };

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
        className="cursor-pointer transition-colors"
        style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}
        onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
        onMouseLeave={e => (e.currentTarget.style.background = '')}
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
        <td className="px-3 py-2 text-xs whitespace-nowrap" style={{ color: '#9CA3AF' }}>
          {formatDate(log.timestampUtc)}
        </td>
        <td className="px-3 py-2">
          <LogLevelBadge level={log.level} />
        </td>
        <td className="px-3 py-2 text-xs" style={{ color: '#D1D5DB' }}>
          {logCategoryLabels[log.category]}
        </td>
        <td className="px-3 py-2 text-sm max-w-md truncate" style={{ color: '#F9FAFB' }} title={log.message}>
          {log.message}
        </td>
        <td className="px-3 py-2 text-xs font-mono" style={{ color: '#9CA3AF' }}>
          {log.httpMethod && log.requestPath ? (
            <span>
              <span className="font-semibold" style={{ color: '#D1D5DB' }}>{log.httpMethod}</span> {log.requestPath}
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
        <tr style={{ background: 'rgba(255,255,255,0.02)' }}>
          <td colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
              {/* Informações básicas */}
              <div className="space-y-2">
                <h4 className="font-semibold" style={HEADING_COLOR}>Informações Gerais</h4>
                <div style={INFO_BOX} className="space-y-1">
                  <p><span style={LABEL_COLOR}>ID:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.id}</span></p>
                  <p><span style={LABEL_COLOR}>Correlation ID:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.correlationId || '-'}</span></p>
                  <p><span style={LABEL_COLOR}>Request ID:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.requestId || '-'}</span></p>
                  <p><span style={LABEL_COLOR}>User ID:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.userId || '-'}</span></p>
                  <p><span style={LABEL_COLOR}>Machine:</span> <span style={VALUE_COLOR}>{details.machineName || '-'}</span></p>
                  <p><span style={LABEL_COLOR}>Environment:</span> <span style={VALUE_COLOR}>{details.environment || '-'}</span></p>
                  {details.responseTimeMs && (
                    <p><span style={LABEL_COLOR}>Response Time:</span> <span style={VALUE_COLOR}>{details.responseTimeMs}ms</span></p>
                  )}
                </div>
              </div>

              {/* Requisição HTTP */}
              {details.requestPath && (
                <div className="space-y-2">
                  <h4 className="font-semibold" style={HEADING_COLOR}>Requisição HTTP</h4>
                  <div style={INFO_BOX} className="space-y-1">
                    <p><span style={LABEL_COLOR}>Método:</span> <span style={VALUE_COLOR}>{details.httpMethod}</span></p>
                    <p><span style={LABEL_COLOR}>Path:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.requestPath}</span></p>
                    {details.queryString && (
                      <p><span style={LABEL_COLOR}>Query:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.queryString}</span></p>
                    )}
                    <p><span style={LABEL_COLOR}>Status:</span> <StatusCodeBadge code={details.statusCode} /></p>
                    <p><span style={LABEL_COLOR}>Client IP:</span> <span style={VALUE_COLOR}>{details.clientIp || '-'}</span></p>
                    <p><span style={LABEL_COLOR}>User Agent:</span> <span className="text-xs" style={VALUE_COLOR}>{details.userAgent || '-'}</span></p>
                  </div>
                </div>
              )}

              {/* Mensagem completa */}
              <div className="md:col-span-2 space-y-2">
                <h4 className="font-semibold" style={HEADING_COLOR}>Mensagem</h4>
                <div style={INFO_BOX}>
                  <pre className="whitespace-pre-wrap text-xs" style={{ color: '#D1D5DB' }}>{details.message}</pre>
                </div>
              </div>

              {/* Headers */}
              {details.headersJson && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-semibold" style={HEADING_COLOR}>Headers</h4>
                  <div className="rounded overflow-x-auto" style={{ background: '#0B1510', border: '1px solid rgba(255,255,255,0.09)', padding: '0.75rem' }}>
                    <pre className="text-xs" style={{ color: '#4ade80' }}>{formatJson(details.headersJson)}</pre>
                  </div>
                </div>
              )}

              {/* Exception */}
              {details.exceptionType && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-semibold" style={{ color: '#f87171' }}>Exceção</h4>
                  <div className="rounded p-3 space-y-2" style={{ background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.25)' }}>
                    <p><span style={LABEL_COLOR}>Tipo:</span> <span className="font-mono" style={{ color: '#f87171' }}>{details.exceptionType}</span></p>
                    <p><span style={LABEL_COLOR}>Mensagem:</span> <span style={{ color: '#fca5a5' }}>{details.exceptionMessage}</span></p>
                    {details.targetSite && (
                      <p><span style={LABEL_COLOR}>Target:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.targetSite}</span></p>
                    )}
                    {details.stackTrace && (
                      <div className="mt-2">
                        <p className="mb-1" style={LABEL_COLOR}>Stack Trace:</p>
                        <pre className="rounded text-xs overflow-x-auto whitespace-pre-wrap p-2" style={{ background: '#0B1510', color: '#f87171' }}>
                          {details.stackTrace}
                        </pre>
                      </div>
                    )}
                    {details.innerException && (
                      <div className="mt-2">
                        <p className="mb-1" style={LABEL_COLOR}>Inner Exception:</p>
                        <pre className="rounded text-xs overflow-x-auto whitespace-pre-wrap p-2" style={{ background: '#0B1510', color: '#fbbf24' }}>
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
                  <h4 className="font-semibold" style={HEADING_COLOR}>Dados Adicionais</h4>
                  <div className="rounded overflow-x-auto" style={{ background: '#0B1510', border: '1px solid rgba(255,255,255,0.09)', padding: '0.75rem' }}>
                    <pre className="text-xs" style={{ color: '#60a5fa' }}>{formatJson(details.additionalData)}</pre>
                  </div>
                </div>
              )}

              {/* AI Prompt Buttons */}
              <div className="md:col-span-2">
                <PromptButtons details={details} />
              </div>
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
        <Loader2 className="h-8 w-8 animate-spin" style={{ color: '#6B7280' }} />
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className="text-center py-12" style={{ color: '#9CA3AF' }}>
        Nenhum log encontrado com os filtros selecionados.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left">
        <thead style={{ background: 'rgba(255,255,255,0.04)', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
          <tr>
            <th className="px-3 py-2 w-10 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}></th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Data/Hora</th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Nível</th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Categoria</th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Mensagem</th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Requisição</th>
            <th className="px-3 py-2 text-xs uppercase font-medium tracking-wider" style={{ color: '#9CA3AF' }}>Status</th>
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
