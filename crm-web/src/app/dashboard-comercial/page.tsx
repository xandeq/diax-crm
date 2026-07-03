'use client';

import { useAuth } from '@/contexts/AuthContext';
import { listUpcomingMeetings, type Meeting } from '@/services/meetings';
import { getSalesDashboard, type SalesDashboard } from '@/services/salesDashboard';
import {
  AlertCircle,
  Banknote,
  CalendarClock,
  Flame,
  Loader2,
  Phone,
  RefreshCw,
  Target,
  TrendingUp,
  Trophy,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });
const MONTH_LABEL = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

const PROPOSAL_STATUS: Record<string, { label: string; cls: string }> = {
  Draft: { label: 'Rascunho', cls: 'text-white/50' },
  Sent: { label: 'Enviadas', cls: 'text-sky-300' },
  Accepted: { label: 'Aceitas', cls: 'text-violet-300' },
  Paid: { label: 'Pagas', cls: 'text-emerald-300' },
  Cancelled: { label: 'Canceladas', cls: 'text-white/35' },
};

const FUNNEL_COLORS = ['bg-slate-500', 'bg-sky-500', 'bg-violet-500', 'bg-amber-500', 'bg-emerald-500'];

function monthLabel(ym: string): string {
  const [, m] = ym.split('-');
  return MONTH_LABEL[Number(m) - 1] ?? ym;
}

