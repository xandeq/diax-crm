'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { AppLogListItemResponse, AppLogResponse, LogLevel, logLevelLabels, logCategoryLabels as serviceCategoryLabels, logsService } from '@/services/logs';
import { ChevronDown, ChevronRight, Loader2, Search, Wrench, RefreshCw, AlertCircle, FileText } from 'lucide-react';
import { useState, useCallback } from 'react';
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
  [LogLevel.Debug]: 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20',
  [LogLevel.Information]: 'bg-blue-500/10 text-blue-300 border-blue-500/20',
  [LogLevel.Warning]: 'bg-amber-500/10 text-amber-300 border-amber-500/20',
  [LogLevel.Error]: 'bg-rose-500/10 text-rose-300 border-rose-500/20',
  [LogLevel.Critical]: 'bg-purple-500/10 text-purple-300 border-purple-500/20'
};

function LogLevelBadge({ level }: { level: LogLevel }) {
  const colors = levelColorClasses[level] || 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20';
  return (
    <Badge variant="outline" className={`text-xs font-semibold px-2 py-0.5 rounded-full ${colors}`}>
      {logLevelLabels[level]}
    </Badge>
  );
}

function StatusCodeBadge({ code }: { code?: number }) {
  if (!code) return <span className="text-zinc-500">-</span>;

  let color = 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20';
  if (code >= 200 && code < 300) color = 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20';
  else if (code >= 300 && code < 400) color = 'bg-blue-500/10 text-blue-300 border-blue-500/20';
  else if (code >= 400 && code < 500) color = 'bg-amber-500/10 text-amber-300 border-amber-500/20';
  else if (code >= 500) color = 'bg-rose-500/10 text-rose-300 border-rose-500/20';

  return <Badge variant="outline" className={`text-xs font-semibold px-2 py-0.5 rounded-full ${color}`}>{code}</Badge>;
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
    <div className="flex items-center gap-2 mt-4 pt-3 flex-wrap border-t border-white/[0.08]">
      <span className="text-xs mr-1 text-zinc-400 font-semibold">Copiar prompt para IA:</span>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-blue-300 border-blue-500/20 bg-blue-500/5 hover:bg-blue-500/15"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('analyze', details); }}
      >
        <Search className="h-3.5 w-3.5" />
        Analisar
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-red-300 border-red-500/20 bg-red-500/5 hover:bg-red-500/15"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('fix', details); }}
      >
        <Wrench className="h-3.5 w-3.5" />
        Corrigir
      </Button>
      <Button
        variant="outline"
        size="sm"
        className="text-xs gap-1.5 text-amber-300 border-amber-500/20 bg-amber-500/5 hover:bg-amber-500/15"
        onClick={(e) => { e.stopPropagation(); copyPromptToClipboard('refactor', details); }}
      >
        <RefreshCw className="h-3.5 w-3.5" />
        Refatorar
      </Button>
    </div>
  );
}

