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
import { toast } from 'sonner';

// ── Status config ─────────────────────────────────────────────────────────────

const CAMPAIGN_STATUS: Record<number, { label: string; className: string }> = {
  0: { label: 'Rascunho',  className: 'bg-slate-100 text-slate-600 border-slate-200' },
  1: { label: 'Agendada',  className: 'bg-blue-100 text-blue-700 border-blue-200' },
  2: { label: 'Enviando',  className: 'bg-amber-100 text-amber-700 border-amber-200' },
  3: { label: 'Concluída', className: 'bg-green-100 text-green-700 border-green-200' },
  4: { label: 'Cancelada', className: 'bg-red-100 text-red-700 border-red-200' },
  5: { label: 'Falhou',    className: 'bg-red-100 text-red-700 border-red-200' },
};

const RECIPIENT_STATUS: Record<number, { label: string; className: string }> = {
  0: { label: 'Na fila',     className: 'bg-slate-100 text-slate-600 border-slate-200' },
  1: { label: 'Processando', className: 'bg-blue-100 text-blue-700 border-blue-200' },
  2: { label: 'Enviado',     className: 'bg-green-100 text-green-700 border-green-200' },
  3: { label: 'Falhou',      className: 'bg-red-100 text-red-700 border-red-200' },
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
  iconBg: string;
}

