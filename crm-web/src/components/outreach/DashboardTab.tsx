'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  AlertTriangle,
  Flame,
  Loader2,
  Mail,
  MessageSquare,
  RefreshCw,
  Send,
  Snowflake,
  Thermometer,
  TrendingUp,
  Users,
} from 'lucide-react';

import { OutreachDashboardResponse } from '@/services/outreach';
import { StatCard, StatusIndicator } from '@/components/outreach/OutreachShared';

// ─── Dashboard Tab ────────────────────────────────────────────────────────────

export interface DashboardTabProps {
  dashboard: OutreachDashboardResponse | null;
  loading: boolean;
  onSegment: () => void;
  onSend: () => void;
  segmenting: boolean;
  sending: boolean;
  onRefresh: () => void;
}

export function DashboardTab({
  dashboard,
  loading,
  onSegment,
  onSend,
  segmenting,
  sending,
  onRefresh,
}: DashboardTabProps) {
  if (loading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-24 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (!dashboard) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <AlertTriangle className="h-10 w-10 mb-3 text-amber-500" />
        <p className="font-medium">Não foi possível carregar o dashboard.</p>
        <Button variant="outline" className="mt-4" onClick={onRefresh}>
          <RefreshCw className="mr-2 h-4 w-4" /> Tentar novamente
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Action Buttons */}
      <div className="flex flex-wrap gap-3 items-center justify-between">
        <div className="flex gap-3">
          <Button
            onClick={onSegment}
            disabled={segmenting || !dashboard.segmentationEnabled}
            variant="outline"
          >
            {segmenting ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <TrendingUp className="mr-2 h-4 w-4" />
            )}
            {segmenting ? 'Segmentando...' : 'Segmentar Agora'}
          </Button>
          <Button
            onClick={onSend}
            disabled={sending || !dashboard.sendEnabled}
          >
            {sending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Send className="mr-2 h-4 w-4" />
            )}
            {sending ? 'Enviando...' : 'Enviar Campanha'}
          </Button>
        </div>
        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw className="mr-2 h-4 w-4" /> Atualizar
        </Button>
      </div>

      {/* Module Status */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Status dos Módulos</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-6">
          <StatusIndicator enabled={dashboard.importEnabled} label="Importação" />
          <StatusIndicator enabled={dashboard.segmentationEnabled} label="Segmentação" />
          <StatusIndicator enabled={dashboard.sendEnabled} label="Envio" />
          <StatusIndicator enabled={dashboard.whatsAppSendEnabled} label="WhatsApp" />
        </CardContent>
      </Card>

      {/* Lead Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Leads</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-5 gap-4">
          <StatCard
            title="Total de Leads"
            value={dashboard.totalLeads}
            icon={<Users className="h-4 w-4 text-slate-600" />}
            accent="bg-slate-100"
          />
          <StatCard
            title="Quentes"
            value={dashboard.hotLeads}
            icon={<Flame className="h-4 w-4 text-red-600" />}
            accent="bg-red-50"
            description="Alto potencial de conversão"
          />
          <StatCard
            title="Mornos"
            value={dashboard.warmLeads}
            icon={<Thermometer className="h-4 w-4 text-orange-600" />}
            accent="bg-orange-50"
            description="Potencial médio"
          />
          <StatCard
            title="Frios"
            value={dashboard.coldLeads}
            icon={<Snowflake className="h-4 w-4 text-blue-600" />}
            accent="bg-blue-50"
            description="Baixo engajamento"
          />
          <StatCard
            title="Sem Segmento"
            value={dashboard.unsegmentedLeads}
            icon={<Users className="h-4 w-4 text-slate-400" />}
            accent="bg-slate-50"
            description="Aguardando segmentação"
          />
        </div>
      </div>

      {/* Email Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Emails Enviados</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            title="Hoje"
            value={dashboard.emailsSentToday}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
          <StatCard
            title="Esta Semana"
            value={dashboard.emailsSentThisWeek}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
          <StatCard
            title="Este Mês"
            value={dashboard.emailsSentThisMonth}
            icon={<Mail className="h-4 w-4 text-indigo-600" />}
            accent="bg-indigo-50"
          />
        </div>
      </div>

      {/* WhatsApp Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">WhatsApp</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            title="Enviados Hoje"
            value={dashboard.whatsAppSentToday}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
          <StatCard
            title="Enviados Esta Semana"
            value={dashboard.whatsAppSentThisWeek}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
          <StatCard
            title="Leads Prontos (WhatsApp)"
            value={dashboard.whatsAppReadyCount}
            icon={<MessageSquare className="h-4 w-4 text-green-600" />}
            accent="bg-green-50"
          />
        </div>
      </div>

      {/* Queue Stats */}
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">Fila de Envio</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <StatCard
            title="Aguardando"
            value={dashboard.pendingInQueue}
            icon={<Send className="h-4 w-4 text-yellow-600" />}
            accent="bg-yellow-50"
            description="Na fila para envio"
          />
          <StatCard
            title="Falhas"
            value={dashboard.failedInQueue}
            icon={<AlertTriangle className="h-4 w-4 text-red-600" />}
            accent="bg-red-50"
            description="Envios com erro"
          />
        </div>
      </div>
    </div>
  );
}
