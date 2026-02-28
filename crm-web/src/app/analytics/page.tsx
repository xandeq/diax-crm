'use client';

import { useState, useEffect } from 'react';
import { apiFetch } from '@/services/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';
import {
  Mail,
  Send,
  Eye,
  MousePointerClick,
  AlertCircle,
  UserX,
  TrendingUp,
  Calendar,
  Loader2,
  RefreshCw,
} from 'lucide-react';
import Link from 'next/link';

interface OverallStats {
  totalCampaigns: number;
  totalEmailsSent: number;
  totalDelivered: number;
  totalOpened: number;
  totalClicks: number;
  totalBounces: number;
  totalUnsubscribes: number;
  openRate: number;
  clickRate: number;
  bounceRate: number;
}

interface CampaignStats {
  id: string;
  name: string;
  subject: string;
  createdAt: string;
  totalRecipients: number;
  sentCount: number;
  deliveredCount: number;
  openCount: number;
  clickCount: number;
  bounceCount: number;
  unsubscribeCount: number;
  failedCount: number;
  openRate: number;
  clickRate: number;
  clickToOpenRate: number;
}

interface DailyEngagement {
  date: string;
  sent: number;
  opened: number;
  clicked: number;
}

interface EngagementTrend {
  dailyData: DailyEngagement[];
}

interface AnalyticsSummary {
  overallStats: OverallStats;
  recentCampaigns: CampaignStats[];
  engagementTrend: EngagementTrend;
}

