'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { formatDateShort } from '@/lib/date-utils';
import {
  EmailQueueItemResponse,
  EmailQueueStatus,
  getEmailQueue,
  PagedEmailQueueResponse,
} from '@/services/outreach';
import {
  AlertTriangle,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Mail,
  RefreshCw,
  Search,
  Send,
  X,
} from 'lucide-react';
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';

const EMAIL_QUEUE_STATUS_CFG: Record<EmailQueueStatus, { label: string; dot: string; badge: string; icon: React.ReactNode }> = {
  [EmailQueueStatus.Queued]:     { label: 'Na fila',     dot: 'bg-yellow-400', badge: 'border-yellow-200 bg-yellow-50 text-yellow-700',  icon: <Send className="h-3 w-3" /> },
  [EmailQueueStatus.Processing]: { label: 'Processando', dot: 'bg-blue-400',   badge: 'border-blue-200 bg-blue-50 text-blue-700',        icon: <Loader2 className="h-3 w-3 animate-spin" /> },
  [EmailQueueStatus.Sent]:       { label: 'Enviado',     dot: 'bg-green-400',  badge: 'border-green-200 bg-green-50 text-green-700',     icon: <Mail className="h-3 w-3" /> },
  [EmailQueueStatus.Failed]:     { label: 'Falhou',      dot: 'bg-red-400',    badge: 'border-red-200 bg-red-50 text-red-700',           icon: <AlertTriangle className="h-3 w-3" /> },
};

type QueueSortKey = 'recipientName' | 'subject' | 'status' | 'scheduledAt' | 'sentAt' | 'attemptCount';

const STATUS_FILTER_OPTIONS: { value: EmailQueueStatus | null; label: string }[] = [
  { value: null,                        label: 'Todos'        },
  { value: EmailQueueStatus.Queued,     label: 'Na fila'      },
  { value: EmailQueueStatus.Sent,       label: 'Enviados'     },
  { value: EmailQueueStatus.Failed,     label: 'Falhas'       },
  { value: EmailQueueStatus.Processing, label: 'Processando'  },
];

function SortIcon({ col, sortKey, dir }: { col: QueueSortKey; sortKey: QueueSortKey; dir: 'asc' | 'desc' }) {
  if (col !== sortKey) return <span className="ml-1 opacity-20 text-[10px]">↕</span>;
  return <span className="ml-1 text-[10px] text-slate-700">{dir === 'asc' ? '↑' : '↓'}</span>;
}

