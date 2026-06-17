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
  Target, TrendingDown, TrendingUp, Users, Wallet, Zap
} from 'lucide-react';
import Link from 'next/link';
import dynamic from 'next/dynamic';

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

  const { funnel, curr, prev, trend, expenses, email, agenda } = data;
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
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
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

        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
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
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <ChartCard
          title="Receita vs Despesas"
          subtitle="Últimos 6 meses"
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

      {/* Mini Agenda and Insights */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl space-y-4">
          <h3 className="text-sm font-bold text-zinc-100">Próximos Compromissos</h3>
          {agenda.length > 0 ? (
            <div className="space-y-2.5">
              {agenda.slice(0, 4).map((item, idx) => (
                <div key={idx} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40">
                  <div className="flex items-center gap-3">
                    <span className="px-2.5 py-1 text-[10px] font-mono font-bold text-teal-400 bg-teal-500/10 rounded-lg">
                      {item.time}
                    </span>
                    <span className="text-xs font-bold text-zinc-100">{item.title}</span>
                  </div>
                  <ArrowRight className="h-3.5 w-3.5 text-zinc-600" />
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-8 text-zinc-500">
              <span className="text-2xl mb-1">📅</span>
              <span className="text-xs font-semibold">Sem compromissos marcados para hoje</span>
            </div>
          )}
        </div>

        <div className="space-y-4">
          <InsightCard
            title="Gargalo comercial detectado"
            description={totalLeads > 0 ? `Queda acentuada de ${worstDrop.v.toFixed(0)}% na etapa de ${worstDrop.lbl}. Considere disparar uma campanha específica para destravar.` : 'Pipeline comercial sem gargalos ativos.'}
            color="amber"
          />
          <InsightCard
            title="Caixa do mês atual"
            description={cashFlow >= 0 ? `Saldo líquido de ${R(cashFlow)} no verde. Bom momento para investimentos em tráfego ou captação.` : `Fluxo de caixa no vermelho por ${R(Math.abs(cashFlow))}. Monitore despesas pendentes.`}
            color={cashFlow >= 0 ? 'teal' : 'purple'}
          />
        </div>
      </div>
    </div>
  );
}
