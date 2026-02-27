'use client';

import { useAuth } from '@/contexts/AuthContext';
import {
  connectAdAccount,
  disconnectAdAccount,
  getAdAccountSummary,
  getCampaigns,
  getInsights,
  syncAdAccount,
} from '@/services/ads';
import type {
  AdAccountSummary,
  FacebookCampaign,
  FacebookInsight,
} from '@/types/ads';
import { STATUS_COLORS } from '@/types/ads';
import {
  AlertCircle,
  BarChart3,
  CheckCircle2,
  ChevronRight,
  Eye,
  Link2,
  Link2Off,
  Loader2,
  MousePointerClick,
  RefreshCw,
  TrendingUp,
  Unplug,
  Wallet,
  Zap,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

const DATE_PRESETS = [
  { label: 'Hoje', value: 'today' },
  { label: 'Ontem', value: 'yesterday' },
  { label: 'Últimos 7 dias', value: 'last_7d' },
  { label: 'Últimos 30 dias', value: 'last_30d' },
  { label: 'Este mês', value: 'this_month' },
  { label: 'Mês passado', value: 'last_month' },
];

function formatCurrency(value: number, currency = 'BRL') {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);
}

function formatNumber(value: number) {
  return new Intl.NumberFormat('pt-BR').format(value);
}

// ===== CONNECT FORM =====

function ConnectForm({ onConnected }: { onConnected: () => void }) {
  const [adAccountId, setAdAccountId] = useState('');
  const [accessToken, setAccessToken] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!adAccountId.trim() || !accessToken.trim()) return;
    setLoading(true);
    try {
      await connectAdAccount({ adAccountId: adAccountId.trim(), accessToken: accessToken.trim() });
      toast.success('Conta de anúncios conectada com sucesso!');
      onConnected();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Erro ao conectar conta';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-lg mx-auto mt-16">
      <div className="text-center mb-8">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-blue-50 mb-4">
          <svg viewBox="0 0 24 24" className="w-8 h-8 fill-[#1877F2]">
            <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
          </svg>
        </div>
        <h2 className="text-xl font-semibold text-slate-800 mb-1">Conectar Facebook Ads</h2>
        <p className="text-sm text-slate-500">
          Insira o ID da conta de anúncios e seu token de acesso da Graph API para gerenciar suas campanhas.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">
            Ad Account ID
          </label>
          <input
            type="text"
            value={adAccountId}
            onChange={e => setAdAccountId(e.target.value)}
            placeholder="Ex: act_123456789 ou 123456789"
            className="w-full px-3 py-2 rounded-md border border-slate-200 text-sm text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
          <p className="text-xs text-slate-400 mt-1">
            Encontre em Gerenciador de Anúncios → Configurações da Conta
          </p>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">
            Token de Acesso (Graph API)
          </label>
          <textarea
            value={accessToken}
            onChange={e => setAccessToken(e.target.value)}
            placeholder="EAABsbCS..."
            rows={3}
            className="w-full px-3 py-2 rounded-md border border-slate-200 text-sm text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none font-mono text-xs"
          />
          <p className="text-xs text-slate-400 mt-1">
            Gere em{' '}
            <span className="text-blue-600">developers.facebook.com → Graph API Explorer</span>
          </p>
        </div>

        <button
          type="submit"
          disabled={loading || !adAccountId.trim() || !accessToken.trim()}
          className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-md bg-[#1877F2] hover:bg-[#166FE5] text-white text-sm font-medium transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? (
            <><Loader2 className="w-4 h-4 animate-spin" /> Verificando...</>
          ) : (
            <><Link2 className="w-4 h-4" /> Conectar Conta</>
          )}
        </button>
      </form>
    </div>
  );
}

// ===== METRIC CARD =====

function MetricCard({
  label,
  value,
  icon: Icon,
  color,
}: {
  label: string;
  value: string;
  icon: React.ElementType;
  color: string;
}) {
  return (
    <div className="bg-white rounded-xl border border-slate-100 p-5">
      <div className="flex items-center gap-3 mb-3">
        <div className={`w-9 h-9 rounded-lg flex items-center justify-center ${color}`}>
          <Icon className="w-4.5 h-4.5" />
        </div>
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">{label}</span>
      </div>
      <p className="text-2xl font-bold text-slate-800">{value}</p>
    </div>
  );
}

