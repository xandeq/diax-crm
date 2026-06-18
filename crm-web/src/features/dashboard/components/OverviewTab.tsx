'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useDashboardOverview } from '../hooks/useDashboardOverview';
import { LoadingSkeleton, LoadingGrid } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { MetricCard } from '@/components/dashboard/MetricCard';
import { DashboardHero } from '@/components/dashboard/DashboardHero';
import { ChartCard } from '@/components/dashboard/ChartCard';
import { StatusBadge } from '@/components/dashboard/StatusBadge';
import { EmptyState } from '@/components/dashboard/EmptyState';
import { SpringMetric } from '@/components/dashboard/SpringMetric';
import { InsightCard } from './InsightCard';
import { HealthBadge } from './HealthBadge';
import {
  Activity, AlertTriangle, ArrowRight, CheckCircle, CreditCard, DollarSign,
  Gauge, Mail, Megaphone, PiggyBank, Repeat, ShieldAlert, Sparkles,
  Target, TrendingDown, TrendingUp, Users, Wallet, Zap, Calendar, ListChecks, Clock
} from 'lucide-react';
import Link from 'next/link';
import dynamic from 'next/dynamic';
import { cn } from '@/lib/utils';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

const C = {
  primary: '#00D4AA',
  success: '#22C55E',
  loss: '#EF4444',
  warn: '#F59E0B',
  info: '#818CF8',
  accent: '#F472B6',
  funnel: ['#6366F1', '#8B5CF6', '#A78BFA', '#EC4899', '#00D4AA'],
  expense: ['#F59E0B', '#EF4444', '#8B5CF6', '#6366F1', '#EC4899'],
  text: '#f4f4f5',
  text2: '#b0b0ba',
  text3: '#8e8e99',
  muted: '#6e6e7a',
  grid: 'rgba(255,255,255,0.045)',
} as const;

const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });
const S = (v: number) => Math.abs(v) >= 1_000_000 ? `R$ ${(v / 1_000_000).toFixed(1)}M` : Math.abs(v) >= 1_000 ? `R$ ${(v / 1_000).toFixed(1)}k` : R(v);