const INFO_BOX = { background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)', borderRadius: '0.5rem', padding: '0.75rem' };
const LABEL_COLOR = { color: '#A1A1AA' };
const VALUE_COLOR = { color: '#E4E4E7' };
const HEADING_COLOR = { color: '#F4F4F5' };

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
      <TableRow
        onClick={handleExpand}
        className={`cursor-pointer hover:bg-white/[0.04] transition-all duration-200 ${expanded ? 'bg-white/[0.03]' : ''}`}
      >
        <TableCell className="px-3 py-2" data-label="Detalhes">
          <Button variant="ghost" size="sm" className="p-0 h-6 w-6 text-zinc-400 hover:text-zinc-100 hover:bg-white/5">
            {loadingDetails ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : expanded ? (
              <ChevronDown className="h-4 w-4" />
            ) : (
              <ChevronRight className="h-4 w-4" />
            )}
          </Button>
        </TableCell>
        <TableCell className="px-3 py-2 text-xs font-medium text-zinc-400 whitespace-nowrap" data-label="Data/Hora">
          {formatDate(log.timestampUtc)}
        </TableCell>
        <TableCell className="px-3 py-2" data-label="Nível">
          <LogLevelBadge level={log.level} />
        </TableCell>
        <TableCell className="px-3 py-2 text-xs font-semibold text-zinc-300" data-label="Categoria">
          {logCategoryLabels[log.category]}
        </TableCell>
        <TableCell className="px-3 py-2 text-sm text-zinc-100 max-w-md truncate" data-label="Mensagem" title={log.message}>
          {log.message}
        </TableCell>
        <TableCell className="px-3 py-2 text-xs font-mono text-zinc-400" data-label="Requisição">
          {log.httpMethod && log.requestPath ? (
            <span>
              <span className="font-bold text-zinc-200">{log.httpMethod}</span> {log.requestPath}
            </span>
          ) : (
            <span className="text-zinc-600">-</span>
          )}
        </TableCell>
        <TableCell className="px-3 py-2" data-label="Status">
          <StatusCodeBadge code={log.statusCode} />
        </TableCell>
      </TableRow>

      {expanded && details && (
        <TableRow data-detail="true" className="bg-zinc-950/20 hover:bg-zinc-950/20 border-b border-white/5">
          <TableCell colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
              {/* Informações básicas */}
              <div className="space-y-2">
                <h4 className="font-bold" style={HEADING_COLOR}>Informações Gerais</h4>
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
                  <h4 className="font-bold" style={HEADING_COLOR}>Requisição HTTP</h4>
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
                <h4 className="font-bold" style={HEADING_COLOR}>Mensagem</h4>
                <div style={INFO_BOX}>
                  <pre className="whitespace-pre-wrap text-xs text-zinc-300 font-mono">{details.message}</pre>
                </div>
              </div>

              {/* Headers */}
              {details.headersJson && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-bold" style={HEADING_COLOR}>Headers</h4>
                  <div className="rounded-lg overflow-x-auto border border-white/5 bg-[#050B08] p-3">
                    <pre className="text-xs font-mono text-[#00D4AA]">{formatJson(details.headersJson)}</pre>
                  </div>
                </div>
              )}

              {/* Exception */}
              {details.exceptionType && (
                <div className="md:col-span-2 space-y-2">
                  <h4 className="font-bold text-rose-400">Exceção</h4>
                  <div className="rounded-lg p-4 space-y-2 bg-rose-500/5 border border-rose-500/15">
                    <p><span style={LABEL_COLOR}>Tipo:</span> <span className="font-mono text-rose-400 font-semibold">{details.exceptionType}</span></p>
                    <p><span style={LABEL_COLOR}>Mensagem:</span> <span className="text-rose-300">{details.exceptionMessage}</span></p>
                    {details.targetSite && (
                      <p><span style={LABEL_COLOR}>Target:</span> <span className="font-mono text-xs" style={VALUE_COLOR}>{details.targetSite}</span></p>
                    )}
                    {details.stackTrace && (
                      <div className="mt-2">
                        <p className="mb-1 text-xs font-semibold" style={LABEL_COLOR}>Stack Trace:</p>
                        <pre className="rounded-md text-xs font-mono overflow-x-auto whitespace-pre-wrap p-2.5 bg-[#050B08] text-rose-300 border border-white/5">
                          {details.stackTrace}
                        </pre>
                      </div>
                    )}
                    {details.innerException && (
                      <div className="mt-2">
                        <p className="mb-1 text-xs font-semibold" style={LABEL_COLOR}>Inner Exception:</p>
                        <pre className="rounded-md text-xs font-mono overflow-x-auto whitespace-pre-wrap p-2.5 bg-[#050B08] text-amber-300 border border-white/5">
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
                  <h4 className="font-bold" style={HEADING_COLOR}>Dados Adicionais</h4>
                  <div className="rounded-lg overflow-x-auto border border-white/5 bg-[#050B08] p-3">
                    <pre className="text-xs font-mono text-blue-400">{formatJson(details.additionalData)}</pre>
                  </div>
                </div>
              )}

              {/* AI Prompt Buttons */}
              <div className="md:col-span-2">
                <PromptButtons details={details} />
              </div>
            </div>
          </TableCell>
        </TableRow>
      )}
    </>
  );
}

export function LogsTable({ logs, loading }: LogsTableProps) {
  if (loading) {
    return (
      <div className="overflow-x-auto">
        <Table className="responsive-table">
          <TableHeader>
            <TableRow>
              <TableHead className="px-3 py-2 w-10"></TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Data/Hora</TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Nível</TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Categoria</TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Mensagem</TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Requisição</TableHead>
              <TableHead className="px-3 py-2 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {Array.from({ length: 8 }).map((_, i) => (
              <TableRow key={i}>
                <TableCell><Skeleton className="h-4 w-4 shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-4 w-28 shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-5 w-16 rounded-full shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-4 w-20 shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-4 w-72 shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-4 w-48 shimmer-bg" /></TableCell>
                <TableCell><Skeleton className="h-5 w-10 rounded-full shimmer-bg" /></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className="text-center py-12 border border-dashed border-white/10 rounded-xl bg-white/[0.01] m-4">
        <FileText className="h-12 w-12 text-zinc-500 mx-auto mb-3 opacity-30" />
        <h3 className="font-semibold text-zinc-300 mb-1">Nenhum log encontrado</h3>
        <p className="text-xs text-zinc-500 max-w-xs mx-auto">
          Experimente alterar os filtros avançados ou buscar por outro termo.
        </p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <Table className="responsive-table">
        <TableHeader>
          <TableRow className="bg-white/[0.03] border-b border-white/[0.08]">
            <TableHead className="px-3 py-3 w-10"></TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Data/Hora</TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Nível</TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Categoria</TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Mensagem</TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Requisição</TableHead>
            <TableHead className="px-3 py-3 text-xs font-semibold text-zinc-400 uppercase tracking-wider">Status</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {logs.map((log) => (
            <LogRow key={log.id} log={log} />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
