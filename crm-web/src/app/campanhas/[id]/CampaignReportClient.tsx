'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
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
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import {
  CampaignRecipient,
  EmailCampaignResponse,
  getCampaignById,
  getCampaignRecipientCustomerIds,
  getCampaignRecipientsByFilter,
} from '@/services/emailMarketing';
import {
  AlertCircle,
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Mail,
  MousePointerClick,
  RefreshCw,
  Send,
  Users,
} from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { toast } from 'sonner';

// ── Status config ─────────────────────────────────────────────────────────────

const CAMPAIGN_STATUS: Record<number, { label: string; className: string }> = {
  0: { label: 'Rascunho',  className: 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20' },
  1: { label: 'Agendada',  className: 'bg-indigo-500/10 text-indigo-300 border-indigo-500/20' },
  2: { label: 'Enviando',  className: 'bg-amber-500/10 text-amber-300 border-amber-500/20' },
  3: { label: 'Concluída', className: 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20' },
  4: { label: 'Cancelada', className: 'bg-rose-500/10 text-rose-300 border-rose-500/20' },
  5: { label: 'Falhou',    className: 'bg-red-500/10 text-red-300 border-red-500/20' },
};

const RECIPIENT_STATUS: Record<number, { label: string; className: string }> = {
  0: { label: 'Na fila',     className: 'bg-zinc-500/10 text-zinc-300 border-zinc-500/20' },
  1: { label: 'Processando', className: 'bg-indigo-500/10 text-indigo-300 border-indigo-500/20' },
  2: { label: 'Enviado',     className: 'bg-emerald-500/10 text-emerald-300 border-emerald-500/20' },
  3: { label: 'Falhou',      className: 'bg-rose-500/10 text-rose-300 border-rose-500/20' },
};

// ── Helpers ───────────────────────────────────────────────────────────────────

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function pct(num: number, den: number): string {
  if (den === 0) return '—';
  return Math.round((num / den) * 100) + '%';
}

// ── Funnel card ───────────────────────────────────────────────────────────────

interface FunnelStep {
  label: string;
  value: number;
  rateLabel: string;
  rateValue: string;
  colorClass: string;
}

function FunnelCard({ step }: { step: FunnelStep }) {
  return (
    <div className={`flex flex-1 flex-col items-center gap-1 rounded-xl border px-4 py-5 text-center transition-all duration-200 hover:scale-[1.02] ${step.colorClass}`}>
      <div className="text-3xl font-bold tabular-nums">{step.value.toLocaleString('pt-BR')}</div>
      <div className="text-sm font-semibold">{step.label}</div>
      <div className="mt-1 text-xs opacity-60">{step.rateLabel}</div>
      <div className="text-xs font-bold">{step.rateValue}</div>
    </div>
  );
}

// ── Recipient table ───────────────────────────────────────────────────────────

interface RecipientTableProps {
  campaignId: string;
  filter: string | null;
  showErrorColumn: boolean;
  showRemarketingButton: boolean;
  campaignName: string;
}

function RecipientTable({
  campaignId,
  filter,
  showErrorColumn,
  showRemarketingButton,
  campaignName,
}: RecipientTableProps) {
  const router = useRouter();
  const [recipients, setRecipients] = useState<CampaignRecipient[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [creatingRemarketing, setCreatingRemarketing] = useState(false);

  const load = useCallback(async (p: number) => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCampaignRecipientsByFilter(campaignId, filter, p, 50);
      setRecipients(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar destinatários.');
    } finally {
      setLoading(false);
    }
  }, [campaignId, filter]);

  useEffect(() => {
    setPage(1);
  }, [filter]);

  useEffect(() => {
    load(page);
  }, [load, page]);

  const handleCreateRemarketing = async () => {
    setCreatingRemarketing(true);
    try {
      const data = await getCampaignRecipientCustomerIds(campaignId, filter);
      if (!data.count) {
        toast.error('Nenhum destinatário encontrado para este grupo.');
        return;
      }
      const filterLabel =
        filter === 'opened'
          ? `que abriram a campanha "${campaignName}"`
          : `que não abriram a campanha "${campaignName}"`;
      sessionStorage.setItem('remarketing_customer_ids', JSON.stringify(data.customerIds));
      sessionStorage.setItem('remarketing_label', filterLabel);
      router.push('/email-marketing');
    } catch (e: unknown) {
      toast.error('Erro ao obter IDs: ' + (e instanceof Error ? e.message : 'Erro inesperado'));
    } finally {
      setCreatingRemarketing(false);
    }
  };

  return (
    <div className="space-y-3">
      {/* Action button */}
      {showRemarketingButton && (
        <div className="flex justify-end">
          <Button
            size="sm"
            onClick={handleCreateRemarketing}
            disabled={creatingRemarketing || totalCount === 0}
            className="bg-[#00D4AA] text-[#050B08] hover:bg-[#00bda0] font-semibold text-xs"
          >
            {creatingRemarketing ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Mail className="mr-2 h-4 w-4" />
            )}
            Criar campanha para este grupo ({totalCount}) →
          </Button>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="flex items-center gap-3 rounded-lg border border-red-500/20 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span className="flex-1">{error}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => load(page)}
            className="border-red-500/30 text-red-300 hover:bg-red-500/20"
          >
            Tentar novamente
          </Button>
        </div>
      )}

      {/* Table */}
      <div className="overflow-x-auto rounded-xl border border-white/10 bg-white/[0.01]">
        <Table className="responsive-table">
          <TableHeader>
            <TableRow>
              <TableHead className="pl-4">Nome</TableHead>
              <TableHead>E-mail</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="whitespace-nowrap">Enviado em</TableHead>
              <TableHead className="whitespace-nowrap">Entregue em</TableHead>
              <TableHead className="whitespace-nowrap">Aberto em</TableHead>
              {showErrorColumn && <TableHead>Erro</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 8 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell><Skeleton className="h-4 w-32 shimmer-bg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-44 shimmer-bg" /></TableCell>
                  <TableCell><Skeleton className="h-5 w-20 rounded-full shimmer-bg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28 shimmer-bg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28 shimmer-bg" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28 shimmer-bg" /></TableCell>
                  {showErrorColumn && <TableCell><Skeleton className="h-4 w-40 shimmer-bg" /></TableCell>}
                </TableRow>
              ))
            ) : recipients.length === 0 ? (
              <TableRow>
                <TableCell colSpan={showErrorColumn ? 7 : 6} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-zinc-500">
                    <Users className="h-8 w-8 opacity-25" />
                    <p className="text-sm">Nenhum destinatário neste grupo.</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              recipients.map((r) => {
                const statusCfg = RECIPIENT_STATUS[r.status] ?? RECIPIENT_STATUS[0];
                return (
                  <TableRow key={r.id} className="hover:bg-white/[0.04]">
                    <TableCell className="pl-4 font-semibold text-zinc-100 max-w-[160px]" data-label="Nome">
                      <span className="truncate block" title={r.recipientName}>
                        {r.recipientName}
                      </span>
                    </TableCell>
                    <TableCell className="text-zinc-300 max-w-[200px]" data-label="E-mail">
                      <span className="truncate block text-sm" title={r.recipientEmail}>
                        {r.recipientEmail}
                      </span>
                    </TableCell>
                    <TableCell data-label="Status">
                      <Badge variant="outline" className={`text-xs font-semibold px-2 py-0.5 rounded-full ${statusCfg.className}`}>
                        {statusCfg.label}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-zinc-400 whitespace-nowrap" data-label="Enviado em">
                      {formatDate(r.sentAt)}
                    </TableCell>
                    <TableCell className="text-sm text-zinc-400 whitespace-nowrap" data-label="Entregue em">
                      {formatDate(r.deliveredAt)}
                    </TableCell>
                    <TableCell className="text-sm text-zinc-400 whitespace-nowrap" data-label="Aberto em">
                      {r.openedAt ? (
                        <span className="inline-flex items-center">
                          {formatDate(r.openedAt)}
                          {r.readCount > 1 && (
                            <span className="ml-1 text-xs font-bold text-amber-400">
                              ({r.readCount}x)
                            </span>
                          )}
                        </span>
                      ) : '-'}
                    </TableCell>
                    {showErrorColumn && (
                      <TableCell className="text-sm text-rose-400 max-w-[200px]" data-label="Erro">
                        <span className="truncate block" title={r.lastError ?? ''}>
                          {r.lastError ?? '-'}
                        </span>
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between pt-1">
          <span className="text-sm text-zinc-400">
            Página <span className="font-semibold text-zinc-100">{page}</span> de <span className="font-semibold text-zinc-100">{totalPages}</span> &middot; {totalCount} destinatário{totalCount !== 1 ? 's' : ''}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page === 1 || loading}
              onClick={() => setPage(p => p - 1)}
              className="border-white/10 hover:bg-white/5 text-zinc-300 h-9 w-9 p-0"
              title="Página Anterior"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages || loading}
              onClick={() => setPage(p => p + 1)}
              className="border-white/10 hover:bg-white/5 text-zinc-300 h-9 w-9 p-0"
              title="Próxima Página"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function CampaignReportClient() {
  const { id } = useParams<{ id: string }>();
  const [campaign, setCampaign] = useState<EmailCampaignResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCampaignById(id);
      setCampaign(data);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar campanha.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  // ── Derived funnel data ──────────────────────────────────────────────────────

  const funnelSteps: FunnelStep[] = campaign
    ? [
        {
          label: 'Total',
          value: campaign.totalRecipients,
          rateLabel: 'destinatários',
          rateValue: '100%',
          colorClass: 'border-white/10 bg-white/[0.02] text-zinc-100',
        },
        {
          label: 'Enviados',
          value: campaign.sentCount,
          rateLabel: 'do total',
          rateValue: pct(campaign.sentCount, campaign.totalRecipients),
          colorClass: 'border-blue-500/20 bg-blue-500/5 text-blue-300',
        },
        {
          label: 'Entregues',
          value: campaign.deliveredCount,
          rateLabel: 'dos enviados',
          rateValue: pct(campaign.deliveredCount, campaign.sentCount),
          colorClass: 'border-[#00D4AA]/20 bg-[#00D4AA]/5 text-[#00D4AA]',
        },
        {
          label: 'Abertos',
          value: campaign.openCount,
          rateLabel: 'dos entregues',
          rateValue: pct(campaign.openCount, campaign.deliveredCount),
          colorClass: 'border-amber-500/20 bg-amber-500/5 text-amber-300',
        },
        {
          label: 'Cliques',
          value: campaign.clickCount,
          rateLabel: 'dos entregues',
          rateValue: pct(campaign.clickCount, campaign.deliveredCount),
          colorClass: 'border-purple-500/20 bg-purple-500/5 text-purple-300',
        },
      ]
    : [];

  // ── Tab counts (approximate) ─────────────────────────────────────────────────
  const countTodos      = campaign?.totalRecipients ?? 0;
  const countAbriram    = campaign?.openCount ?? 0;
  const countNaoAbriram = campaign ? Math.max(0, (campaign.sentCount - campaign.openCount)) : 0;
  const countProblemas  = campaign
    ? campaign.failedCount + campaign.bounceCount + campaign.unsubscribeCount
    : 0;

  const statusCfg = campaign ? (CAMPAIGN_STATUS[campaign.status] ?? CAMPAIGN_STATUS[0]) : null;

  return (
    <div className="space-y-6 px-4 py-5 sm:px-6 max-w-7xl mx-auto">

      {/* Header */}
      <div>
        <Link href="/campanhas">
          <Button variant="ghost" size="sm" className="mb-3 -ml-2 text-zinc-400 hover:text-zinc-100 hover:bg-white/5">
            <ArrowLeft className="h-4 w-4 mr-1.5" />
            Voltar para Campanhas
          </Button>
        </Link>

        {loading ? (
          <div className="space-y-2">
            <Skeleton className="h-8 w-72 shimmer-bg" />
            <Skeleton className="h-4 w-96 shimmer-bg" />
          </div>
        ) : error ? (
          <div className="flex items-center gap-3 rounded-lg border border-red-500/20 bg-red-500/10 px-4 py-3 text-sm text-red-300">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span className="flex-1">{error}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={load}
              className="border-red-500/30 text-red-300 hover:bg-red-500/20"
            >
              Tentar novamente
            </Button>
          </div>
        ) : campaign ? (
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div>
              <div className="flex items-center gap-3 flex-wrap">
                <h1 className="text-2xl font-bold tracking-tight text-zinc-100">
                  {campaign.name}
                </h1>
                {statusCfg && (
                  <Badge variant="outline" className={`text-xs font-semibold px-2 py-0.5 rounded-full ${statusCfg.className}`}>
                    {statusCfg.label}
                  </Badge>
                )}
              </div>
              <p className="mt-1 text-zinc-400 text-sm">{campaign.subject}</p>
              <p className="mt-1 text-xs text-zinc-500">
                Criado em {formatDate(campaign.createdAt)}
                {campaign.updatedAt && ` · Atualizado em ${formatDate(campaign.updatedAt)}`}
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={load}
              title="Recarregar"
              className="border-white/10 hover:bg-white/5 text-zinc-300"
            >
              <RefreshCw className="h-4 w-4" />
            </Button>
          </div>
        ) : null}
      </div>

      {/* Funnel */}
      {!loading && !error && campaign && (
        <div className="space-y-3">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-zinc-400 flex items-center gap-2">
            <Send className="h-4 w-4 text-[#00D4AA]" />
            Funil de Entrega
          </h2>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
            {funnelSteps.map((step) => (
              <FunnelCard key={step.label} step={step} />
            ))}
          </div>
        </div>
      )}

      {/* Funnel skeleton */}
      {loading && (
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex flex-col items-center gap-2 rounded-xl border border-white/10 p-5 bg-white/[0.01]">
              <Skeleton className="h-8 w-16 shimmer-bg" />
              <Skeleton className="h-4 w-20 shimmer-bg" />
              <Skeleton className="h-3 w-12 shimmer-bg" />
            </div>
          ))}
        </div>
      )}

      {/* Tabs */}
      {!loading && !error && campaign && (
        <div className="space-y-3">
          <h2 className="text-sm font-semibold uppercase tracking-wider text-zinc-400 flex items-center gap-2">
            <Users className="h-4 w-4 text-[#00D4AA]" />
            Destinatários
          </h2>
          <Card className="border border-white/10 bg-white/[0.02]">
            <CardContent className="p-4 sm:p-6">
              <Tabs defaultValue="todos">
                <TabsList className="mb-4 flex-wrap h-auto gap-1.5 p-1 bg-white/5 border border-white/5 rounded-lg">
                  <TabsTrigger
                    value="todos"
                    className="px-3 py-1.5 text-xs rounded-md data-[state=active]:bg-white/10 data-[state=active]:text-zinc-100 text-zinc-400 font-semibold"
                  >
                    Todos ({countTodos})
                  </TabsTrigger>
                  <TabsTrigger
                    value="opened"
                    className="px-3 py-1.5 text-xs rounded-md text-emerald-400 data-[state=active]:bg-emerald-500/15 data-[state=active]:text-emerald-300 font-semibold"
                  >
                    <MousePointerClick className="mr-1 h-3.5 w-3.5" />
                    Abriram ({countAbriram})
                  </TabsTrigger>
                  <TabsTrigger
                    value="not_opened"
                    className="px-3 py-1.5 text-xs rounded-md text-zinc-400 data-[state=active]:bg-white/10 data-[state=active]:text-zinc-200 font-semibold"
                  >
                    Não Abriram ({countNaoAbriram})
                  </TabsTrigger>
                  <TabsTrigger
                    value="problems"
                    className="px-3 py-1.5 text-xs rounded-md text-rose-400 data-[state=active]:bg-rose-500/15 data-[state=active]:text-rose-300 font-semibold"
                  >
                    <AlertCircle className="mr-1 h-3.5 w-3.5" />
                    Problemas ({countProblemas})
                  </TabsTrigger>
                </TabsList>

                <TabsContent value="todos" className="mt-0">
                  <RecipientTable
                    campaignId={id}
                    filter={null}
                    showErrorColumn={false}
                    showRemarketingButton={false}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="opened" className="mt-0">
                  <RecipientTable
                    campaignId={id}
                    filter="opened"
                    showErrorColumn={false}
                    showRemarketingButton={true}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="not_opened" className="mt-0">
                  <RecipientTable
                    campaignId={id}
                    filter="not_opened"
                    showErrorColumn={false}
                    showRemarketingButton={true}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="problems" className="mt-0">
                  <RecipientTable
                    campaignId={id}
                    filter="problems"
                    showErrorColumn={true}
                    showRemarketingButton={false}
                    campaignName={campaign.name}
                  />
                </TabsContent>
              </Tabs>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}