export default function SalesDashboardPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [data, setData] = useState<SalesDashboard | null>(null);
  const [meetings, setMeetings] = useState<Meeting[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    Promise.all([
      getSalesDashboard().then(setData),
      listUpcomingMeetings().then(setMeetings).catch(() => setMeetings([])),
    ])
      .catch(err => setError(err instanceof Error ? err.message : 'Erro ao carregar o dashboard.'))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    load();
  }, [authLoading, isAuthenticated, router, load]);

  if (authLoading || (isLoading && !data)) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    );
  }

  const maxFunnel = Math.max(1, ...(data?.funnel.map(f => f.count) ?? [1]));
  const maxRevenue = Math.max(1, ...(data?.monthlyRevenue.map(m => m.total) ?? [1]));

  return (
    <div className="min-h-screen">
      {/* Header */}
      <div className="border-b border-white/5 bg-white/[0.02]">
        <div className="max-w-screen-xl mx-auto px-6 py-5 flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-xl font-bold text-white flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-violet-400" /> Dashboard Comercial
            </h1>
            <p className="text-[12px] text-white/40 mt-0.5">Funil, receita e compromissos — visão do negócio em uma tela</p>
          </div>
          <button
            type="button"
            onClick={load}
            className="h-9 w-9 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center text-white/50 hover:text-white transition-all"
            title="Atualizar"
          >
            <RefreshCw className="h-4 w-4" />
          </button>
        </div>
      </div>

      {error && (
        <div className="max-w-screen-xl mx-auto px-6 pt-4">
          <div className="flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-sm text-red-300">
            <AlertCircle className="h-4 w-4" /> {error}
          </div>
        </div>
      )}

      {data && (
        <div className="max-w-screen-xl mx-auto px-6 py-6 space-y-6">
          {/* KPI cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            <Kpi icon={<Target className="h-4 w-4 text-violet-300" />} label="Previsão ponderada"
              value={BRL.format(data.weightedForecast)} />
            <Kpi icon={<Trophy className="h-4 w-4 text-emerald-300" />} label="Fechado (30 dias)"
              value={BRL.format(data.wonLast30DaysValue)} sub={`${data.wonLast30DaysCount} negócio${data.wonLast30DaysCount === 1 ? '' : 's'}`} />
            <Kpi icon={<Banknote className="h-4 w-4 text-sky-300" />} label="Taxa de aceite (propostas)"
              value={`${Math.round(data.proposalAcceptanceRate * 100)}%`} />
            <Kpi icon={<Flame className="h-4 w-4 text-red-300" />} label="Leads quentes"
              value={String(data.hotLeads)} sub={`${data.warmLeads} mornos · ${data.coldLeads} frios`} />
          </div>

          <div className="grid lg:grid-cols-2 gap-6">
            {/* Funil */}
            <section className="rounded-2xl bg-white/[0.03] border border-white/10 p-6">
              <h2 className="text-sm font-semibold text-white/80 mb-4">Funil de vendas</h2>
              <div className="space-y-3">
                {data.funnel.map((stage, i) => (
                  <div key={stage.stage}>
                    <div className="flex items-center justify-between text-[12px] mb-1">
                      <span className="text-white/60">{stage.label}</span>
                      <span className="text-white/85 font-semibold">{stage.count.toLocaleString('pt-BR')}</span>
                    </div>
                    <div className="h-5 rounded-lg bg-white/[0.04] overflow-hidden">
                      <div
                        className={`h-full ${FUNNEL_COLORS[i]} opacity-70 rounded-lg transition-all`}
                        style={{ width: `${Math.max(2, (stage.count / maxFunnel) * 100)}%` }}
                      />
                    </div>
                    {stage.conversionToNext != null && (
                      <p className="text-[10px] text-white/30 mt-0.5">
                        ↓ {(stage.conversionToNext * 100).toFixed(1)}% avançaram
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </section>

            {/* Receita mensal */}
            <section className="rounded-2xl bg-white/[0.03] border border-white/10 p-6">
              <h2 className="text-sm font-semibold text-white/80 mb-4">Receita de propostas pagas (6 meses)</h2>
              <div className="flex items-end gap-2 h-48">
                {data.monthlyRevenue.map(m => (
                  <div key={m.month} className="flex-1 flex flex-col items-center gap-1">
                    <span className="text-[10px] text-emerald-300/80 font-medium">
                      {m.total > 0 ? BRL.format(m.total) : ''}
                    </span>
                    <div
                      className="w-full rounded-t-lg bg-gradient-to-t from-emerald-600/60 to-emerald-400/60 transition-all"
                      style={{ height: `${Math.max(m.total > 0 ? 8 : 2, (m.total / maxRevenue) * 100)}%` }}
                      title={`${m.count} proposta${m.count === 1 ? '' : 's'} paga${m.count === 1 ? '' : 's'}`}
                    />
                    <span className="text-[10px] text-white/40">{monthLabel(m.month)}</span>
                  </div>
                ))}
              </div>
            </section>
          </div>

          <div className="grid lg:grid-cols-2 gap-6">
            {/* Propostas */}
            <section className="rounded-2xl bg-white/[0.03] border border-white/10 p-6">
              <h2 className="text-sm font-semibold text-white/80 mb-4">Propostas</h2>
              {data.proposals.length === 0 ? (
                <p className="text-[12px] text-white/35">
                  Nenhuma proposta ainda — gere a primeira pelo botão <em>Proposta</em> nos cards do pipeline.
                </p>
              ) : (
                <div className="space-y-2">
                  {data.proposals.map(p => {
                    const meta = PROPOSAL_STATUS[p.status] ?? { label: p.status, cls: 'text-white/60' };
                    return (
                      <div key={p.status} className="flex items-center justify-between text-sm rounded-xl bg-white/[0.03] px-4 py-2.5">
                        <span className={meta.cls}>{meta.label}</span>
                        <span className="text-white/80">
                          {p.count} · <span className="font-semibold">{BRL.format(p.total)}</span>
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}
            </section>

            {/* Compromissos */}
            <section className="rounded-2xl bg-white/[0.03] border border-white/10 p-6">
              <h2 className="text-sm font-semibold text-white/80 mb-4">Agenda comercial</h2>
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-xl bg-white/[0.03] p-4">
                  <CalendarClock className="h-4 w-4 text-violet-300 mb-2" />
                  <p className="text-2xl font-bold text-white">{data.upcomingMeetings}</p>
                  <p className="text-[11px] text-white/40">reuniões agendadas</p>
                </div>
                <div className="rounded-xl bg-white/[0.03] p-4">
                  <Phone className="h-4 w-4 text-amber-300 mb-2" />
                  <p className="text-2xl font-bold text-white">{data.openFollowUps}</p>
                  <p className="text-[11px] text-white/40">follow-ups abertos</p>
                </div>
              </div>
              {meetings.length > 0 && (
                <div className="mt-4 space-y-1.5">
                  {meetings.slice(0, 5).map(m => {
                    const brt = new Date(new Date(m.scheduledAt).getTime() - 3 * 3600 * 1000);
                    return (
                      <div key={m.id} className="flex items-center justify-between text-[12px] rounded-lg bg-white/[0.03] px-3 py-2">
                        <span className="text-white/70 truncate">{m.contactName}</span>
                        <span className="text-violet-300/80 shrink-0 ml-2">
                          {String(brt.getUTCDate()).padStart(2, '0')}/{String(brt.getUTCMonth() + 1).padStart(2, '0')} {String(brt.getUTCHours()).padStart(2, '0')}:{String(brt.getUTCMinutes()).padStart(2, '0')}
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}
              <p className="text-[11px] text-white/30 mt-4">
                O briefing diário no Telegram (~7h30) traz esta visão com nomes e contatos.
              </p>
            </section>
          </div>
        </div>
      )}
    </div>
  );
}

function Kpi({ icon, label, value, sub }: { icon: React.ReactNode; label: string; value: string; sub?: string }) {
  return (
    <div className="rounded-2xl bg-white/[0.03] border border-white/10 p-4">
      <div className="flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-white/40 mb-2">
        {icon} {label}
      </div>
      <p className="text-xl font-bold text-white">{value}</p>
      {sub && <p className="text-[11px] text-white/35 mt-0.5">{sub}</p>}
    </div>
  );
}
