'use client';

import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Skeleton } from '@/components/ui/skeleton';
import { Textarea } from '@/components/ui/textarea';
import { type ErrorLogResponse, errorLogsService } from '@/services/errorLogs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Check, CheckCircle, ClipboardCopy, Terminal } from 'lucide-react';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';
import { ErrorLevelBadge } from './ErrorLevelBadge';

interface Props {
  logId: string | null;
  onClose: () => void;
  onResolved?: (id: string) => void;
}

function CopyButton({ text, label }: { text: string; label: string }) {
  const [copied, setCopied] = useState(false);
  const copy = useCallback(() => {
    navigator.clipboard.writeText(text).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  }, [text]);
  return (
    <button
      onClick={copy}
      className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs transition-colors hover:bg-white/10"
      style={{ color: copied ? '#4ADE80' : '#9CA3AF' }}
      aria-label={label}
      title={label}
    >
      {copied ? <Check className="h-3.5 w-3.5" /> : <ClipboardCopy className="h-3.5 w-3.5" />}
      {copied ? 'Copiado!' : 'Copiar'}
    </button>
  );
}

function buildClaudeCliPrompt(log: ErrorLogResponse): string {
  return `claude "Fix this ${log.level} in ${log.appName}:
Message: ${log.message}
Exception: ${log.exceptionType ?? 'N/A'}
File: ${log.source ?? 'unknown'}${log.lineNumber ? `:${log.lineNumber}` : ''}
Request: ${log.requestMethod ?? ''} ${log.requestPath ?? ''}

Stack trace:
${log.stackTrace ?? '(no stack trace)'}"`;
}

function buildCodexPrompt(log: ErrorLogResponse): string {
  return `codex "Fix ${log.exceptionType ?? 'error'} in ${log.appName} at ${log.source ?? 'unknown'}${log.lineNumber ? `:${log.lineNumber}` : ''}
${log.message}

${log.stackTrace ?? ''}"`;
}

function formatAbsolute(iso: string) {
  try {
    return format(parseISO(iso), "dd/MM/yyyy 'às' HH:mm:ss", { locale: ptBR });
  } catch {
    return iso;
  }
}