export function EmailQueueSection() {
  const [queue, setQueue]         = useState<PagedEmailQueueResponse | null>(null);
  const [loading, setLoading]     = useState(false);
  const [page, setPage]           = useState(1);
  const [pageSize, setPageSize]   = useState(20);
  const [statusFilter, setStatusFilter] = useState<EmailQueueStatus | null>(null);
  const [search, setSearch]       = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [sortKey, setSortKey]     = useState<QueueSortKey>('scheduledAt');
  const [sortDir, setSortDir]     = useState<'asc' | 'desc'>('desc');
  const searchTimerRef            = useRef<ReturnType<typeof setTimeout> | null>(null);

  const loadQueue = useCallback(async (p: number, ps: number, sf: EmailQueueStatus | null, s: string) => {
    setLoading(true);
    try {
      const data = await getEmailQueue(p, ps, sf ?? undefined, s);
      setQueue(data);
    } catch {
      toast.error('Erro ao carregar fila de emails.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadQueue(page, pageSize, statusFilter, search);
  }, [loadQueue, page, pageSize, statusFilter, search]);

  const handleSearchChange = (val: string) => {
    setSearchInput(val);
    if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      setSearch(val);
      setPage(1);
    }, 350);
  };

  const handleStatusFilter = (sf: EmailQueueStatus | null) => {
    setStatusFilter(sf);
    setPage(1);
  };

  const handlePageSize = (ps: number) => {
    setPageSize(ps);
    setPage(1);
  };

  const toggleSort = (key: QueueSortKey) => {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortKey(key); setSortDir('asc'); }
  };

  const sortedItems: EmailQueueItemResponse[] = queue ? [...queue.items].sort((a, b) => {
    let va: string | number = '';
    let vb: string | number = '';
    if (sortKey === 'recipientName') { va = a.recipientName; vb = b.recipientName; }
    else if (sortKey === 'subject')  { va = a.subject; vb = b.subject; }
    else if (sortKey === 'status')   { va = a.status; vb = b.status; }
    else if (sortKey === 'scheduledAt') { va = a.scheduledAt ?? ''; vb = b.scheduledAt ?? ''; }
    else if (sortKey === 'sentAt')   { va = a.sentAt ?? ''; vb = b.sentAt ?? ''; }
    else if (sortKey === 'attemptCount') { va = a.attemptCount; vb = b.attemptCount; }
    if (va < vb) return sortDir === 'asc' ? -1 : 1;
    if (va > vb) return sortDir === 'asc' ? 1 : -1;
    return 0;
  }) : [];

  const firstItem = queue ? (queue.page - 1) * queue.pageSize + 1 : 0;
  const lastItem  = queue ? Math.min(queue.page * queue.pageSize, queue.totalCount) : 0;

  const ColHead = ({ col, label, className = '' }: { col: QueueSortKey; label: string; className?: string }) => (
    <TableHead
      className={`cursor-pointer select-none whitespace-nowrap hover:bg-slate-50 transition-colors ${className}`}
      onClick={() => toggleSort(col)}
    >
      <span className="flex items-center gap-0.5">
        {label}
        <SortIcon col={col} sortKey={sortKey} dir={sortDir} />
      </span>
    </TableHead>
  );

  return (
    <Card className="mt-6">
      <CardHeader className="pb-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <CardTitle className="text-base flex items-center gap-2">
              <Mail className="h-4 w-4 text-slate-500" />
              Fila de Emails
            </CardTitle>
            {queue && (
              <Badge variant="secondary" className="font-normal tabular-nums">
                {queue.totalCount.toLocaleString('pt-BR')}
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-2">
            <div className="relative">
              <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                placeholder="Buscar destinatário ou assunto…"
                className="h-8 pl-8 w-56 text-sm"
                value={searchInput}
                onChange={e => handleSearchChange(e.target.value)}
              />
              {searchInput && (
                <button
                  className="absolute right-2 top-2 text-muted-foreground hover:text-foreground"
                  onClick={() => { setSearchInput(''); setSearch(''); setPage(1); }}
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
            <select
              className="h-8 rounded-md border bg-background px-2 text-xs"
              value={pageSize}
              onChange={e => handlePageSize(Number(e.target.value))}
            >
              {[10, 20, 50, 100].map(n => (
                <option key={n} value={n}>{n} / pág</option>
              ))}
            </select>
            <Button
              variant="ghost"
              size="sm"
              className="h-8 w-8 p-0"
              onClick={() => loadQueue(page, pageSize, statusFilter, search)}
              disabled={loading}
              title="Atualizar"
            >
              {loading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap gap-1.5 pt-1">
          {STATUS_FILTER_OPTIONS.map(opt => {
            const active = statusFilter === opt.value;
            return (
              <button
                key={String(opt.value)}
                onClick={() => handleStatusFilter(opt.value)}
                className={`flex items-center gap-1 rounded-full px-3 py-0.5 text-xs font-medium border transition-colors ${
                  active
                    ? 'bg-slate-900 text-white border-slate-900'
                    : 'bg-background text-slate-500 border-slate-200 hover:border-slate-400 hover:text-slate-700'
                }`}
              >
                {opt.value !== null && (
                  <span className={`inline-block h-1.5 w-1.5 rounded-full ${EMAIL_QUEUE_STATUS_CFG[opt.value].dot}`} />
                )}
                {opt.label}
                {active && queue && (
                  <span className="ml-0.5 opacity-70">({queue.totalCount})</span>
                )}
              </button>
            );
          })}
        </div>
      </CardHeader>

      <CardContent className="p-0">
        {loading && !queue && (
          <div className="space-y-1 p-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full rounded" />
            ))}
          </div>
        )}

        {!loading && queue && queue.totalCount === 0 && (
          <div className="flex flex-col items-center justify-center py-16 text-slate-400">
            <Mail className="h-10 w-10 mb-3 opacity-20" />
            <p className="font-medium text-slate-500 text-sm">
              {search || statusFilter !== null ? 'Nenhum resultado para os filtros aplicados.' : 'Nenhum email na fila de envio.'}
            </p>
            {(search || statusFilter !== null) && (
              <button
                className="mt-2 text-xs text-primary underline"
                onClick={() => { setSearchInput(''); setSearch(''); setStatusFilter(null); setPage(1); }}
              >
                Limpar filtros
              </button>
            )}
          </div>
        )}

        {queue && queue.totalCount > 0 && (
          <>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="bg-slate-50/60">
                    <ColHead col="recipientName" label="Destinatário"  className="pl-5 w-[200px]" />
                    <ColHead col="subject"       label="Assunto"       className="w-[240px]" />
                    <ColHead col="status"        label="Status"        className="w-[130px]" />
                    <ColHead col="scheduledAt"   label="Agendado"      className="w-[150px]" />
                    <ColHead col="sentAt"        label="Enviado"       className="w-[150px]" />
                    <ColHead col="attemptCount"  label="Tentativas"    className="w-[90px] text-right" />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading && sortedItems.length === 0
                    ? Array.from({ length: pageSize }).map((_, i) => (
                        <TableRow key={i}>
                          {Array.from({ length: 6 }).map((__, j) => (
                            <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>
                          ))}
                        </TableRow>
                      ))
                    : sortedItems.map(item => {
                        const cfg = EMAIL_QUEUE_STATUS_CFG[item.status] ?? EMAIL_QUEUE_STATUS_CFG[EmailQueueStatus.Queued];
                        return (
                          <TableRow key={item.id} className={`hover:bg-slate-50/60 transition-colors ${loading ? 'opacity-60' : ''}`}>
                            <TableCell className="pl-5">
                              <div className="font-medium text-slate-900 truncate max-w-[180px] text-sm" title={item.recipientName}>
                                {item.recipientName}
                              </div>
                              <div className="text-xs text-slate-400 truncate max-w-[180px]" title={item.recipientEmail}>
                                {item.recipientEmail}
                              </div>
                            </TableCell>
                            <TableCell>
                              <span className="text-sm text-slate-600 truncate block max-w-[220px]" title={item.subject}>
                                {item.subject}
                              </span>
                            </TableCell>
                            <TableCell>
                              <Badge variant="outline" className={`gap-1 text-xs font-medium ${cfg.badge}`}>
                                {cfg.icon}
                                {cfg.label}
                              </Badge>
                              {item.status === EmailQueueStatus.Failed && item.lastError && (
                                <p
                                  className="text-[11px] text-red-400 mt-0.5 max-w-[140px] truncate cursor-help"
                                  title={item.lastError}
                                >
                                  {item.lastError}
                                </p>
                              )}
                            </TableCell>
                            <TableCell className="text-xs text-slate-500 whitespace-nowrap">
                              {formatDateShort(item.scheduledAt)}
                            </TableCell>
                            <TableCell className="text-xs text-slate-500 whitespace-nowrap">
                              {item.sentAt ? formatDateShort(item.sentAt) : <span className="text-slate-300">—</span>}
                            </TableCell>
                            <TableCell className="text-right pr-5">
                              <span className={`text-sm font-semibold tabular-nums ${item.attemptCount > 1 ? 'text-amber-600' : 'text-slate-500'}`}>
                                {item.attemptCount}
                              </span>
                            </TableCell>
                          </TableRow>
                        );
                      })
                  }
                </TableBody>
              </Table>
            </div>

            <div className="flex items-center justify-between border-t px-5 py-3">
              <p className="text-xs text-slate-500">
                {queue.totalCount > 0
                  ? `Mostrando ${firstItem.toLocaleString('pt-BR')}–${lastItem.toLocaleString('pt-BR')} de ${queue.totalCount.toLocaleString('pt-BR')} registros`
                  : 'Sem registros'}
              </p>
              <div className="flex items-center gap-1">
                <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(1)}             disabled={!queue.hasPreviousPage || loading} title="Primeira página">«</Button>
                <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p - 1)}    disabled={!queue.hasPreviousPage || loading}><ChevronLeft className="h-3.5 w-3.5" /></Button>
                <span className="px-3 text-xs text-slate-600 whitespace-nowrap">
                  {queue.page} / {queue.totalPages}
                </span>
                <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(p => p + 1)}    disabled={!queue.hasNextPage || loading}><ChevronRight className="h-3.5 w-3.5" /></Button>
                <Button variant="outline" size="sm" className="h-7 w-7 p-0" onClick={() => setPage(queue.totalPages)} disabled={!queue.hasNextPage || loading} title="Última página">»</Button>
              </div>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