export default function AnalyticsPage() {
  const [analytics, setAnalytics] = useState<AnalyticsSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [days, setDays] = useState(30);

  const loadAnalytics = async () => {
    setIsLoading(true);
    try {
      const data = await apiFetch<AnalyticsSummary>(
        `/api/v1/email-campaigns/analytics?days=${days}`
      );
      setAnalytics(data);
    } catch (error) {
      console.error('Erro ao carregar analytics:', error);
      toast.error('Erro ao carregar dados de analytics');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadAnalytics();
  }, [days]);

  if (isLoading) {
    return (
      <div className="p-8">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-center h-64">
            <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
          </div>
        </div>
      </div>
    );
  }

  if (!analytics) {
    return (
      <div className="p-8">
        <div className="max-w-7xl mx-auto">
          <Card>
            <CardContent className="p-8 text-center">
              <AlertCircle className="h-12 w-12 text-slate-400 mx-auto mb-4" />
              <p className="text-slate-600">Erro ao carregar dados de analytics</p>
              <Button onClick={loadAnalytics} className="mt-4" variant="outline">
                <RefreshCw className="h-4 w-4 mr-2" />
                Tentar novamente
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  const { overallStats, recentCampaigns, engagementTrend } = analytics;

  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-slate-900">Analytics de Email Marketing</h1>
            <p className="text-slate-600 mt-2">
              Visão geral do desempenho das suas campanhas
            </p>
          </div>
          <div className="flex items-center gap-3">
            <select
              value={days}
              onChange={(e) => setDays(Number(e.target.value))}
              className="px-4 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value={7}>Últimos 7 dias</option>
              <option value={30}>Últimos 30 dias</option>
              <option value={90}>Últimos 90 dias</option>
              <option value={365}>Último ano</option>
            </select>
            <Button onClick={loadAnalytics} variant="outline" size="sm">
              <RefreshCw className="h-4 w-4 mr-2" />
              Atualizar
            </Button>
          </div>
        </div>

        {/* Overall Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
              <CardTitle className="text-sm font-medium text-slate-600">
                Total de Emails Enviados
              </CardTitle>
              <Send className="h-4 w-4 text-blue-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{overallStats.totalEmailsSent.toLocaleString()}</div>
              <p className="text-xs text-slate-500 mt-1">
                {overallStats.totalCampaigns} {overallStats.totalCampaigns === 1 ? 'campanha' : 'campanhas'}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
              <CardTitle className="text-sm font-medium text-slate-600">Taxa de Abertura</CardTitle>
              <Eye className="h-4 w-4 text-green-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{overallStats.openRate.toFixed(1)}%</div>
              <p className="text-xs text-slate-500 mt-1">
                {overallStats.totalOpened.toLocaleString()} aberturas
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
              <CardTitle className="text-sm font-medium text-slate-600">Taxa de Cliques</CardTitle>
              <MousePointerClick className="h-4 w-4 text-purple-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{overallStats.clickRate.toFixed(1)}%</div>
              <p className="text-xs text-slate-500 mt-1">
                {overallStats.totalClicks.toLocaleString()} cliques
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
              <CardTitle className="text-sm font-medium text-slate-600">Taxa de Bounce</CardTitle>
              <AlertCircle className="h-4 w-4 text-red-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{overallStats.bounceRate.toFixed(1)}%</div>
              <p className="text-xs text-slate-500 mt-1">
                {overallStats.totalBounces.toLocaleString()} bounces
              </p>
            </CardContent>
          </Card>
        </div>

        {/* Additional Stats */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Mail className="h-5 w-5 text-blue-600" />
                Entrega e Engajamento
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-600">Entregues</span>
                <span className="font-semibold">{overallStats.totalDelivered.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-600">Abertos</span>
                <span className="font-semibold">{overallStats.totalOpened.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-600">Cliques</span>
                <span className="font-semibold">{overallStats.totalClicks.toLocaleString()}</span>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <UserX className="h-5 w-5 text-red-600" />
                Problemas e Descadastros
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-600">Bounces</span>
                <span className="font-semibold text-red-600">{overallStats.totalBounces.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-600">Descadastros</span>
                <span className="font-semibold text-orange-600">{overallStats.totalUnsubscribes.toLocaleString()}</span>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Engagement Trend Chart */}
        {engagementTrend.dailyData.length > 0 && (
          <Card className="mb-8">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="h-5 w-5 text-blue-600" />
                Tendência de Engajamento
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {engagementTrend.dailyData.map((day, index) => (
                  <div key={index} className="flex items-center gap-4">
                    <div className="w-24 text-sm text-slate-600">
                      {new Date(day.date).toLocaleDateString('pt-BR', {
                        day: '2-digit',
                        month: 'short',
                      })}
                    </div>
                    <div className="flex-1">
                      <div className="flex gap-2 items-center">
                        <div className="flex-1 bg-slate-100 rounded-full h-2 overflow-hidden">
                          <div
                            className="bg-blue-500 h-full"
                            style={{ width: `${Math.min((day.sent / Math.max(...engagementTrend.dailyData.map(d => d.sent))) * 100, 100)}%` }}
                          />
                        </div>
                        <span className="text-xs text-slate-500 w-12 text-right">{day.sent}</span>
                      </div>
                    </div>
                    <div className="flex gap-4 text-xs">
                      <span className="text-green-600">👁️ {day.opened}</span>
                      <span className="text-purple-600">🖱️ {day.clicked}</span>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Recent Campaigns */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base flex items-center gap-2">
              <Calendar className="h-5 w-5 text-blue-600" />
              Campanhas Recentes
            </CardTitle>
          </CardHeader>
          <CardContent>
            {recentCampaigns.length === 0 ? (
              <div className="text-center py-8 text-slate-500">
                <Mail className="h-12 w-12 mx-auto mb-3 text-slate-300" />
                <p>Nenhuma campanha encontrada no período selecionado</p>
                <Link href="/email-marketing">
                  <Button className="mt-4" variant="outline">
                    Criar primeira campanha
                  </Button>
                </Link>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-slate-200">
                      <th className="text-left py-3 px-4 text-sm font-medium text-slate-600">Campanha</th>
                      <th className="text-left py-3 px-4 text-sm font-medium text-slate-600">Data</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-slate-600">Enviados</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-slate-600">Taxa Abertura</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-slate-600">Taxa Cliques</th>
                      <th className="text-right py-3 px-4 text-sm font-medium text-slate-600">Bounces</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentCampaigns.map((campaign) => (
                      <tr key={campaign.id} className="border-b border-slate-100 hover:bg-slate-50">
                        <td className="py-3 px-4">
                          <div>
                            <div className="font-medium text-sm">{campaign.name}</div>
                            <div className="text-xs text-slate-500">{campaign.subject}</div>
                          </div>
                        </td>
                        <td className="py-3 px-4 text-sm text-slate-600">
                          {new Date(campaign.createdAt).toLocaleDateString('pt-BR', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric',
                          })}
                        </td>
                        <td className="py-3 px-4 text-right">
                          <div className="text-sm font-medium">{campaign.sentCount}</div>
                          <div className="text-xs text-slate-500">
                            de {campaign.totalRecipients}
                          </div>
                        </td>
                        <td className="py-3 px-4 text-right">
                          <Badge
                            variant="outline"
                            className={
                              campaign.openRate >= 20
                                ? 'bg-green-50 text-green-700 border-green-200'
                                : campaign.openRate >= 10
                                ? 'bg-yellow-50 text-yellow-700 border-yellow-200'
                                : 'bg-red-50 text-red-700 border-red-200'
                            }
                          >
                            {campaign.openRate.toFixed(1)}%
                          </Badge>
                          <div className="text-xs text-slate-500 mt-1">
                            {campaign.openCount} aberturas
                          </div>
                        </td>
                        <td className="py-3 px-4 text-right">
                          <Badge
                            variant="outline"
                            className={
                              campaign.clickRate >= 5
                                ? 'bg-purple-50 text-purple-700 border-purple-200'
                                : campaign.clickRate >= 2
                                ? 'bg-blue-50 text-blue-700 border-blue-200'
                                : 'bg-slate-50 text-slate-700 border-slate-200'
                            }
                          >
                            {campaign.clickRate.toFixed(1)}%
                          </Badge>
                          <div className="text-xs text-slate-500 mt-1">
                            {campaign.clickCount} cliques
                          </div>
                        </td>
                        <td className="py-3 px-4 text-right">
                          <span className="text-sm font-medium text-red-600">
                            {campaign.bounceCount}
                          </span>
                          {campaign.unsubscribeCount > 0 && (
                            <div className="text-xs text-orange-600 mt-1">
                              {campaign.unsubscribeCount} descadastro(s)
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