// ===== CAMPAIGN ROW =====

function CampaignRow({ campaign }: { campaign: FacebookCampaign }) {
  const colorClass = STATUS_COLORS[campaign.status] ?? STATUS_COLORS.UNKNOWN;
  const budget = campaign.dailyBudget
    ? `R$ ${(parseInt(campaign.dailyBudget) / 100).toFixed(2)}/dia`
    : campaign.lifetimeBudget
    ? `R$ ${(parseInt(campaign.lifetimeBudget) / 100).toFixed(2)} total`
    : '—';

  return (
    <tr className="border-t border-slate-50 hover:bg-slate-50/50 transition-colors">
      <td className="px-4 py-3">
        <span className="text-sm font-medium text-slate-800">{campaign.name}</span>
      </td>
      <td className="px-4 py-3">
        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${colorClass}`}>
          {campaign.status}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-slate-600">{campaign.objective || '—'}</td>
      <td className="px-4 py-3 text-sm text-slate-600">{budget}</td>
      <td className="px-4 py-3 text-sm text-slate-400">
        {campaign.startTime ? new Date(campaign.startTime).toLocaleDateString('pt-BR') : '—'}
      </td>
    </tr>
  );
}

// ===== INSIGHT ROW =====

function InsightRow({ insight }: { insight: FacebookInsight }) {
  const spend = parseFloat(insight.spend || '0');
  const ctr = parseFloat(insight.ctr || '0');

  return (
    <tr className="border-t border-slate-50 hover:bg-slate-50/50 transition-colors">
      <td className="px-4 py-3 text-sm font-medium text-slate-800">{insight.campaignName || insight.campaignId}</td>
      <td className="px-4 py-3 text-sm text-slate-600">{formatNumber(parseInt(insight.impressions || '0'))}</td>
      <td className="px-4 py-3 text-sm text-slate-600">{formatNumber(parseInt(insight.clicks || '0'))}</td>
      <td className="px-4 py-3 text-sm text-slate-600">{ctr.toFixed(2)}%</td>
      <td className="px-4 py-3 text-sm text-slate-600">
        {insight.cpm ? `R$ ${parseFloat(insight.cpm).toFixed(2)}` : '—'}
      </td>
      <td className="px-4 py-3 text-sm font-medium text-slate-800">
        R$ {spend.toFixed(2)}
      </td>
    </tr>
  );
}

// ===== MAIN PAGE =====

export default function AdsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [summary, setSummary] = useState<AdAccountSummary | null>(null);
  const [campaigns, setCampaigns] = useState<FacebookCampaign[]>([]);
  const [insights, setInsights] = useState<FacebookInsight[]>([]);
  const [datePreset, setDatePreset] = useState('last_30d');
  const [loading, setLoading] = useState(true);
  const [loadingInsights, setLoadingInsights] = useState(false);
  const [notConnected, setNotConnected] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [activeTab, setActiveTab] = useState<'overview' | 'campaigns' | 'insights'>('overview');

  const loadData = useCallback(async () => {
    setLoading(true);
    setNotConnected(false);
    try {
      const [summaryData, campaignsData] = await Promise.all([
        getAdAccountSummary(),
        getCampaigns(),
      ]);
      setSummary(summaryData);
      setCampaigns(campaignsData);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : '';
      if (msg.includes('NotFound') || msg.includes('404')) {
        setNotConnected(true);
      } else {
        toast.error('Erro ao carregar dados dos anúncios');
      }
    } finally {
      setLoading(false);
    }
  }, []);

  const loadInsights = useCallback(async (preset: string) => {
    setLoadingInsights(true);
    try {
      const data = await getInsights({ datePreset: preset, level: 'campaign' });
      setInsights(data);
    } catch {
      toast.error('Erro ao carregar insights');
    } finally {
      setLoadingInsights(false);
    }
  }, []);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) { router.push('/login'); return; }
    loadData();
  }, [isAuthenticated, authLoading, router, loadData]);

  useEffect(() => {
    if (activeTab === 'insights' && !notConnected && !loading) {
      loadInsights(datePreset);
    }
  }, [activeTab, datePreset, notConnected, loading, loadInsights]);

  const handleSync = async () => {
    setSyncing(true);
    try {
      await syncAdAccount();
      toast.success('Conta sincronizada com sucesso!');
      await loadData();
    } catch {
      toast.error('Erro ao sincronizar conta');
    } finally {
      setSyncing(false);
    }
  };

  const handleDisconnect = async () => {
    if (!confirm('Tem certeza que deseja desconectar sua conta de anúncios?')) return;
    try {
      await disconnectAdAccount();
      toast.success('Conta desconectada.');
      setNotConnected(true);
      setSummary(null);
      setCampaigns([]);
      setInsights([]);
    } catch {
      toast.error('Erro ao desconectar conta');
    }
  };

  if (authLoading || loading) {
    return (
      <div className="flex items-center justify-center min-h-[300px]">
        <Loader2 className="w-6 h-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (notConnected) {
    return (
      <div className="max-w-3xl mx-auto px-4">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-800">Anúncios</h1>
          <p className="text-slate-500 text-sm mt-1">Gerencie suas campanhas do Facebook Ads</p>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-8">
          <ConnectForm onConnected={loadData} />
        </div>
      </div>
    );
  }

  const account = summary?.account;
  const currency = account?.currency || 'BRL';

  return (
    <div className="max-w-5xl mx-auto px-4">
      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <h1 className="text-2xl font-bold text-slate-800">Anúncios</h1>
            {account?.isActive && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-50 text-green-700 border border-green-200">
                <CheckCircle2 className="w-3 h-3" /> Conectado
              </span>
            )}
          </div>
          {account && (
            <p className="text-sm text-slate-500">
              {account.accountName} · {account.adAccountId} · {account.currency}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleSync}
            disabled={syncing}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50 transition disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${syncing ? 'animate-spin' : ''}`} />
            Sincronizar
          </button>
          <button
            onClick={handleDisconnect}
            className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-red-600 border border-red-100 rounded-lg hover:bg-red-50 transition"
          >
            <Unplug className="w-3.5 h-3.5" />
            Desconectar
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-slate-100 mb-6">
        {([
          { key: 'overview', label: 'Visão Geral', icon: BarChart3 },
          { key: 'campaigns', label: 'Campanhas', icon: Zap },
          { key: 'insights', label: 'Insights', icon: TrendingUp },
        ] as const).map(({ key, label, icon: Icon }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            className={`flex items-center gap-1.5 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
              activeTab === key
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            <Icon className="w-4 h-4" />
            {label}
          </button>
        ))}
      </div>

      {/* Overview Tab */}
      {activeTab === 'overview' && summary && (
        <div>
          {/* Metrics */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <MetricCard
              label="Gasto Total (30d)"
              value={formatCurrency(summary.totalSpend, currency)}
              icon={Wallet}
              color="bg-blue-50 text-blue-600"
            />
            <MetricCard
              label="Impressões (30d)"
              value={formatNumber(summary.totalImpressions)}
              icon={Eye}
              color="bg-purple-50 text-purple-600"
            />
            <MetricCard
              label="Cliques (30d)"
              value={formatNumber(summary.totalClicks)}
              icon={MousePointerClick}
              color="bg-teal-50 text-teal-600"
            />
            <MetricCard
              label="CTR Médio"
              value={`${summary.averageCtr.toFixed(2)}%`}
              icon={TrendingUp}
              color="bg-amber-50 text-amber-600"
            />
          </div>

          {/* Campaign status */}
          <div className="grid grid-cols-2 gap-4 mb-6">
            <div className="bg-white rounded-xl border border-slate-100 p-5 flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-slate-50 flex items-center justify-center">
                <Zap className="w-5 h-5 text-slate-400" />
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-800">{summary.totalCampaigns}</p>
                <p className="text-xs text-slate-500">Campanhas totais</p>
              </div>
            </div>
            <div className="bg-white rounded-xl border border-slate-100 p-5 flex items-center gap-4">
              <div className="w-12 h-12 rounded-xl bg-green-50 flex items-center justify-center">
                <CheckCircle2 className="w-5 h-5 text-green-500" />
              </div>
              <div>
                <p className="text-2xl font-bold text-slate-800">{summary.activeCampaigns}</p>
                <p className="text-xs text-slate-500">Campanhas ativas</p>
              </div>
            </div>
          </div>

          {/* Quick navigation */}
          <div className="bg-white rounded-xl border border-slate-100 divide-y divide-slate-50">
            <button
              onClick={() => setActiveTab('campaigns')}
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-slate-50/50 transition-colors"
            >
              <div className="flex items-center gap-3">
                <Zap className="w-4 h-4 text-slate-400" />
                <span className="text-sm font-medium text-slate-700">Ver todas as campanhas</span>
              </div>
              <ChevronRight className="w-4 h-4 text-slate-300" />
            </button>
            <button
              onClick={() => setActiveTab('insights')}
              className="w-full flex items-center justify-between px-5 py-4 hover:bg-slate-50/50 transition-colors"
            >
              <div className="flex items-center gap-3">
                <TrendingUp className="w-4 h-4 text-slate-400" />
                <span className="text-sm font-medium text-slate-700">Ver relatório de insights</span>
              </div>
              <ChevronRight className="w-4 h-4 text-slate-300" />
            </button>
          </div>

          {account?.lastSyncAt && (
            <p className="text-xs text-slate-400 mt-4 text-center">
              Última sincronização: {new Date(account.lastSyncAt).toLocaleString('pt-BR')}
            </p>
          )}
        </div>
      )}

      {/* Campaigns Tab */}
      {activeTab === 'campaigns' && (
        <div className="bg-white rounded-xl border border-slate-100 overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-50 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-700">
              Campanhas ({campaigns.length})
            </h2>
          </div>
          {campaigns.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400">
              <AlertCircle className="w-8 h-8 mb-2" />
              <p className="text-sm">Nenhuma campanha encontrada</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr className="bg-slate-50/50">
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Nome</th>
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Status</th>
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Objetivo</th>
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Orçamento</th>
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Início</th>
                  </tr>
                </thead>
                <tbody>
                  {campaigns.map(c => (
                    <CampaignRow key={c.id} campaign={c} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Insights Tab */}
      {activeTab === 'insights' && (
        <div>
          {/* Date selector */}
          <div className="flex items-center gap-2 mb-4 flex-wrap">
            {DATE_PRESETS.map(p => (
              <button
                key={p.value}
                onClick={() => setDatePreset(p.value)}
                className={`px-3 py-1.5 text-xs font-medium rounded-lg border transition-colors ${
                  datePreset === p.value
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'text-slate-600 border-slate-200 hover:bg-slate-50'
                }`}
              >
                {p.label}
              </button>
            ))}
          </div>

          <div className="bg-white rounded-xl border border-slate-100 overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-50">
              <h2 className="text-sm font-semibold text-slate-700">
                Insights por Campanha
              </h2>
            </div>
            {loadingInsights ? (
              <div className="flex items-center justify-center py-16">
                <Loader2 className="w-5 h-5 animate-spin text-slate-400" />
              </div>
            ) : insights.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 text-slate-400">
                <BarChart3 className="w-8 h-8 mb-2" />
                <p className="text-sm">Nenhum dado encontrado para o período selecionado</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left">
                  <thead>
                    <tr className="bg-slate-50/50">
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Campanha</th>
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Impressões</th>
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Cliques</th>
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">CTR</th>
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">CPM</th>
                      <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Gasto</th>
                    </tr>
                  </thead>
                  <tbody>
                    {insights.map((insight, i) => (
                      <InsightRow key={`${insight.campaignId}-${i}`} insight={insight} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