function FunnelCard({ step }: { step: FunnelStep }) {
  return (
    <div className={`flex flex-1 flex-col items-center gap-1 rounded-xl border px-4 py-5 text-center ${step.colorClass}`}>
      <div className="text-3xl font-bold tabular-nums">{step.value.toLocaleString('pt-BR')}</div>
      <div className="text-sm font-medium">{step.label}</div>
      <div className="mt-1 text-xs text-muted-foreground">{step.rateLabel}</div>
      <div className="text-xs font-semibold">{step.rateValue}</div>
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
    } catch (e: any) {
      setError(e?.message ?? 'Erro ao carregar destinatários.');
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
    } catch (e: any) {
      toast.error('Erro ao obter IDs: ' + e.message);
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
            variant="outline"
            onClick={handleCreateRemarketing}
            disabled={creatingRemarketing || totalCount === 0}
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
        <div className="flex items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span className="flex-1">{error}</span>
          <Button variant="outline" size="sm" onClick={() => load(page)} className="border-red-300 text-red-700">
            Tentar novamente
          </Button>
        </div>
      )}

      {/* Table */}
      <div className="overflow-x-auto rounded-md border">
        <Table>
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
                  <TableCell><Skeleton className="h-4 w-32" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-44" /></TableCell>
                  <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                  {showErrorColumn && <TableCell><Skeleton className="h-4 w-40" /></TableCell>}
                </TableRow>
              ))
            ) : recipients.length === 0 ? (
              <TableRow>
                <TableCell colSpan={showErrorColumn ? 7 : 6} className="py-12 text-center">
                  <div className="flex flex-col items-center gap-2 text-muted-foreground">
                    <Users className="h-8 w-8 opacity-25" />
                    <p className="text-sm">Nenhum destinatário neste grupo.</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              recipients.map((r) => {
                const statusCfg = RECIPIENT_STATUS[r.status] ?? RECIPIENT_STATUS[0];
                return (
                  <TableRow key={r.id} className="hover:bg-slate-50/60">
                    <TableCell className="pl-4 font-medium text-slate-900 max-w-[160px]">
                      <span className="truncate block" title={r.recipientName}>
                        {r.recipientName}
                      </span>
                    </TableCell>
                    <TableCell className="text-slate-600 max-w-[200px]">
                      <span className="truncate block text-sm" title={r.recipientEmail}>
                        {r.recipientEmail}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline" className={`text-xs ${statusCfg.className}`}>
                        {statusCfg.label}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-slate-500 whitespace-nowrap">
                      {formatDate(r.sentAt)}
                    </TableCell>
                    <TableCell className="text-sm text-slate-500 whitespace-nowrap">
                      {formatDate(r.deliveredAt)}
                    </TableCell>
                    <TableCell className="text-sm text-slate-500 whitespace-nowrap">
                      {r.openedAt ? (
                        <span>
                          {formatDate(r.openedAt)}
                          {r.readCount > 1 && (
                            <span className="ml-1 text-xs font-medium text-amber-600">
                              ({r.readCount}x)
                            </span>
                          )}
                        </span>
                      ) : '-'}
                    </TableCell>
                    {showErrorColumn && (
                      <TableCell className="text-sm text-red-600 max-w-[200px]">
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
          <span className="text-sm text-muted-foreground">
            Página {page} de {totalPages} &middot; {totalCount} destinatário{totalCount !== 1 ? 's' : ''}
          </span>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" disabled={page === 1 || loading} onClick={() => setPage(p => p - 1)}>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="sm" disabled={page >= totalPages || loading} onClick={() => setPage(p => p + 1)}>
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function CampaignReportClient({ params }: { params: { id: string } }) {
  const { id } = params;
  const [campaign, setCampaign] = useState<EmailCampaignResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getCampaignById(id);
      setCampaign(data);
    } catch (e: any) {
      setError(e?.message ?? 'Erro ao carregar campanha.');
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
          colorClass: 'border-slate-200 bg-slate-50 text-slate-800',
          iconBg: 'bg-slate-200',
        },
        {
          label: 'Enviados',
          value: campaign.sentCount,
          rateLabel: 'do total',
          rateValue: pct(campaign.sentCount, campaign.totalRecipients),
          colorClass: 'border-blue-200 bg-blue-50 text-blue-900',
          iconBg: 'bg-blue-200',
        },
        {
          label: 'Entregues',
          value: campaign.deliveredCount,
          rateLabel: 'dos enviados',
          rateValue: pct(campaign.deliveredCount, campaign.sentCount),
          colorClass: 'border-green-200 bg-green-50 text-green-900',
          iconBg: 'bg-green-200',
        },
        {
          label: 'Abertos',
          value: campaign.openCount,
          rateLabel: 'dos entregues',
          rateValue: pct(campaign.openCount, campaign.deliveredCount),
          colorClass: 'border-amber-200 bg-amber-50 text-amber-900',
          iconBg: 'bg-amber-200',
        },
        {
          label: 'Cliques',
          value: campaign.clickCount,
          rateLabel: 'dos entregues',
          rateValue: pct(campaign.clickCount, campaign.deliveredCount),
          colorClass: 'border-purple-200 bg-purple-50 text-purple-900',
          iconBg: 'bg-purple-200',
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

  // ── Render ───────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6 px-4 py-5 sm:px-6 max-w-7xl mx-auto">

      {/* Header */}
      <div>
        <Link href="/campanhas">
          <Button variant="ghost" size="sm" className="mb-3 -ml-2 text-slate-500 hover:text-slate-800">
            <ArrowLeft className="h-4 w-4 mr-1.5" />
            Campanhas
          </Button>
        </Link>

        {loading ? (
          <div className="space-y-2">
            <Skeleton className="h-8 w-72" />
            <Skeleton className="h-4 w-96" />
          </div>
        ) : error ? (
          <div className="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span className="flex-1">{error}</span>
            <Button variant="outline" size="sm" onClick={load} className="border-red-300 text-red-700">
              Tentar novamente
            </Button>
          </div>
        ) : campaign ? (
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div>
              <div className="flex items-center gap-3 flex-wrap">
                <h1 className="text-2xl font-bold tracking-tight text-slate-900">
                  {campaign.name}
                </h1>
                {statusCfg && (
                  <Badge variant="outline" className={`text-xs font-medium ${statusCfg.className}`}>
                    {statusCfg.label}
                  </Badge>
                )}
              </div>
              <p className="mt-1 text-slate-500 text-sm">{campaign.subject}</p>
              <p className="mt-1 text-xs text-slate-400">
                Criado em {formatDate(campaign.createdAt)}
                {campaign.updatedAt && ` · Atualizado em ${formatDate(campaign.updatedAt)}`}
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={load} title="Recarregar">
              <RefreshCw className="h-4 w-4" />
            </Button>
          </div>
        ) : null}
      </div>

      {/* Funnel */}
      {!loading && !error && campaign && (
        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500 flex items-center gap-2">
            <Send className="h-4 w-4" />
            Funil de Entrega
          </h2>
          <div className="flex flex-wrap gap-3">
            {funnelSteps.map((step) => (
              <FunnelCard key={step.label} step={step} />
            ))}
          </div>
        </div>
      )}

      {/* Funnel skeleton */}
      {loading && (
        <div className="flex flex-wrap gap-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex flex-1 flex-col items-center gap-2 rounded-xl border p-5">
              <Skeleton className="h-8 w-16" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-3 w-12" />
            </div>
          ))}
        </div>
      )}

      {/* Tabs */}
      {!loading && !error && campaign && (
        <div>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500 flex items-center gap-2">
            <Users className="h-4 w-4" />
            Destinatários
          </h2>
          <Card>
            <CardContent className="p-4 sm:p-6">
              <Tabs defaultValue="todos">
                <TabsList className="mb-4 flex-wrap h-auto gap-1">
                  <TabsTrigger value="todos">
                    Todos ({countTodos})
                  </TabsTrigger>
                  <TabsTrigger value="opened" className="text-green-700 data-[state=active]:bg-green-100">
                    <MousePointerClick className="mr-1 h-3.5 w-3.5" />
                    Abriram ({countAbriram})
                  </TabsTrigger>
                  <TabsTrigger value="not_opened" className="text-slate-600">
                    Nao Abriram ({countNaoAbriram})
                  </TabsTrigger>
                  <TabsTrigger value="problems" className="text-red-600 data-[state=active]:bg-red-50">
                    <AlertCircle className="mr-1 h-3.5 w-3.5" />
                    Problemas ({countProblemas})
                  </TabsTrigger>
                </TabsList>

                <TabsContent value="todos">
                  <RecipientTable
                    campaignId={id}
                    filter={null}
                    showErrorColumn={false}
                    showRemarketingButton={false}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="opened">
                  <RecipientTable
                    campaignId={id}
                    filter="opened"
                    showErrorColumn={false}
                    showRemarketingButton={true}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="not_opened">
                  <RecipientTable
                    campaignId={id}
                    filter="not_opened"
                    showErrorColumn={false}
                    showRemarketingButton={true}
                    campaignName={campaign.name}
                  />
                </TabsContent>

                <TabsContent value="problems">
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