export function ErrorLogDetail({ logId, onClose, onResolved }: Props) {
  const queryClient = useQueryClient();
  const [resolveNote, setResolveNote] = useState('');
  const [showNoteInput, setShowNoteInput] = useState(false);

  const { data: log, isLoading } = useQuery({
    queryKey: ['error-log', logId],
    queryFn: () => errorLogsService.getById(logId!),
    enabled: !!logId,
    staleTime: 30_000,
  });

  const resolveMutation = useMutation({
    mutationFn: ({ id, note }: { id: string; note?: string }) =>
      errorLogsService.resolve(id, note),
    onMutate: async ({ id }) => {
      // Optimistic update: marca como resolvido imediatamente na UI
      await queryClient.cancelQueries({ queryKey: ['error-log', id] });
      const prev = queryClient.getQueryData<ErrorLogResponse>(['error-log', id]);
      queryClient.setQueryData<ErrorLogResponse>(['error-log', id], old =>
        old ? { ...old, isResolved: true, resolvedAt: new Date().toISOString(), resolutionNote: resolveNote } : old
      );
      return { prev };
    },
    onError: (_err, { id }, context) => {
      // Rollback se falhar
      if (context?.prev) queryClient.setQueryData(['error-log', id], context.prev);
      toast.error('Erro ao resolver log. Tente novamente.');
    },
    onSuccess: (data, { id }) => {
      queryClient.setQueryData(['error-log', id], data);
      queryClient.invalidateQueries({ queryKey: ['error-logs'] });
      queryClient.invalidateQueries({ queryKey: ['error-log-stats'] });
      toast.success('Log marcado como resolvido.');
      setShowNoteInput(false);
      setResolveNote('');
      onResolved?.(id);
    },
  });

  const handleResolve = () => {
    if (!log) return;
    resolveMutation.mutate({ id: log.id, note: resolveNote || undefined });
  };

  const card = { background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)', borderRadius: 8 };

  return (
    <Sheet open={!!logId} onOpenChange={open => !open && onClose()}>
      <SheetContent
        side="right"
        className="w-full sm:max-w-2xl overflow-y-auto p-0"
        style={{ background: '#0D1A11', borderLeft: '1px solid rgba(255,255,255,0.08)' }}
      >
        {isLoading || !log ? (
          <div className="p-6 space-y-4">
            <Skeleton className="h-6 w-40" style={{ background: 'rgba(255,255,255,0.08)' }} />
            <Skeleton className="h-4 w-full" style={{ background: 'rgba(255,255,255,0.06)' }} />
            <Skeleton className="h-4 w-3/4" style={{ background: 'rgba(255,255,255,0.06)' }} />
            <Skeleton className="h-32 w-full" style={{ background: 'rgba(255,255,255,0.06)' }} />
          </div>
        ) : (
          <>
            {/* Header */}
            <SheetHeader className="px-6 py-4 sticky top-0 z-10" style={{ background: '#0D1A11', borderBottom: '1px solid rgba(255,255,255,0.08)' }}>
              <div className="flex items-start gap-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <ErrorLevelBadge level={log.level} />
                    <span className="text-xs px-2 py-0.5 rounded-full" style={{ background: 'rgba(255,255,255,0.07)', color: '#9CA3AF' }}>
                      {log.appName}
                    </span>
                    {log.isResolved && (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-green-500/15 text-green-400 flex items-center gap-1">
                        <CheckCircle className="h-3 w-3" /> Resolvido
                      </span>
                    )}
                  </div>
                  <SheetTitle className="text-sm font-medium text-left" style={{ color: '#F9FAFB' }}>
                    {log.exceptionType ?? 'Erro sem tipo'}
                  </SheetTitle>
                </div>
              </div>
            </SheetHeader>

            <div className="p-6 space-y-5">
              {/* Mensagem */}
              <div>
                <p className="text-xs mb-1" style={{ color: '#6B7280' }}>Mensagem</p>
                <p className="text-sm break-words" style={{ color: '#E5E7EB' }}>{log.message}</p>
              </div>

              {/* Metadados */}
              <div className="grid grid-cols-2 gap-3 text-xs">
                {[
                  { label: 'App', value: log.appName },
                  { label: 'Ambiente', value: log.environment },
                  { label: 'Arquivo', value: log.source ? `${log.source}${log.lineNumber ? `:${log.lineNumber}` : ''}` : '-' },
                  { label: 'Ocorrências', value: log.occurrenceCount.toLocaleString('pt-BR') },
                  { label: 'Primeira vez', value: formatAbsolute(log.firstSeenAt) },
                  { label: 'Última vez', value: formatAbsolute(log.lastSeenAt) },
                  ...(log.requestMethod ? [{ label: 'Requisição', value: `${log.requestMethod} ${log.requestPath ?? ''}` }] : []),
                  ...(log.userId ? [{ label: 'User ID', value: log.userId }] : []),
                ].map(m => (
                  <div key={m.label} className="rounded-md p-2.5" style={card}>
                    <p style={{ color: '#6B7280' }}>{m.label}</p>
                    <p className="mt-0.5 truncate font-medium" style={{ color: '#D1D5DB' }} title={m.value}>{m.value}</p>
                  </div>
                ))}
              </div>

              {/* Stack trace */}
              {log.stackTrace && (
                <div>
                  <div className="flex items-center justify-between mb-1.5">
                    <p className="text-xs flex items-center gap-1.5" style={{ color: '#6B7280' }}>
                      <Terminal className="h-3.5 w-3.5" /> Stack trace
                    </p>
                    <CopyButton text={log.stackTrace} label="Copiar stack trace" />
                  </div>
                  <pre
                    className="text-xs rounded-lg p-4 overflow-x-auto overflow-y-auto max-h-64 font-mono leading-5"
                    style={{ background: 'rgba(0,0,0,0.4)', color: '#86EFAC', border: '1px solid rgba(255,255,255,0.07)' }}
                  >
                    {log.stackTrace}
                  </pre>
                </div>
              )}

              {/* Additional data */}
              {log.additionalData && (
                <div>
                  <p className="text-xs mb-1.5" style={{ color: '#6B7280' }}>Dados adicionais</p>
                  <pre
                    className="text-xs rounded-lg p-3 overflow-x-auto font-mono"
                    style={{ background: 'rgba(0,0,0,0.3)', color: '#D1D5DB', border: '1px solid rgba(255,255,255,0.06)' }}
                  >
                    {(() => { try { return JSON.stringify(JSON.parse(log.additionalData!), null, 2); } catch { return log.additionalData; } })()}
                  </pre>
                </div>
              )}

              {/* Resolução existente */}
              {log.isResolved && log.resolutionNote && (
                <div className="rounded-lg p-3" style={{ background: 'rgba(74,222,128,0.08)', border: '1px solid rgba(74,222,128,0.15)' }}>
                  <p className="text-xs text-green-400 mb-1">Nota de resolução</p>
                  <p className="text-sm" style={{ color: '#D1D5DB' }}>{log.resolutionNote}</p>
                </div>
              )}

              {/* Prompts para Claude/Codex */}
              <div>
                <p className="text-xs mb-2" style={{ color: '#6B7280' }}>Prompts para correção</p>
                <div className="space-y-2">
                  {[
                    { label: 'Claude Code CLI', prompt: buildClaudeCliPrompt(log) },
                    { label: 'Codex',           prompt: buildCodexPrompt(log) },
                  ].map(({ label, prompt }) => (
                    <div key={label} className="rounded-lg p-3" style={{ background: 'rgba(0,0,0,0.25)', border: '1px solid rgba(255,255,255,0.07)' }}>
                      <div className="flex items-center justify-between mb-2">
                        <span className="text-xs font-medium" style={{ color: '#9CA3AF' }}>{label}</span>
                        <CopyButton text={prompt} label={`Copiar prompt ${label}`} />
                      </div>
                      <pre
                        className="text-xs text-green-300 font-mono overflow-x-auto whitespace-pre-wrap break-words leading-4"
                        style={{ maxHeight: 80, overflow: 'hidden', maskImage: 'linear-gradient(to bottom, black 60%, transparent)' }}
                      >
                        {prompt}
                      </pre>
                    </div>
                  ))}
                </div>
              </div>

              {/* Ação: Resolver */}
              {!log.isResolved && (
                <div className="rounded-lg p-4" style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)' }}>
                  {!showNoteInput ? (
                    <Button
                      className="w-full gap-2"
                      style={{ background: 'rgba(74,222,128,0.15)', color: '#4ADE80', border: '1px solid rgba(74,222,128,0.25)' }}
                      onClick={() => setShowNoteInput(true)}
                      disabled={resolveMutation.isPending}
                    >
                      <CheckCircle className="h-4 w-4" />
                      Marcar como Resolvido
                    </Button>
                  ) : (
                    <div className="space-y-3">
                      <Textarea
                        value={resolveNote}
                        onChange={e => setResolveNote(e.target.value)}
                        placeholder="Nota de resolução (opcional) — ex: corrigido no commit abc123"
                        rows={2}
                        className="text-sm resize-none"
                        style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#F9FAFB' }}
                        autoFocus
                      />
                      <div className="flex gap-2">
                        <Button
                          className="flex-1 gap-2"
                          style={{ background: 'rgba(74,222,128,0.15)', color: '#4ADE80', border: '1px solid rgba(74,222,128,0.25)' }}
                          onClick={handleResolve}
                          disabled={resolveMutation.isPending}
                        >
                          <CheckCircle className="h-4 w-4" />
                          {resolveMutation.isPending ? 'Resolvendo…' : 'Confirmar'}
                        </Button>
                        <Button
                          variant="ghost"
                          onClick={() => setShowNoteInput(false)}
                          disabled={resolveMutation.isPending}
                          style={{ color: '#9CA3AF' }}
                        >
                          Cancelar
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}
