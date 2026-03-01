'use client';

import { useAuth } from '@/contexts/AuthContext';
import { ApiError } from '@/services/api';
import {
  connectAdAccount,
  createCampaign,
  disconnectAdAccount,
  getAdAccountSummary,
  getAdSets,
  getCampaigns,
  getInsights,
  syncAdAccount,
  updateAdSetBudget,
  updateAdSetStatus,
  updateCampaignBudget,
  updateCampaignStatus,
} from '@/services/ads';
import type {
  AdAccountSummary,
  CreateCampaignRequest,
  FacebookAdSet,
  FacebookCampaign,
  FacebookInsight,
  UpdateAdSetBudgetRequest,
  UpdateCampaignBudgetRequest,
} from '@/types/ads';
import { STATUS_COLORS } from '@/types/ads';
import {
  AlertCircle,
  BarChart3,
  CheckCircle2,
  ChevronRight,
  Eye,
  Layers,
  Link2,
  Loader2,
  MousePointerClick,
  Pause,
  Pencil,
  Play,
  Plus,
  RefreshCw,
  TrendingUp,
  Unplug,
  Wallet,
  X,
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

const CAMPAIGN_OBJECTIVES = [
  { value: 'OUTCOME_AWARENESS', label: 'Reconhecimento (Awareness)' },
  { value: 'OUTCOME_TRAFFIC', label: 'Tráfego (Traffic)' },
  { value: 'OUTCOME_ENGAGEMENT', label: 'Engajamento (Engagement)' },
  { value: 'OUTCOME_LEADS', label: 'Geração de Leads' },
  { value: 'OUTCOME_APP_PROMOTION', label: 'Promoção de App' },
  { value: 'OUTCOME_SALES', label: 'Vendas (Sales)' },
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

// ===== NEW CAMPAIGN MODAL =====

function NewCampaignModal({
  open,
  onClose,
  onSuccess,
}: {
  open: boolean;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [name, setName] = useState('');
  const [objective, setObjective] = useState('OUTCOME_TRAFFIC');
  const [status, setStatus] = useState<'ACTIVE' | 'PAUSED'>('PAUSED');
  const [budgetType, setBudgetType] = useState<'daily' | 'lifetime'>('daily');
  const [budgetValue, setBudgetValue] = useState('');
  const [startTime, setStartTime] = useState('');
  const [stopTime, setStopTime] = useState('');
  const [loading, setLoading] = useState(false);

  const reset = () => {
    setName('');
    setObjective('OUTCOME_TRAFFIC');
    setStatus('PAUSED');
    setBudgetType('daily');
    setBudgetValue('');
    setStartTime('');
    setStopTime('');
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !budgetValue) return;
    const budgetCentavos = Math.round(parseFloat(budgetValue) * 100).toString();
    const payload: CreateCampaignRequest = {
      name: name.trim(),
      objective,
      status,
      ...(budgetType === 'daily' ? { dailyBudget: budgetCentavos } : { lifetimeBudget: budgetCentavos }),
      ...(startTime ? { startTime } : {}),
      ...(stopTime ? { stopTime } : {}),
    };
    setLoading(true);
    try {
      await createCampaign(payload);
      toast.success('Campanha criada com sucesso!');
      handleClose();
      onSuccess();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao criar campanha');
    } finally {
      setLoading(false);
    }
  };

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
          <h2 className="text-sm font-semibold text-slate-800">Nova Campanha</h2>
          <button onClick={handleClose} className="text-slate-400 hover:text-slate-600">
            <X className="w-4 h-4" />
          </button>
        </div>
        <form onSubmit={handleSubmit} className="p-5 space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Nome *</label>
            <input
              value={name}
              onChange={e => setName(e.target.value)}
              placeholder="Ex: Campanha Verão 2025"
              required
              className="w-full px-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Objetivo</label>
            <select
              value={objective}
              onChange={e => setObjective(e.target.value)}
              className="w-full px-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {CAMPAIGN_OBJECTIVES.map(o => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Status inicial</label>
            <div className="flex gap-2">
              {(['PAUSED', 'ACTIVE'] as const).map(s => (
                <button
                  key={s}
                  type="button"
                  onClick={() => setStatus(s)}
                  className={`flex-1 py-1.5 text-xs font-medium rounded-md border transition-colors ${
                    status === s
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'text-slate-600 border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  {s === 'PAUSED' ? 'Pausada' : 'Ativa'}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Tipo de orçamento</label>
            <div className="flex gap-2 mb-2">
              {([['daily', 'Diário'], ['lifetime', 'Total']] as const).map(([val, label]) => (
                <button
                  key={val}
                  type="button"
                  onClick={() => setBudgetType(val)}
                  className={`flex-1 py-1.5 text-xs font-medium rounded-md border transition-colors ${
                    budgetType === val
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'text-slate-600 border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-slate-400">R$</span>
              <input
                type="number"
                min="1"
                step="0.01"
                value={budgetValue}
                onChange={e => setBudgetValue(e.target.value)}
                placeholder="0,00"
                required
                className="w-full pl-9 pr-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Início (opcional)</label>
              <input
                type="datetime-local"
                value={startTime}
                onChange={e => setStartTime(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">
                Fim {budgetType === 'lifetime' ? '*' : '(opcional)'}
              </label>
              <input
                type="datetime-local"
                value={stopTime}
                onChange={e => setStopTime(e.target.value)}
                required={budgetType === 'lifetime'}
                className="w-full px-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={handleClose}
              className="flex-1 py-2 text-sm text-slate-600 border border-slate-200 rounded-md hover:bg-slate-50 transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || !name.trim() || !budgetValue}
              className="flex-1 py-2 text-sm font-medium bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-1.5"
            >
              {loading ? (
                <><Loader2 className="w-3.5 h-3.5 animate-spin" /> Criando...</>
              ) : (
                'Criar Campanha'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ===== EDIT CAMPAIGN BUDGET MODAL =====

function EditBudgetModal({
  campaign,
  onClose,
  onSuccess,
}: {
  campaign: FacebookCampaign | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [budgetType, setBudgetType] = useState<'daily' | 'lifetime'>('daily');
  const [budgetValue, setBudgetValue] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (campaign) {
      setBudgetType(campaign.lifetimeBudget ? 'lifetime' : 'daily');
      const raw = parseInt(campaign.dailyBudget || campaign.lifetimeBudget || '0');
      setBudgetValue(raw > 0 ? (raw / 100).toFixed(2) : '');
    }
  }, [campaign]);

  if (!campaign) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!budgetValue) return;
    const budgetCentavos = Math.round(parseFloat(budgetValue) * 100).toString();
    const payload: UpdateCampaignBudgetRequest = {
      ...(budgetType === 'daily' ? { dailyBudget: budgetCentavos } : { lifetimeBudget: budgetCentavos }),
    };
    setLoading(true);
    try {
      await updateCampaignBudget(campaign.id, payload);
      toast.success('Orçamento atualizado!');
      onClose();
      onSuccess();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao atualizar orçamento');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-sm">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
          <h2 className="text-sm font-semibold text-slate-800">Editar Orçamento</h2>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X className="w-4 h-4" />
          </button>
        </div>
        <form onSubmit={handleSubmit} className="p-5 space-y-4">
          <p className="text-xs text-slate-500">
            Campanha: <span className="font-medium text-slate-700">{campaign.name}</span>
          </p>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Tipo de orçamento</label>
            <div className="flex gap-2">
              {([['daily', 'Diário'], ['lifetime', 'Total']] as const).map(([val, label]) => (
                <button
                  key={val}
                  type="button"
                  onClick={() => setBudgetType(val)}
                  className={`flex-1 py-1.5 text-xs font-medium rounded-md border transition-colors ${
                    budgetType === val
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'text-slate-600 border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Valor</label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-slate-400">R$</span>
              <input
                type="number"
                min="1"
                step="0.01"
                value={budgetValue}
                onChange={e => setBudgetValue(e.target.value)}
                required
                className="w-full pl-9 pr-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2 text-sm text-slate-600 border border-slate-200 rounded-md hover:bg-slate-50 transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || !budgetValue}
              className="flex-1 py-2 text-sm font-medium bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-1.5"
            >
              {loading ? (
                <><Loader2 className="w-3.5 h-3.5 animate-spin" /> Salvando...</>
              ) : (
                'Salvar'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ===== EDIT AD SET BUDGET MODAL =====

function EditAdSetBudgetModal({
  adSet,
  onClose,
  onSuccess,
}: {
  adSet: FacebookAdSet | null;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [budgetType, setBudgetType] = useState<'daily' | 'lifetime'>('daily');
  const [budgetValue, setBudgetValue] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (adSet) {
      setBudgetType(adSet.lifetimeBudget ? 'lifetime' : 'daily');
      const raw = parseInt(adSet.dailyBudget || adSet.lifetimeBudget || '0');
      setBudgetValue(raw > 0 ? (raw / 100).toFixed(2) : '');
    }
  }, [adSet]);

  if (!adSet) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!budgetValue) return;
    const budgetCentavos = Math.round(parseFloat(budgetValue) * 100).toString();
    const payload: UpdateAdSetBudgetRequest = {
      ...(budgetType === 'daily' ? { dailyBudget: budgetCentavos } : { lifetimeBudget: budgetCentavos }),
    };
    setLoading(true);
    try {
      await updateAdSetBudget(adSet.id, payload);
      toast.success('Orçamento do conjunto atualizado!');
      onClose();
      onSuccess();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao atualizar orçamento');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-sm">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
          <h2 className="text-sm font-semibold text-slate-800">Editar Orçamento do Conjunto</h2>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600">
            <X className="w-4 h-4" />
          </button>
        </div>
        <form onSubmit={handleSubmit} className="p-5 space-y-4">
          <p className="text-xs text-slate-500">
            Conjunto: <span className="font-medium text-slate-700">{adSet.name}</span>
          </p>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Tipo de orçamento</label>
            <div className="flex gap-2">
              {([['daily', 'Diário'], ['lifetime', 'Total']] as const).map(([val, label]) => (
                <button
                  key={val}
                  type="button"
                  onClick={() => setBudgetType(val)}
                  className={`flex-1 py-1.5 text-xs font-medium rounded-md border transition-colors ${
                    budgetType === val
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'text-slate-600 border-slate-200 hover:bg-slate-50'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Valor</label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs text-slate-400">R$</span>
              <input
                type="number"
                min="1"
                step="0.01"
                value={budgetValue}
                onChange={e => setBudgetValue(e.target.value)}
                required
                className="w-full pl-9 pr-3 py-2 text-sm rounded-md border border-slate-200 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2 text-sm text-slate-600 border border-slate-200 rounded-md hover:bg-slate-50 transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading || !budgetValue}
              className="flex-1 py-2 text-sm font-medium bg-blue-600 text-white rounded-md hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-1.5"
            >
              {loading ? (
                <><Loader2 className="w-3.5 h-3.5 animate-spin" /> Salvando...</>
              ) : (
                'Salvar'
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ===== CAMPAIGN ROW =====

function CampaignRow({
  campaign,
  onStatusToggle,
  onEditBudget,
  isToggling,
}: {
  campaign: FacebookCampaign;
  onStatusToggle: () => void;
  onEditBudget: () => void;
  isToggling: boolean;
}) {
  const colorClass = STATUS_COLORS[campaign.status] ?? STATUS_COLORS.UNKNOWN;
  const budget = campaign.dailyBudget
    ? `R$ ${(parseInt(campaign.dailyBudget) / 100).toFixed(2)}/dia`
    : campaign.lifetimeBudget
    ? `R$ ${(parseInt(campaign.lifetimeBudget) / 100).toFixed(2)} total`
    : '—';

  const canToggle = campaign.status === 'ACTIVE' || campaign.status === 'PAUSED';
  const isPausing = campaign.status === 'ACTIVE';

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
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5">
          <button
            onClick={onStatusToggle}
            disabled={!canToggle || isToggling}
            title={isPausing ? 'Pausar campanha' : 'Ativar campanha'}
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition disabled:opacity-30 disabled:cursor-not-allowed"
          >
            {isToggling
              ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
              : isPausing
              ? <Pause className="w-3.5 h-3.5" />
              : <Play className="w-3.5 h-3.5" />
            }
          </button>
          <button
            onClick={onEditBudget}
            title="Editar orçamento"
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition"
          >
            <Pencil className="w-3.5 h-3.5" />
          </button>
        </div>
      </td>
    </tr>
  );
}

// ===== AD SET ROW =====

function AdSetRow({
  adSet,
  campaignName,
  onStatusToggle,
  onEditBudget,
  isToggling,
}: {
  adSet: FacebookAdSet;
  campaignName: string;
  onStatusToggle: () => void;
  onEditBudget: () => void;
  isToggling: boolean;
}) {
  const colorClass = STATUS_COLORS[adSet.status] ?? STATUS_COLORS.UNKNOWN;
  const budget = adSet.dailyBudget
    ? `R$ ${(parseInt(adSet.dailyBudget) / 100).toFixed(2)}/dia`
    : adSet.lifetimeBudget
    ? `R$ ${(parseInt(adSet.lifetimeBudget) / 100).toFixed(2)} total`
    : '—';

  const canToggle = adSet.status === 'ACTIVE' || adSet.status === 'PAUSED';
  const isPausing = adSet.status === 'ACTIVE';

  return (
    <tr className="border-t border-slate-50 hover:bg-slate-50/50 transition-colors">
      <td className="px-4 py-3">
        <span className="text-sm font-medium text-slate-800">{adSet.name}</span>
        {campaignName && (
          <span className="block text-xs text-slate-400 mt-0.5">{campaignName}</span>
        )}
      </td>
      <td className="px-4 py-3">
        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${colorClass}`}>
          {adSet.status}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-slate-600">{adSet.optimizationGoal || '—'}</td>
      <td className="px-4 py-3 text-sm text-slate-600">{budget}</td>
      <td className="px-4 py-3 text-sm text-slate-400">
        {adSet.startTime ? new Date(adSet.startTime).toLocaleDateString('pt-BR') : '—'}
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1.5">
          <button
            onClick={onStatusToggle}
            disabled={!canToggle || isToggling}
            title={isPausing ? 'Pausar conjunto' : 'Ativar conjunto'}
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition disabled:opacity-30 disabled:cursor-not-allowed"
          >
            {isToggling
              ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
              : isPausing
              ? <Pause className="w-3.5 h-3.5" />
              : <Play className="w-3.5 h-3.5" />
            }
          </button>
          <button
            onClick={onEditBudget}
            title="Editar orçamento"
            className="p-1.5 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-100 transition"
          >
            <Pencil className="w-3.5 h-3.5" />
          </button>
        </div>
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
  const [activeTab, setActiveTab] = useState<'overview' | 'campaigns' | 'adsets' | 'insights'>('overview');

  // Campaign write state
  const [showNewCampaign, setShowNewCampaign] = useState(false);
  const [editBudgetCampaign, setEditBudgetCampaign] = useState<FacebookCampaign | null>(null);
  const [togglingIds, setTogglingIds] = useState<Set<string>>(new Set());

  // Ad Sets state
  const [adSets, setAdSets] = useState<FacebookAdSet[]>([]);
  const [loadingAdSets, setLoadingAdSets] = useState(false);
  const [adSetCampaignFilter, setAdSetCampaignFilter] = useState('');
  const [editBudgetAdSet, setEditBudgetAdSet] = useState<FacebookAdSet | null>(null);
  const [togglingAdSetIds, setTogglingAdSetIds] = useState<Set<string>>(new Set());

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
      // Treat any 404 or "NotFound" code as "no account connected yet"
      const is404 = err instanceof ApiError && err.status === 404;
      const isNotFound = err instanceof ApiError && (err.code?.includes('NotFound') ?? false);
      if (is404 || isNotFound) {
        setNotConnected(true);
      } else {
        toast.error('Erro ao carregar dados dos anúncios');
      }
    } finally {
      setLoading(false);
    }
  }, []);

  const refreshCampaigns = useCallback(async () => {
    try {
      const data = await getCampaigns();
      setCampaigns(data);
    } catch {
      toast.error('Erro ao atualizar campanhas');
    }
  }, []);

  const refreshAdSets = useCallback(async (campaignId?: string) => {
    setLoadingAdSets(true);
    try {
      const data = await getAdSets(campaignId || undefined);
      setAdSets(data);
    } catch {
      toast.error('Erro ao carregar conjuntos de anúncios');
    } finally {
      setLoadingAdSets(false);
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

  useEffect(() => {
    if (activeTab === 'adsets' && !notConnected && !loading) {
      refreshAdSets(adSetCampaignFilter || undefined);
    }
  }, [activeTab, adSetCampaignFilter, notConnected, loading, refreshAdSets]);

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
      setAdSets([]);
    } catch {
      toast.error('Erro ao desconectar conta');
    }
  };

  const handleStatusToggle = async (campaign: FacebookCampaign) => {
    const newStatus = campaign.status === 'ACTIVE' ? 'PAUSED' : 'ACTIVE';
    setTogglingIds(prev => new Set(prev).add(campaign.id));
    try {
      await updateCampaignStatus(campaign.id, { status: newStatus });
      toast.success(newStatus === 'PAUSED' ? 'Campanha pausada.' : 'Campanha ativada.');
      await refreshCampaigns();
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao atualizar status');
    } finally {
      setTogglingIds(prev => {
        const next = new Set(prev);
        next.delete(campaign.id);
        return next;
      });
    }
  };

  const handleAdSetStatusToggle = async (adSet: FacebookAdSet) => {
    const newStatus = adSet.status === 'ACTIVE' ? 'PAUSED' : 'ACTIVE';
    setTogglingAdSetIds(prev => new Set(prev).add(adSet.id));
    try {
      await updateAdSetStatus(adSet.id, { status: newStatus });
      toast.success(newStatus === 'PAUSED' ? 'Conjunto pausado.' : 'Conjunto ativado.');
      await refreshAdSets(adSetCampaignFilter || undefined);
    } catch (err: unknown) {
      toast.error(err instanceof Error ? err.message : 'Erro ao atualizar status');
    } finally {
      setTogglingAdSetIds(prev => {
        const next = new Set(prev);
        next.delete(adSet.id);
        return next;
      });
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

  // Build a lookup map for campaign names (used in AdSetRow)
  const campaignNameById = campaigns.reduce<Record<string, string>>((acc, c) => {
    acc[c.id] = c.name;
    return acc;
  }, {});

  return (
    <div className="max-w-5xl mx-auto px-4">
      {/* Modals */}
      <NewCampaignModal
        open={showNewCampaign}
        onClose={() => setShowNewCampaign(false)}
        onSuccess={refreshCampaigns}
      />
      <EditBudgetModal
        campaign={editBudgetCampaign}
        onClose={() => setEditBudgetCampaign(null)}
        onSuccess={refreshCampaigns}
      />
      <EditAdSetBudgetModal
        adSet={editBudgetAdSet}
        onClose={() => setEditBudgetAdSet(null)}
        onSuccess={() => refreshAdSets(adSetCampaignFilter || undefined)}
      />

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
          { key: 'adsets', label: 'Conjuntos', icon: Layers },
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
            <button
              onClick={() => setShowNewCampaign(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
            >
              <Plus className="w-3.5 h-3.5" />
              Nova Campanha
            </button>
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
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {campaigns.map(c => (
                    <CampaignRow
                      key={c.id}
                      campaign={c}
                      onStatusToggle={() => handleStatusToggle(c)}
                      onEditBudget={() => setEditBudgetCampaign(c)}
                      isToggling={togglingIds.has(c.id)}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Ad Sets Tab */}
      {activeTab === 'adsets' && (
        <div className="bg-white rounded-xl border border-slate-100 overflow-hidden">
          <div className="px-5 py-4 border-b border-slate-50 flex items-center justify-between gap-4">
            <h2 className="text-sm font-semibold text-slate-700 shrink-0">
              Conjuntos ({adSets.length})
            </h2>
            <select
              value={adSetCampaignFilter}
              onChange={e => setAdSetCampaignFilter(e.target.value)}
              className="ml-auto px-3 py-1.5 text-xs rounded-md border border-slate-200 text-slate-600 focus:outline-none focus:ring-2 focus:ring-blue-500 max-w-xs"
            >
              <option value="">Todas as campanhas</option>
              {campaigns.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          {loadingAdSets ? (
            <div className="flex items-center justify-center py-16">
              <Loader2 className="w-5 h-5 animate-spin text-slate-400" />
            </div>
          ) : adSets.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400">
              <AlertCircle className="w-8 h-8 mb-2" />
              <p className="text-sm">Nenhum conjunto encontrado</p>
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
                    <th className="px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {adSets.map(a => (
                    <AdSetRow
                      key={a.id}
                      adSet={a}
                      campaignName={campaignNameById[a.campaignId] ?? ''}
                      onStatusToggle={() => handleAdSetStatusToggle(a)}
                      onEditBudget={() => setEditBudgetAdSet(a)}
                      isToggling={togglingAdSetIds.has(a.id)}
                    />
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
