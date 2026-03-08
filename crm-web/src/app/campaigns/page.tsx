'use client';

import { useState, useEffect, useCallback, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import {
  ArrowLeft,
  Users,
  Send,
  CheckCircle2,
  Eye,
  MousePointerClick,
  AlertTriangle,
  Loader2,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import {
  getCampaignById,
  getCampaignRecipients,
  EmailCampaignResponse,
  CampaignRecipient,
  PagedCampaignRecipients,
} from '@/services/emailMarketing';

const statusLabels: Record<number, string> = {
  0: 'Na fila',
  1: 'Processando',
  2: 'Enviado',
  3: 'Falhou',
};

const statusColors: Record<number, string> = {
  0: 'bg-yellow-50 text-yellow-700 border-yellow-200',
  1: 'bg-blue-50 text-blue-700 border-blue-200',
  2: 'bg-green-50 text-green-700 border-green-200',
  3: 'bg-red-50 text-red-700 border-red-200',
};

const campaignStatusLabels: Record<number, string> = {
  0: 'Rascunho',
  1: 'Agendada',
  2: 'Processando',
  3: 'Concluida',
  4: 'Cancelada',
};

const campaignStatusColors: Record<number, string> = {
  0: 'bg-slate-100 text-slate-700',
  1: 'bg-blue-100 text-blue-700',
  2: 'bg-yellow-100 text-yellow-700',
  3: 'bg-green-100 text-green-700',
  4: 'bg-red-100 text-red-700',
};

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function calcRate(part: number, total: number): string {
  if (total === 0) return '0.0';
  return ((part / total) * 100).toFixed(1);
}

function CampaignDetailContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const campaignId = searchParams.get('id');

  const [campaign, setCampaign] = useState<EmailCampaignResponse | null>(null);
  const [recipients, setRecipients] = useState<PagedCampaignRecipients | null>(null);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);

  const loadData = useCallback(async (p: number) => {
    if (!campaignId) return;
    try {
      setLoading(true);
      const [campaignData, recipientsData] = await Promise.all([
        getCampaignById(campaignId),
        getCampaignRecipients(campaignId, p, 50),
      ]);
      setCampaign(campaignData);
      setRecipients(recipientsData);
    } catch {
      toast.error('Erro ao carregar dados da campanha');
    } finally {
      setLoading(false);
    }
  }, [campaignId]);

  useEffect(() => {
    loadData(page);
  }, [page, loadData]);

  if (!campaignId) {
    return (
      <div className="max-w-4xl mx-auto p-6 text-center">
        <p className="text-slate-500">Nenhuma campanha selecionada.</p>
        <Button variant="outline" className="mt-4" onClick={() => router.push('/analytics')}>
          Voltar para Analytics
        </Button>
      </div>
    );
  }

  if (loading && !campaign) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    );
  }

  if (!campaign) {
    return (
      <div className="max-w-4xl mx-auto p-6 text-center">
        <p className="text-slate-500">Campanha nao encontrada.</p>
        <Button variant="outline" className="mt-4" onClick={() => router.push('/analytics')}>
          Voltar para Analytics
        </Button>
      </div>
    );
  }

  const sent = campaign.sentCount || 0;
  const delivered = campaign.deliveredCount || 0;
  const opened = campaign.openCount || 0;
  const clicked = campaign.clickCount || 0;
  const bounced = (campaign.bounceCount || 0) + (campaign.unsubscribeCount || 0);
  const total = campaign.totalRecipients || 0;

  const stats = [
    { label: 'Destinatarios', value: total, icon: Users, color: 'text-slate-600', bg: 'bg-slate-50' },
    { label: 'Enviados', value: sent, icon: Send, color: 'text-blue-600', bg: 'bg-blue-50', rate: calcRate(sent, total) },
    { label: 'Entregues', value: delivered, icon: CheckCircle2, color: 'text-emerald-600', bg: 'bg-emerald-50', rate: calcRate(delivered, sent) },
    { label: 'Abertos', value: opened, icon: Eye, color: 'text-purple-600', bg: 'bg-purple-50', rate: calcRate(opened, delivered) },
    { label: 'Clicados', value: clicked, icon: MousePointerClick, color: 'text-indigo-600', bg: 'bg-indigo-50', rate: calcRate(clicked, delivered) },
    { label: 'Bounces', value: bounced, icon: AlertTriangle, color: 'text-red-600', bg: 'bg-red-50', rate: calcRate(bounced, sent) },
  ];

  const items = recipients?.items || [];

  return (
    <div className="max-w-6xl mx-auto p-4 md:p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <Button variant="ghost" size="icon" onClick={() => router.push('/analytics')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-xl font-bold text-slate-800">{campaign.name}</h1>
            <p className="text-sm text-slate-500 mt-1">Assunto: {campaign.subject}</p>
            <div className="flex items-center gap-2 mt-2">
              <Badge className={campaignStatusColors[campaign.status] || 'bg-slate-100'}>
                {campaignStatusLabels[campaign.status] || 'Desconhecido'}
              </Badge>
              <span className="text-xs text-slate-400">
                Criada em {formatDate(campaign.createdAt)}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        {stats.map((s) => (
          <Card key={s.label} className="border-0 shadow-sm">
            <CardContent className="p-4">
              <div className="flex items-center gap-2 mb-2">
                <div className={`p-1.5 rounded-lg ${s.bg}`}>
                  <s.icon className={`h-4 w-4 ${s.color}`} />
                </div>
              </div>
              <div className="text-2xl font-bold text-slate-800">{s.value}</div>
              <div className="text-xs text-slate-500">{s.label}</div>
              {s.rate && (
                <div className="text-xs text-slate-400 mt-0.5">{s.rate}%</div>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Recipients Table */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Users className="h-5 w-5 text-blue-600" />
            Destinatarios ({recipients?.totalCount || 0})
          </CardTitle>
        </CardHeader>
        <CardContent>
          {items.length === 0 ? (
            <div className="text-center py-8 text-slate-500">
              <Users className="h-12 w-12 mx-auto mb-3 text-slate-300" />
              <p>Nenhum destinatario encontrado para esta campanha</p>
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-slate-200">
                      <th className="text-left py-3 px-4 text-sm font-medium text-slate-600">Nome</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-slate-600">Email</th>
                      <th className="text-center py-3 px-4 text-sm font-medium text-slate-600">Status</th>
                      <th className="text-center py-3 px-4 text-sm font-medium text-slate-600">Entregue</th>
                      <th className="text-center py-3 px-4 text-sm font-medium text-slate-600">Aberto</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-slate-600">Enviado em</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((r: CampaignRecipient) => (
                      <tr key={r.id} className="border-b border-slate-100 hover:bg-slate-50">
                        <td className="py-3 px-4 text-sm font-medium text-slate-800">
                          {r.recipientName || '-'}
                        </td>
                        <td className="py-3 px-4 text-sm text-slate-600">
                          {r.recipientEmail}
                        </td>
                        <td className="py-3 px-4 text-center">
                          <Badge variant="outline" className={statusColors[r.status] || ''}>
                            {statusLabels[r.status] || 'Desconhecido'}
                          </Badge>
                        </td>
                        <td className="py-3 px-4 text-center">
                          {r.deliveredAt ? (
                            <span className="inline-flex items-center gap-1 text-sm text-emerald-600">
                              <CheckCircle2 className="h-3.5 w-3.5" />
                              {formatDate(r.deliveredAt)}
                            </span>
                          ) : (
                            <span className="text-sm text-slate-400">-</span>
                          )}
                        </td>
                        <td className="py-3 px-4 text-center">
                          {r.openedAt ? (
                            <span className="inline-flex items-center gap-1 text-sm text-purple-600">
                              <Eye className="h-3.5 w-3.5" />
                              {formatDate(r.openedAt)}
                              {r.readCount > 1 && (
                                <span className="text-xs text-purple-400">({r.readCount}x)</span>
                              )}
                            </span>
                          ) : (
                            <span className="text-sm text-slate-400">-</span>
                          )}
                        </td>
                        <td className="py-3 px-4 text-sm text-slate-500">
                          {formatDate(r.sentAt)}
                          {r.lastError && (
                            <div className="text-xs text-red-500 mt-0.5 max-w-[200px] truncate" title={r.lastError}>
                              {r.lastError}
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {recipients && recipients.totalPages > 1 && (
                <div className="flex items-center justify-between mt-4 pt-4 border-t border-slate-100">
                  <span className="text-sm text-slate-500">
                    Pagina {recipients.page} de {recipients.totalPages}
                  </span>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!recipients.hasPreviousPage}
                      onClick={() => setPage((p) => p - 1)}
                    >
                      <ChevronLeft className="h-4 w-4 mr-1" />
                      Anterior
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!recipients.hasNextPage}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      Proximo
                      <ChevronRight className="h-4 w-4 ml-1" />
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default function CampaignDetailPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
      </div>
    }>
      <CampaignDetailContent />
    </Suspense>
  );
}