export function OverviewTab() {
  const { data, isLoading, isError, error, refetch } = useDashboardOverview();
  const [mounted, setMounted] = useState(false);
  const [activeSubTab, setActiveSubTab] = useState<'agenda' | 'tasks' | 'checklist'>('agenda');

  useEffect(() => {
    setMounted(true);
    const handleSync = () => refetch();
    window.addEventListener('dashboard-sync', handleSync);
    return () => window.removeEventListener('dashboard-sync', handleSync);
  }, [refetch]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <LoadingSkeleton rows={1} />
        <LoadingGrid cards={5} />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <LoadingSkeleton rows={2} />
          <LoadingSkeleton rows={2} />
          <LoadingSkeleton rows={2} />
        </div>
      </div>
    );
  }

  if (isError) {
    return <ErrorState message={error instanceof Error ? error.message : undefined} onRetry={refetch} />;
  }

  if (!data) return null;

  const { funnel, curr, prev, trend, expenses, email, agenda, tasks = [], checklists = [] } = data;
  const cs = curr?.summary;
  const income = cs?.totalIncome ?? 0;
  const expensesTotal = cs?.totalExpenses ?? 0;
  const cashFlow = cs?.remainingBalance ?? 0;
  const totalLeads = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;
  const openRate = email?.openRate ?? 0;

  const revMoM = cs && prev?.summary && prev.summary.totalIncome > 0 
    ? ((income - prev.summary.totalIncome) / prev.summary.totalIncome) * 100 
    : 0;
  const expMoM = cs && prev?.summary && prev.summary.totalExpenses > 0 
    ? ((expensesTotal - prev.summary.totalExpenses) / prev.summary.totalExpenses) * 100 
    : 0;

  // KPIs
  const kpis = [
    { label: 'Receita do Mês', value: income, prefix: 'R$ ', delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, spark: trend.map(t => t.income), color: C.primary, icon: <DollarSign size={14} /> },
    { label: 'Total Despesas', value: expensesTotal, prefix: 'R$ ', delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, spark: trend.map(t => t.expense), color: C.warn, icon: <Wallet size={14} /> },
    { label: 'Leads no Funil', value: totalLeads, spark: [funnel.lead, funnel.contacted, funnel.qualified, funnel.negotiating, funnel.customer], color: C.info, up: true, icon: <Users size={14} /> },
    { label: 'Abertura Email', value: openRate, suffix: '%', color: C.accent, up: openRate >= 20, icon: <Mail size={14} /> },
  ];

  // Radar Health
  const maxTrend = Math.max(0, ...trend.map(t => t.income));
  const convRate = (totalLeads + funnel.customer) > 0 ? (funnel.customer / (totalLeads + funnel.customer)) * 100 : 0;
  const healthMetrics = [
    { k: 'Receita', v: maxTrend > 0 ? Math.max(0, Math.min(100, (income / maxTrend) * 100)) : (income > 0 ? 100 : 0) },
    { k: 'Fluxo', v: income > 0 ? Math.max(0, Math.min(100, (cashFlow / income) * 100)) : (cashFlow >= 0 ? 60 : 0) },
    { k: 'Conversão', v: Math.max(0, Math.min(100, convRate * 4)) },
    { k: 'Email', v: Math.max(0, Math.min(100, (openRate / 30) * 100)) },
    { k: 'Pipeline', v: Math.max(0, Math.min(100, (totalLeads / 20) * 100)) },
    { k: 'Investível', v: income > 0 ? Math.max(0, Math.min(100, ((cs?.availableToInvest ?? 0) / income) * 100)) : 0 },
  ];
  const healthScore = Math.round(healthMetrics.reduce((a, b) => a + b.v, 0) / healthMetrics.length);
  const scoreColor = healthScore >= 70 ? C.success : healthScore >= 45 ? C.warn : C.loss;

  // CommandCenter calculations
  const worstDrop = [
    { lbl: 'Lead→Contato', v: funnel.lead > 0 ? (1 - funnel.contacted / funnel.lead) * 100 : 0 },
    { lbl: 'Contato→Qualif', v: funnel.contacted > 0 ? (1 - funnel.qualified / funnel.contacted) * 100 : 0 },
    { lbl: 'Qualif.→Neg.', v: funnel.qualified > 0 ? (1 - funnel.negotiating / funnel.qualified) * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b, { lbl: 'None', v: 0 });

  const answers = [
    { q: 'O que funciona?', icon: <CheckCircle size={14} />, c: revMoM >= 0 ? C.primary : C.warn, text: revMoM > 0 ? `Receita +${revMoM.toFixed(1)}% vs mês ant.` : 'Receita estável', href: '/finance' },
    { q: 'O que está quebrado?', icon: <AlertTriangle size={14} />, c: worstDrop.v > 50 ? C.loss : C.warn, text: totalLeads > 0 ? `${worstDrop.v.toFixed(0)}% drop em ${worstDrop.lbl}` : 'Pipeline vazio', href: '/leads' },
    { q: 'Onde perde dinheiro?', icon: <DollarSign size={14} />, c: cashFlow < 0 ? C.loss : C.warn, text: cashFlow < 0 ? `Fluxo negativo: ${S(cashFlow)}` : expenses.length > 0 ? `${expenses[0].name}: ${expenses[0].pct.toFixed(0)}% das despesas` : 'Verifique despesas', href: '/finance' },
    { q: 'O que fazer agora?', icon: <Zap size={14} />, c: C.info, text: totalLeads > 0 ? `${totalLeads} leads aguardam ação` : 'Adicione leads', href: '/leads' },
    { q: 'Qual ação gera receita?', icon: <Target size={14} />, c: C.primary, text: email && email.openRate > 0 ? `Email: ${email.openRate.toFixed(1)}% abertura` : convRate > 0 ? `Conversão: ${convRate.toFixed(1)}%` : 'Analise canais', href: '/outreach' },
  ];

  return (
    <div className="space-y-6">
      {/* Hero Header */}
      <DashboardHero data={data} loading={false} mounted={mounted} />

      {/* CommandCenter */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3.5">
        {answers.map((a, i) => (
          <Link key={i} href={a.href} className="group no-underline">
            <div
              style={{ background: `${a.c}0a`, borderColor: `${a.c}22` }}
              className="p-4 rounded-xl border hover:border-zinc-700/60 transition-all duration-300 h-full flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <span style={{ color: a.c }}>{a.icon}</span>
                  <span className="text-[10px] font-bold uppercase tracking-wider" style={{ color: a.c }}>{a.q}</span>
                </div>
                <div className="text-xs font-bold text-zinc-100 leading-normal">{a.text}</div>
              </div>
              <div className="mt-3 flex items-center gap-1 text-[10px] font-bold transition-all group-hover:translate-x-1" style={{ color: a.c }}>
                <span>Acessar</span>
                <span>→</span>
              </div>
            </div>
          </Link>
        ))}
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
        {kpis.map((k, idx) => (
          <MetricCard key={k.label} {...k} idx={idx} />
        ))}
      </div>

      {/* Health Radar section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <div className="lg:col-span-8 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Saúde do Negócio</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Métricas de saúde corporativa normalizadas</p>
            </div>
            <HealthBadge status={healthScore >= 70 ? 'healthy' : healthScore >= 45 ? 'warning' : 'critical'} label={`${healthScore} Score`} />
          </div>
          {mounted && (
            <ApexChart
              type="radar"
              height={280}
              series={[{ name: 'Score', data: healthMetrics.map(m => Math.round(m.v)) }]}
              options={{
                chart: { toolbar: { show: false }, background: 'transparent' },
                labels: healthMetrics.map(m => m.k),
                colors: [C.primary],
                stroke: { width: 2 },
                fill: { opacity: 0.15 },
                markers: { size: 3, colors: [C.primary] },
                yaxis: { show: false, min: 0, max: 100 },
                xaxis: { labels: { style: { colors: healthMetrics.map(() => C.text2), fontSize: '11px', fontWeight: 600 } } },
                plotOptions: { radar: { polygons: { strokeColors: C.grid, connectorColors: C.grid } } },
                tooltip: { theme: 'dark', y: { formatter: (v: number) => `${v}/100` } },
              }}
            />
          )}
        </div>

        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100">Visão do Score</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Geral ponderado</p>
          </div>
          {mounted && (
            <ApexChart
              type="radialBar"
              height={200}
              series={[healthScore]}
              options={{
                chart: { background: 'transparent' },
                colors: [scoreColor],
                plotOptions: {
                  radialBar: {
                    hollow: { size: '60%' },
                    track: { background: 'rgba(255,255,255,0.05)' },
                    dataLabels: {
                      name: { show: false },
                      value: { show: true, color: C.text, fontSize: '32px', fontFamily: 'var(--font-mono)', fontWeight: 800, offsetY: 10 },
                    },
                  },
                },
                stroke: { lineCap: 'round' },
              }}
            />
          )}
          <div className="space-y-2.5">
            {healthMetrics.slice(0, 4).map(m => (
              <div key={m.k} className="flex items-center justify-between text-xs">
                <span className="font-semibold text-zinc-400">{m.k}</span>
                <span className="font-mono font-bold text-zinc-200">{Math.round(m.v)}%</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Finance and Funnel section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <ChartCard
          title="Receita vs Despesas"
          subtitle="Últimos 6 meses"
          className="lg:col-span-7"
          loading={false}
          hasData={trend.length > 0}
          chartConfig={mounted ? {
            type: 'line',
            height: 250,
            series: [
              { name: 'Receita', type: 'area', data: trend.map(t => Math.round(t.income)) },
              { name: 'Despesas', type: 'line', data: trend.map(t => Math.round(t.expense)) },
            ],
            options: {
              chart: { toolbar: { show: false }, background: 'transparent' },
              colors: [C.primary, C.warn],
              stroke: { curve: 'smooth', width: [3, 2], dashArray: [0, 4] },
              fill: { type: ['gradient', 'none'], gradient: { opacityFrom: 0.2, opacityTo: 0 } },
              xaxis: { categories: trend.map(t => t.label), labels: { style: { colors: C.muted } } },
              yaxis: { labels: { style: { colors: C.muted }, formatter: (v: number) => `R$ ${(v / 1000).toFixed(0)}k` } },
              grid: { borderColor: C.grid, strokeDashArray: 4 },
              legend: { labels: { colors: C.text2 }, position: 'top', horizontalAlign: 'right' },
              tooltip: { theme: 'dark', y: { formatter: (v: number) => R(v) } },
            }
          } : null}
          height={250}
        />

        <ChartCard
          title="Funil Comercial"
          subtitle="Distribuição dos contatos por etapa do funil"
          className="lg:col-span-5"
          loading={false}
          hasData={true}
          chartConfig={mounted ? {
            type: 'bar',
            height: 250,
            series: [{ name: 'Contatos', data: [funnel.lead, funnel.contacted, funnel.qualified, funnel.negotiating, funnel.customer] }],
            options: {
              chart: { toolbar: { show: false }, background: 'transparent' },
              plotOptions: { bar: { horizontal: true, barHeight: '55%', borderRadius: 6, distributed: true } },
              colors: [...C.funnel],
              xaxis: { categories: ['Leads', 'Contato', 'Qualif.', 'Negoc.', 'Clientes'], labels: { style: { colors: C.muted } } },
              yaxis: { labels: { style: { colors: C.text2, fontSize: '11px', fontWeight: 600 } } },
              grid: { borderColor: C.grid },
              dataLabels: { enabled: true, style: { fontSize: '10px' } },
              legend: { show: false },
              tooltip: { theme: 'dark' },
            }
          } : null}
          height={250}
        />
      </div>

      {/* Mini Agenda, Tasks, Checklist Workspace & Actionable Insights */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <div className="lg:col-span-8 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl space-y-4">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4 pb-2 border-b border-zinc-800/40">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Controle Pessoal & Operacional</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Espaço unificado de produtividade e prioridades</p>
            </div>
            <div className="flex gap-1 bg-zinc-900/60 p-0.5 rounded-lg border border-zinc-800/60 text-[10px] font-bold self-end sm:self-center shrink-0">
              <button
                type="button"
                onClick={() => setActiveSubTab('agenda')}
                className={cn(
                  "px-2.5 py-1.5 rounded-md transition-all cursor-pointer",
                  activeSubTab === 'agenda' ? "bg-teal-500 text-zinc-950 shadow-sm" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                Agenda ({agenda.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveSubTab('tasks')}
                className={cn(
                  "px-2.5 py-1.5 rounded-md transition-all cursor-pointer",
                  activeSubTab === 'tasks' ? "bg-teal-500 text-zinc-950 shadow-sm" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                Tarefas ({tasks.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveSubTab('checklist')}
                className={cn(
                  "px-2.5 py-1.5 rounded-md transition-all cursor-pointer",
                  activeSubTab === 'checklist' ? "bg-teal-500 text-zinc-950 shadow-sm" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                Checklist ({checklists.length})
              </button>
            </div>
          </div>

          {activeSubTab === 'agenda' && (
            <div className="space-y-2.5">
              {agenda.length > 0 ? (
                agenda.slice(0, 5).map((item, idx) => (
                  <div key={idx} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors">
                    <div className="flex items-center gap-3">
                      <span className="px-2.5 py-1 text-[10px] font-mono font-bold text-teal-400 bg-teal-500/10 rounded-lg shrink-0">
                        {item.time}
                      </span>
                      <span className="text-xs font-bold text-zinc-100">{item.title}</span>
                    </div>
                    <Link href="/agenda" className="text-zinc-500 hover:text-zinc-300">
                      <ArrowRight className="h-3.5 w-3.5" />
                    </Link>
                  </div>
                ))
              ) : (
                <div className="flex flex-col items-center justify-center py-12 text-zinc-500">
                  <Calendar className="h-8 w-8 stroke-[1.5] mb-2 text-zinc-600" />
                  <span className="text-xs font-semibold">Sem compromissos agendados para hoje</span>
                  <Link href="/agenda" className="mt-2.5 text-[10px] font-bold text-teal-400 hover:underline">
                    Ir para Agenda
                  </Link>
                </div>
              )}
            </div>
          )}

          {activeSubTab === 'tasks' && (
            <div className="space-y-2.5">
              {tasks.length > 0 ? (
                tasks.slice(0, 5).map((task) => (
                  <div key={task.id} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors">
                    <div className="flex items-center gap-3 min-w-0">
                      <span className={cn(
                        "px-2 py-0.5 text-[9px] font-bold uppercase rounded-md shrink-0",
                        task.priority === 'Urgent' && "bg-red-500/15 text-red-400 border border-red-500/20",
                        task.priority === 'High' && "bg-amber-500/15 text-amber-400 border border-amber-500/20",
                        task.priority === 'Medium' && "bg-blue-500/15 text-blue-400 border border-blue-500/20",
                        task.priority === 'Low' && "bg-zinc-800 text-zinc-400"
                      )}>
                        {task.priority === 'Urgent' ? 'Urgente' : task.priority === 'High' ? 'Alta' : task.priority === 'Medium' ? 'Média' : 'Baixa'}
                      </span>
                      <span className="text-xs font-bold text-zinc-100 truncate">{task.title}</span>
                    </div>
                    <Link href="/tasks" className="text-zinc-500 hover:text-zinc-300 shrink-0">
                      <ArrowRight className="h-3.5 w-3.5" />
                    </Link>
                  </div>
                ))
              ) : (
                <div className="flex flex-col items-center justify-center py-12 text-zinc-500">
                  <ListChecks className="h-8 w-8 stroke-[1.5] mb-2 text-zinc-600" />
                  <span className="text-xs font-semibold">Todas as tarefas concluídas!</span>
                  <Link href="/tasks" className="mt-2.5 text-[10px] font-bold text-teal-400 hover:underline">
                    Criar Nova Tarefa
                  </Link>
                </div>
              )}
            </div>
          )}

          {activeSubTab === 'checklist' && (
            <div className="space-y-2.5">
              {checklists.length > 0 ? (
                checklists.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors">
                    <div className="flex items-center gap-3 min-w-0">
                      <span className={cn(
                        "w-2 h-2 rounded-full shrink-0",
                        item.priority === 3 && "bg-red-500", // Urgent
                        item.priority === 2 && "bg-amber-500", // High
                        item.priority === 1 && "bg-blue-500", // Medium
                        item.priority === 0 && "bg-zinc-500" // Low
                      )} />
                      <div className="min-w-0">
                        <span className="text-xs font-bold text-zinc-100 block truncate">{item.title}</span>
                        <span className="text-[9px] text-zinc-500 font-bold uppercase tracking-wider">{item.categoryName || 'Geral'}</span>
                      </div>
                    </div>
                    <Link href="/household/checklists" className="text-zinc-500 hover:text-zinc-300 shrink-0">
                      <ArrowRight className="h-3.5 w-3.5" />
                    </Link>
                  </div>
                ))
              ) : (
                <div className="flex flex-col items-center justify-center py-12 text-zinc-500">
                  <CheckCircle className="h-8 w-8 stroke-[1.5] mb-2 text-zinc-600" />
                  <span className="text-xs font-semibold">Nenhum item pendente no checklist</span>
                  <Link href="/household/checklists" className="mt-2.5 text-[10px] font-bold text-teal-400 hover:underline">
                    Adicionar Item
                  </Link>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="lg:col-span-4 space-y-4">
          <InsightCard
            title="Gargalo comercial detectado"
            badgeText="Funil de Leads"
            context={totalLeads > 0 ? `Queda acentuada na transição de ${worstDrop.lbl}.` : "Pipeline sem leads suficientes."}
            impact={totalLeads > 0 ? `${worstDrop.v.toFixed(0)}% de taxa de abandono identificada.` : "Impossibilidade de traçar taxas de conversão precisas."}
            actionRecommended={totalLeads > 0 ? "Dispare follow-ups ou campanhas de reengajamento focadas." : "Cadastre ou importe novos leads para iniciar prospecção."}
            actionText="Ver Leads Relacionados"
            actionHref="/leads"
            color="amber"
          />
          <InsightCard
            title="Fluxo de Caixa do Mês"
            badgeText="Gestão Financeira"
            context={cashFlow >= 0 ? `Saldo líquido operacional de ${R(cashFlow)} no azul.` : `Déficit líquido operacional de ${R(Math.abs(cashFlow))}.`}
            impact={cashFlow >= 0 ? "Bons níveis de liquidez para alocação em tráfego ou CAC." : "Risco de redução de margens e saldo geral de contas."}
            actionRecommended={cashFlow >= 0 ? "Excelente momento para escalar aquisição de leads." : "Estude renegociar despesas recorrentes e pagamentos pendentes."}
            actionText="Ver Dashboard Financeiro"
            actionHref="/finance"
            color={cashFlow >= 0 ? 'teal' : 'purple'}
          />
          {tasks.length > 0 && (
            <InsightCard
              title="Ações Comerciais Pendentes"
              badgeText="Controle de Tarefas"
              context={`Existem ${tasks.length} tarefas de prospecção pendentes.`}
              impact="Risco de perda de velocidade (timing) na negociação comercial."
              actionRecommended="Conclua os contatos agendados para destravar negociações."
              actionText="Gerenciar Minhas Tarefas"
              actionHref="/tasks"
              color="blue"
            />
          )}
        </div>
      </div>
    </div>
  );
}
