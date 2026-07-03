'use client';

import { RoleGuard } from '@/components/RoleGuard';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { emailOpsService, type EmailOpsProvider } from '@/services/emailOps';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Activity, AlertTriangle, RefreshCw, RotateCcw, Skull } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

// ─── Stats Cards (mesmo padrão da Central de Erros) ─────────────────────────

function StatCard({ label, value, color = '#9CA3AF', loading = false }: {
  label: string; value?: number; color?: string; loading?: boolean;
}) {
  return (
    <div className="rounded-xl p-4" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
      <p className="text-xs mb-2" style={{ color: '#6B7280' }}>{label}</p>
      {loading
        ? <Skeleton className="h-7 w-16" style={{ background: 'rgba(255,255,255,0.07)' }} />
        : <p className="text-2xl font-bold" style={{ color }}>{(value ?? 0).toLocaleString('pt-BR')}</p>
      }
    </div>
  );
}

function BreakerBadge({ p }: { p: EmailOpsProvider }) {
  if (!p.enabled) return <Badge variant="outline">desabilitado</Badge>;
  if (p.breakerOpen) return <Badge variant="destructive">breaker ABERTO</Badge>;
  if (p.breakerHalfOpen) return <Badge className="bg-amber-600 text-white">half-open</Badge>;
  return <Badge className="bg-emerald-700 text-white">ok</Badge>;
}

// ─── Página ──────────────────────────────────────────────────────────────────

