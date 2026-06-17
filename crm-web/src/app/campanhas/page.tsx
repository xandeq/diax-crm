'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { PerfectGrid, GridColumn } from '@/components/data-table/PerfectGrid';
import {
  EmailCampaignResponse,
  getEmailCampaigns,
} from '@/services/emailMarketing';
import {
  AlertCircle,
  ExternalLink,
  Loader2,
  Mail,
  Plus,
  RefreshCw,
} from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import Link from 'next/link';
import { useState } from 'react';

// ── Status config ─────────────────────────────────────────────────────────────

const CAMPAIGN_STATUS: Record<
  number,
  { label: string; className: string }
> = {
  0: { label: 'Rascunho',   className: 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20' },
  1: { label: 'Agendada',   className: 'bg-indigo-500/10 text-indigo-300 border-indigo-500/20' },
  2: { label: 'Enviando',   className: 'bg-amber-500/10 text-amber-300 border-amber-500/20' },
  3: { label: 'Concluída',  className: 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20' },
  4: { label: 'Cancelada',  className: 'bg-rose-500/10 text-rose-300 border-rose-500/20' },
  5: { label: 'Falhou',     className: 'bg-red-500/10 text-red-300 border-red-500/20' },
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

// ── Page ──────────────────────────────────────────────────────────────────────

const PAGE_SIZE = 20;

export default function CampanhasPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE);

  const { data, isLoading: loading, isError, error, refetch } = useQuery({
    queryKey: ['campaigns', page, pageSize],
    queryFn: () => getEmailCampaigns(page, pageSize),
  });

  const campaigns: EmailCampaignResponse[] = data?.items ?? [];
  const totalCount = data?.totalCount ?? 0;
  const errorMessage = isError ? (error instanceof Error ? error.message : 'Erro ao carregar campanhas.') : null;

  const columns: GridColumn<EmailCampaignResponse>[] = [
    {
      id: 'name',
      header: 'Nome',
      cell: (c) => (
        <span className="font-semibold text-zinc-100 truncate block max-w-[180px]" title={c.name}>
          {c.name}
        </span>
      ),
    },
    {
      id: 'subject',
      header: 'Assunto',
      cell: (c) => (
        <span className="text-zinc-300 truncate block max-w-[220px]" title={c.subject}>
          {c.subject}
        </span>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      cell: (c) => {
        const statusCfg = CAMPAIGN_STATUS[c.status] ?? CAMPAIGN_STATUS[0];
        return (
          <Badge
            variant="outline"
            className={`text-xs font-semibold px-2 py-0.5 rounded-full ${statusCfg.className}`}
          >
            {statusCfg.label}
          </Badge>
        );
      },
    },
    {
      id: 'sentCount',
      header: 'Enviados',
      className: 'text-right',
      cell: (c) => <span className="text-zinc-200 tabular-nums">{c.sentCount}</span>,
    },
    {
      id: 'deliveredCount',
      header: 'Entregues',
      className: 'text-right',
      cell: (c) => <span className="text-zinc-200 tabular-nums">{c.deliveredCount}</span>,
    },
    {
      id: 'openCount',
      header: 'Abertos',
      className: 'text-right',
      cell: (c) => <span className="text-zinc-200 tabular-nums">{formatMetric(c.openCount, c.deliveredCount)}</span>,
    },
    {
      id: 'clickCount',
      header: 'Cliques',
      className: 'text-right',
      cell: (c) => <span className="text-zinc-200 tabular-nums">{formatMetric(c.clickCount, c.deliveredCount)}</span>,
    },
    {
      id: 'createdAt',
      header: 'Criado em',
      cell: (c) => <span className="text-zinc-400 text-sm whitespace-nowrap">{formatDateTime(c.createdAt)}</span>,
    },
    {
      id: 'actions',
      header: 'Ações',
      className: 'text-right pr-6',
      cell: (c) => (
        <Link href={`/campanhas/${c.id}`}>
          <Button variant="outline" size="sm" className="gap-1.5 border-white/10 hover:bg-white/5 text-zinc-300">
            <ExternalLink className="h-3.5 w-3.5" />
            Relatório
          </Button>
        </Link>
      ),
    },
  ];

  return (
    <div className="space-y-5 px-4 py-5 sm:px-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
            <Mail className="h-8 w-8 text-[#00D4AA]" />
            Campanhas de E-mail
          </h1>
          <p className="text-zinc-400 mt-1">
            Histórico de campanhas enviadas e seus resultados.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={loading}
            title="Recarregar"
            className="border-white/10 hover:bg-white/5 text-zinc-300"
          >
            {loading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <RefreshCw className="h-4 w-4" />
            )}
          </Button>
          <Link href="/email-marketing">
            <Button className="bg-[#00D4AA] text-[#050B08] hover:bg-[#00bda0] font-semibold">
              <Plus className="mr-2 h-4 w-4" />
              Nova Campanha
            </Button>
          </Link>
        </div>
      </div>

      {/* Error state */}
      {errorMessage && (
        <div className="flex items-center gap-3 rounded-lg border border-red-500/20 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          <AlertCircle className="h-5 w-5 shrink-0" />
          <span className="flex-1">{errorMessage}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            className="border-red-500/30 text-red-300 hover:bg-red-500/20"
          >
            Tentar novamente
          </Button>
        </div>
      )}

      {/* PerfectGrid Table */}
      <PerfectGrid
        columns={columns}
        data={campaigns}
        loading={loading}
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={Math.max(1, Math.ceil(totalCount / pageSize))}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPage(1);
        }}
        getRowId={(c) => c.id}
        itemLabel="campanhas"
        emptyTitle="Nenhuma campanha encontrada"
        emptyDescription="Crie sua primeira campanha para começar a acompanhar o desempenho de envios."
      />
    </div>
  );
}
