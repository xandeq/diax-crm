'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { getContactEmailStats } from '@/services/emailStats';
import { Mail, TrendingUp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { formatDate } from '@/lib/date-utils';

interface EngagementStatsCardProps {
  customerId: string;
}

export function EngagementStatsCard({ customerId }: EngagementStatsCardProps) {
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await getContactEmailStats(customerId);
        setStats(data);
      } catch (err: any) {
        // Falha silenciosa se Brevo não estiver configurado
        setError(err?.message ?? 'Não foi possível carregar estatísticas de engajamento');
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, [customerId]);

  if (loading) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Engajamento de Email
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-6 w-full" />
          <Skeleton className="h-6 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="bg-muted/30 border-muted">
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Engajamento de Email
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">{error}</p>
        </CardContent>
      </Card>
    );
  }

  if (!stats) {
    return (
      <Card className="bg-muted/30 border-muted">
        <CardHeader className="pb-3">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Engajamento de Email
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-xs text-muted-foreground">Sem dados de engajamento ainda. Nenhum email foi enviado.</p>
        </CardContent>
      </Card>
    );
  }

  // Engagement level
  const engagementLevel = stats.engagementLevel ?? 0; // 0=Low, 1=Medium, 2=High
  const engagementLabelMap: Record<number, string> = {
    0: 'Baixo',
    1: 'Médio',
    2: 'Alto',
  };
  const engagementColorMap: Record<number, string> = {
    0: 'bg-red-100 text-red-700 border-red-200',
    1: 'bg-amber-100 text-amber-700 border-amber-200',
    2: 'bg-green-100 text-green-700 border-green-200',
  };
  const engagementEmojiMap: Record<number, string> = {
    0: '🔴',
    1: '🟡',
    2: '🟢',
  };

  // Calculate rates
  const openRate = stats.totalDelivered > 0 ? Math.round((stats.totalOpened / stats.totalDelivered) * 100) : 0;
  const clickRate = stats.totalDelivered > 0 ? Math.round((stats.totalClicked / stats.totalDelivered) * 100) : 0;
  const ctor = stats.totalOpened > 0 ? Math.round((stats.totalClicked / stats.totalOpened) * 100) : 0; // Click-Through Open Rate

  // Format cached time
  let cachedTimeAgo = '';
  if (stats.calculatedAt) {
    const calcDate = new Date(stats.calculatedAt);
    const now = new Date();
    const diffMinutes = Math.floor((now.getTime() - calcDate.getTime()) / 60000);
    if (diffMinutes < 60) {
      cachedTimeAgo = `há ${diffMinutes}min`;
    } else {
      const diffHours = Math.floor(diffMinutes / 60);
      cachedTimeAgo = diffHours < 24 ? `há ${diffHours}h` : formatDate(stats.calculatedAt);
    }
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <Mail className="h-4 w-4" />
            Engajamento de Email
          </CardTitle>
          <Badge className={engagementColorMap[engagementLevel]}>
            {engagementEmojiMap[engagementLevel]} {engagementLabelMap[engagementLevel]}
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Stats grid: Sent | Delivered | Opened | Clicked */}
        <div className="grid grid-cols-4 gap-2 text-center text-xs">
          <div className="rounded-lg bg-muted/50 p-2">
            <div className="text-lg font-bold tabular-nums">{stats.totalSent}</div>
            <div className="text-muted-foreground">Enviados</div>
          </div>
          <div className="rounded-lg bg-muted/50 p-2">
            <div className="text-lg font-bold tabular-nums">{stats.totalDelivered}</div>
            <div className="text-muted-foreground">Entregues</div>
          </div>
          <div className="rounded-lg bg-blue-50 p-2">
            <div className="text-lg font-bold tabular-nums text-blue-700">{stats.totalOpened}</div>
            <div className="text-muted-foreground">Abertos</div>
          </div>
          <div className="rounded-lg bg-green-50 p-2">
            <div className="text-lg font-bold tabular-nums text-green-700">{stats.totalClicked}</div>
            <div className="text-muted-foreground">Cliques</div>
          </div>
        </div>

        {/* Open Rate */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-xs">
            <span className="font-medium">Taxa de Abertura</span>
            <span className="font-semibold text-blue-600">{openRate}%</span>
          </div>
          <Progress value={openRate} className="h-2" />
        </div>

        {/* Click Rate */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-xs">
            <span className="font-medium">Taxa de Clique</span>
            <span className="font-semibold text-green-600">{clickRate}%</span>
          </div>
          <Progress value={clickRate} className="h-2" />
        </div>

        {/* CTOR: Click-Through Open Rate */}
        <div className="space-y-1.5 rounded-lg bg-purple-50 p-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-medium flex items-center gap-1">
              <TrendingUp className="h-3 w-3" />
              CTOR (Cliques / Abertos)
            </span>
            <span className="font-semibold text-purple-700">{ctor}%</span>
          </div>
        </div>

        {/* Cached time */}
        {cachedTimeAgo && (
          <p className="text-[11px] text-muted-foreground">
            Dados atualizados: {cachedTimeAgo}
          </p>
        )}

        {/* Bounced / Unsubscribed warnings */}
        {(stats.totalBounced > 0 || stats.totalUnsubscribed > 0) && (
          <div className="space-y-1 border-t pt-2 text-xs text-muted-foreground">
            {stats.totalBounced > 0 && <div>⚠️ {stats.totalBounced} email(s) devolvido(s)</div>}
            {stats.totalUnsubscribed > 0 && <div>⚠️ {stats.totalUnsubscribed} descadastrado(s)</div>}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