export default function EmailHealthPage() {
  const qc = useQueryClient();
  const [dlqPage, setDlqPage] = useState(1);

  const { data: summary, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['email-ops-summary'],
    queryFn: () => emailOpsService.getOpsSummary(),
    staleTime: 15_000,
    placeholderData: prev => prev,
  });

  const { data: dlq, isLoading: dlqLoading } = useQuery({
    queryKey: ['email-dead-letter', dlqPage],
    queryFn: () => emailOpsService.getDeadLetter(dlqPage, 20),
    staleTime: 15_000,
    placeholderData: prev => prev,
  });

  const requeue = useMutation({
    mutationFn: (id: string) => emailOpsService.requeueDeadLetter(id),
    onSuccess: (r) => {
      toast.success(`Reprocessado via ${r.provider}.`);
      void qc.invalidateQueries({ queryKey: ['email-dead-letter'] });
      void qc.invalidateQueries({ queryKey: ['email-ops-summary'] });
    },
    onError: (e) => toast.error(`Falha ao reprocessar: ${e instanceof Error ? e.message : String(e)}`),
  });

  const q = summary?.queue;
  const totalPages = dlq ? Math.max(1, Math.ceil(dlq.totalCount / dlq.pageSize)) : 1;

  return (
    <RoleGuard allowedRoles={['Admin']}>
      <div className="space-y-5">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>
              <Activity className="inline h-5 w-5 mr-2 text-emerald-400" aria-hidden />
              Saúde do Email
            </h1>
            <p className="text-sm mt-0.5" style={{ color: '#9CA3AF' }}>
              Fila, breakers, quotas por provider e dead-letter queue
            </p>
          </div>
          <Button variant="outline" size="sm" onClick={() => refetch()} disabled={isFetching} className="gap-2">
            <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} aria-hidden />
            Atualizar
          </Button>
        </div>

        {/* Breaker global aberto */}
        {summary?.pilot.isOpen && (
          <div className="rounded-xl p-4 flex items-start gap-3" style={{ background: 'rgba(248,113,113,0.08)', border: '1px solid rgba(248,113,113,0.35)' }}>
            <AlertTriangle className="h-5 w-5 text-red-400 mt-0.5 shrink-0" aria-hidden />
            <div>
              <p className="font-semibold text-red-300">Circuit breaker GLOBAL aberto — campanhas pausadas</p>
              <p className="text-sm text-red-200/80">{summary.pilot.reason ?? 'Sem motivo registrado.'} Reset em Email Marketing PRO → Pilot.</p>
            </div>
          </div>
        )}

        {/* Stats da fila */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          <StatCard label="Na fila" value={q?.queued} color="#E5E7EB" loading={isLoading} />
          <StatCard label="Processando" value={q?.processing} color="#93C5FD" loading={isLoading} />
          <StatCard label="Aguardando retry" value={q?.failed} color="#FCD34D" loading={isLoading} />
          <StatCard label="Dead-letter" value={q?.deadLettered} color="#F87171" loading={isLoading} />
          <StatCard label="Enviados hoje" value={q?.sentToday} color="#6EE7B7" loading={isLoading} />
          <StatCard label="Última hora" value={q?.sentLastHour} color="#A5B4FC" loading={isLoading} />
        </div>

        {/* Providers */}
        <div className="rounded-xl overflow-hidden" style={{ border: '1px solid rgba(255,255,255,0.09)' }}>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Provider</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Enviados hoje</TableHead>
                <TableHead className="text-right">Limite diário</TableHead>
                <TableHead className="text-right">Na fila</TableHead>
                <TableHead>Motivo do breaker</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(summary?.providers ?? []).map(p => (
                <TableRow key={p.key}>
                  <TableCell className="font-medium">{p.provider}</TableCell>
                  <TableCell><BreakerBadge p={p} /></TableCell>
                  <TableCell className="text-right">{p.sentToday.toLocaleString('pt-BR')}</TableCell>
                  <TableCell className="text-right">{p.dailyLimit != null ? p.dailyLimit.toLocaleString('pt-BR') : '—'}</TableCell>
                  <TableCell className="text-right">{p.queued.toLocaleString('pt-BR')}</TableCell>
                  <TableCell className="text-xs text-muted-foreground max-w-[280px] truncate">{p.breakerReason ?? '—'}</TableCell>
                </TableRow>
              ))}
              {isLoading && (
                <TableRow><TableCell colSpan={6}><Skeleton className="h-6 w-full" /></TableCell></TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {/* DLQ */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold" style={{ color: '#F9FAFB' }}>
              <Skull className="inline h-4 w-4 mr-2 text-red-400" aria-hidden />
              Dead-letter queue {dlq ? `(${dlq.totalCount})` : ''}
            </h2>
            {totalPages > 1 && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Button variant="outline" size="sm" disabled={dlqPage <= 1} onClick={() => setDlqPage(p => p - 1)}>Anterior</Button>
                <span>{dlqPage}/{totalPages}</span>
                <Button variant="outline" size="sm" disabled={dlqPage >= totalPages} onClick={() => setDlqPage(p => p + 1)}>Próxima</Button>
              </div>
            )}
          </div>

          {dlqLoading ? (
            <Skeleton className="h-24 w-full" />
          ) : (dlq?.items.length ?? 0) === 0 ? (
            <div className="rounded-xl p-6 text-center text-sm" style={{ background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)', color: '#9CA3AF' }}>
              Nenhum email na DLQ — tudo entregue ou em retry. 🎉
            </div>
          ) : (
            <div className="rounded-xl overflow-hidden" style={{ border: '1px solid rgba(255,255,255,0.09)' }}>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Destinatário</TableHead>
                    <TableHead>Assunto</TableHead>
                    <TableHead>Provider</TableHead>
                    <TableHead className="text-right">Tentativas</TableHead>
                    <TableHead>Último erro</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {dlq!.items.map(item => (
                    <TableRow key={item.id}>
                      <TableCell className="font-medium">{item.recipientEmail}</TableCell>
                      <TableCell className="max-w-[220px] truncate">{item.subject}</TableCell>
                      <TableCell>{item.assignedProvider}</TableCell>
                      <TableCell className="text-right">{item.attemptCount}</TableCell>
                      <TableCell className="text-xs text-muted-foreground max-w-[280px] truncate" title={item.lastError ?? ''}>
                        {item.lastError ?? '—'}
                      </TableCell>
                      <TableCell>
                        <Button
                          variant="outline"
                          size="sm"
                          className="gap-1.5"
                          disabled={requeue.isPending}
                          onClick={() => requeue.mutate(item.id)}
                        >
                          <RotateCcw className="h-3.5 w-3.5" aria-hidden />
                          Reprocessar
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </div>

        {/* Config operacional */}
        {summary && (
          <p className="text-xs" style={{ color: '#6B7280' }}>
            Alertas Telegram: {summary.ops.telegramConfigured && summary.ops.opsAlertsEnabled ? '✅ ativos' : '⚠️ inativos'} ·
            Fallback in-cycle: {summary.ops.inCycleFallbackEnabled ? `✅ até ${summary.ops.maxFallbackProvidersPerItem} provider(s)` : '❌ desligado'} ·
            Limites do sistema: {summary.limits.daily}/dia, {summary.limits.hourly}/hora
            {summary.ops.sandboxRedirectTo ? ` · sandbox → ${summary.ops.sandboxRedirectTo}` : ''}
          </p>
        )}
      </div>
    </RoleGuard>
  );
}
