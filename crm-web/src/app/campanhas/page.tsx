'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  EmailCampaignResponse,
  getEmailCampaigns,
} from '@/services/emailMarketing';
import {
  AlertCircle,
  BarChart2,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Loader2,
  Mail,
  Plus,
  RefreshCw,
} from 'lucide-react';
import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';

// ── Status config ─────────────────────────────────────────────────────────────

const CAMPAIGN_STATUS: Record<
  number,
  { label: string; className: string }
> = {
  0: { label: 'Rascunho',   className: 'bg-slate-100 text-slate-600 border-slate-200' },
  1: { label: 'Agendada',   className: 'bg-blue-100 text-blue-700 border-blue-200' },
  2: { label: 'Enviando',   className: 'bg-amber-100 text-amber-700 border-amber-200' },
  3: { label: 'Concluída',  className: 'bg-green-100 text-green-700 border-green-200' },
  4: { label: 'Cancelada',  className: 'bg-red-100 text-red-700 border-red-200' },
  5: { label: 'Falhou',     className: 'bg-red-100 text-red-700 border-red-200' },
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatMetric(count: number, total: number): string {
  if (total === 0) return String(count);
  const pct = Math.round((count / total) * 100);
  return `${count} (${pct}%)`;
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

// ── Skeleton rows ─────────────────────────────────────────────────────────────

function TableSkeleton() {
  return (
    <>
      {Array.from({ length: 6 }).map((_, i) => (
        <TableRow key={i}>
          <TableCell><Skeleton className="h-4 w-40" /></TableCell>
          <TableCell><Skeleton className="h-4 w-56" /></TableCell>
          <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
          <TableCell><Skeleton className="h-4 w-16" /></TableCell>
          <TableCell><Skeleton className="h-4 w-16" /></TableCell>
          <TableCell><Skeleton className="h-4 w-16" /></TableCell>
          <TableCell><Skeleton className="h-4 w-16" /></TableCell>
          <TableCell><Skeleton className="h-4 w-32" /></TableCell>
          <TableCell><Skeleton className="h-4 w-24" /></TableCell>
        </TableRow>
      ))}
    </>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

const PAGE_SIZE = 20;

export default function CampanhasPage() {
  const [campaigns, setCampaigns] = useState<EmailCampaignResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const load = useCallback(async (p: number) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getEmailCampaigns(p, PAGE_SIZE);
      setCampaigns(data.items);
      setTotalCount(data.totalCount);
    } catch (e: any) {
      setError(e?.message ?? 'Erro ao carregar campanhas.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load(page);
  }, [load, page]);

  const handlePrev = () => setPage((p) => Math.max(1, p - 1));
  const handleNext = () => setPage((p) => Math.min(totalPages, p + 1));

  return (
    <div className="space-y-5 px-4 py-5 sm:px-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900 flex items-center gap-2">
            <Mail className="h-8 w-8 text-primary" />
            Campanhas de E-mail
          </h1>
          <p className="text-slate-500 mt-1">
            Histórico de campanhas enviadas e seus resultados.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => load(page)}
            disabled={loading}
            title="Recarregar"
          >
            {loading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
          </Button>
          <Link href="/email-marketing">
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Nova Campanha
            </Button>
          </Link>
        </div>
      </div>

      {/* Error state */}
      {error && (
        <div className="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          <AlertCircle className="h-5 w-5 shrink-0" />
          <span className="flex-1">{error}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => load(page)}
            className="border-red-300 text-red-700 hover:bg-red-100"
          >
            Tentar novamente
          </Button>
        </div>
      )}

      {/* Table card */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <BarChart2 className="h-5 w-5" />
            Campanhas
            {!loading && (
              <span className="text-muted-foreground font-normal text-sm">
                ({totalCount})
              </span>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="pl-6">Nome</TableHead>
                  <TableHead>Assunto</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Enviados</TableHead>
                  <TableHead className="text-right">Entregues</TableHead>
                  <TableHead className="text-right">Abertos</TableHead>
                  <TableHead className="text-right">Cliques</TableHead>
                  <TableHead>Criado em</TableHead>
                  <TableHead className="text-right pr-6">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableSkeleton />
                ) : campaigns.length === 0 && !error ? (
                  <TableRow>
                    <TableCell colSpan={9} className="py-16 text-center">
                      <div className="flex flex-col items-center gap-3 text-slate-400">
                        <Mail className="h-12 w-12 opacity-25" />
                        <p className="text-sm">
                          Nenhuma campanha encontrada. Clique em &ldquo;+ Nova Campanha&rdquo; para
                          começar.
                        </p>
                        <Link href="/email-marketing">
                          <Button variant="outline" size="sm">
                            <Plus className="mr-2 h-4 w-4" />
                            Nova Campanha
                          </Button>
                        </Link>
                      </div>
                    </TableCell>
                  </TableRow>
                ) : (
                  campaigns.map((c) => {
                    const statusCfg =
                      CAMPAIGN_STATUS[c.status] ?? CAMPAIGN_STATUS[0];
                    return (
                      <TableRow key={c.id} className="hover:bg-slate-50/60">
                        {/* Nome */}
                        <TableCell className="pl-6 font-medium text-slate-900 max-w-[180px]">
                          <span className="truncate block" title={c.name}>
                            {c.name}
                          </span>
                        </TableCell>

                        {/* Assunto */}
                        <TableCell className="text-slate-600 max-w-[220px]">
                          <span className="truncate block" title={c.subject}>
                            {c.subject}
                          </span>
                        </TableCell>

                        {/* Status */}
                        <TableCell>
                          <Badge
                            variant="outline"
                            className={`text-xs font-medium ${statusCfg.className}`}
                          >
                            {statusCfg.label}
                          </Badge>
                        </TableCell>

                        {/* Enviados */}
                        <TableCell className="text-right text-sm tabular-nums text-slate-700">
                          {c.sentCount}
                        </TableCell>

                        {/* Entregues */}
                        <TableCell className="text-right text-sm tabular-nums text-slate-700">
                          {c.deliveredCount}
                        </TableCell>

                        {/* Abertos */}
                        <TableCell className="text-right text-sm tabular-nums text-slate-700">
                          {formatMetric(c.openCount, c.deliveredCount)}
                        </TableCell>

                        {/* Cliques */}
                        <TableCell className="text-right text-sm tabular-nums text-slate-700">
                          {formatMetric(c.clickCount, c.deliveredCount)}
                        </TableCell>

                        {/* Data */}
                        <TableCell className="text-sm text-slate-500 whitespace-nowrap">
                          {formatDateTime(c.createdAt)}
                        </TableCell>

                        {/* Ações */}
                        <TableCell className="text-right pr-6">
                          <Link href={`/campanhas/${c.id}`}>
                            <Button variant="outline" size="sm" className="gap-1.5">
                              <ExternalLink className="h-3.5 w-3.5" />
                              Ver Relatório
                            </Button>
                          </Link>
                        </TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-6 py-3">
              <span className="text-sm text-muted-foreground">
                Página {page} de {totalPages} &middot; {totalCount} campanha
                {totalCount !== 1 ? 's' : ''}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page === 1 || loading}
                  onClick={handlePrev}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages || loading}
                  onClick={handleNext}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
