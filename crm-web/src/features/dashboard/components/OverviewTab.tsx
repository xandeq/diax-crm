'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useDashboardOverview } from '../hooks/useDashboardOverview';
import { useOpsDashboard } from '../hooks/useOpsDashboard';
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
import { Button } from '@/components/ui/button';
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
  const { data: opsData } = useOpsDashboard();
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

  const { funnel, curr, prev, trend, expenses, email, agenda, tasks = [], checklists = [], recentLeads = [] } = data;

  // Defensive safeguards
  const funnelSafe = funnel || { lead: 0, contacted: 0, qualified: 0, negotiating: 0, customer: 0 };
  const emailSafe = email || { openRate: 0 };
  const trendSafe = Array.isArray(trend) ? trend : [];
  const agendaSafe = Array.isArray(agenda) ? agenda : [];
  const tasksSafe = Array.isArray(tasks) ? tasks : [];
  const checklistsSafe = Array.isArray(checklists) ? checklists : [];
  const recentLeadsSafe = Array.isArray(recentLeads) ? recentLeads : [];
  const expensesSafe = Array.isArray(expenses) ? expenses : [];

  const cs = curr?.summary || { totalIncome: 0, totalExpenses: 0, remainingBalance: 0, availableToInvest: 0 };
  const income = cs.totalIncome;
  const expensesTotal = cs.totalExpenses;
  const cashFlow = cs.remainingBalance;
  const totalLeads = funnelSafe.lead + funnelSafe.contacted + funnelSafe.qualified + funnelSafe.negotiating;
  const openRate = emailSafe.openRate;

  const revMoM = cs && prev?.summary && prev.summary.totalIncome > 0 
    ? ((income - prev.summary.totalIncome) / prev.summary.totalIncome) * 100 
    : 0;
  const expMoM = cs && prev?.summary && prev.summary.totalExpenses > 0 
    ? ((expensesTotal - prev.summary.totalExpenses) / prev.summary.totalExpenses) * 100 
    : 0;

  // --- COCKPIT & MONEY RADAR CALCULATIONS ---
  const sevenDaysAgo = new Date();
  sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);

  const staleLeads = recentLeadsSafe.filter((l: any) => {
    if (!l) return false;
    if (l.status === 4 || l.status === 5 || l.status === 6) return false;
    return !l.lastContactAt || new Date(l.lastContactAt) < sevenDaysAgo;
  });

  const getEstimatedValue = (status: number) => {
    if (status === 3) return 10000; // Negotiating
    if (status === 2) return 5000;  // Qualified
    if (status === 1) return 3000;  // Contacted
    return 1500;                    // Lead
  };

  const potentialPipelineValue = 
    (funnelSafe.lead * 1500) + 
    (funnelSafe.contacted * 3000) + 
    (funnelSafe.qualified * 5000) + 
    (funnelSafe.negotiating * 10000);

  const staleValue = staleLeads.reduce((acc: number, l: any) => acc + getEstimatedValue(l.status), 0);

  // Contas a receber/pagar pendentes reais do mês
  const unpaidExpenses = Array.isArray(curr?.expenses) ? curr.expenses.filter((t: any) => t && t.status === 1).reduce((acc: number, t: any) => acc + Math.abs(t.amount), 0) : 0;
  const unpaidIncomes = Array.isArray(curr?.incomes) ? curr.incomes.filter((t: any) => t && t.status === 1).reduce((acc: number, t: any) => acc + t.amount, 0) : 0;

  const errorStats = opsData?.errorStats;

  const betterChannel = openRate >= 20 ? 'E-mail Marketing' : 'WhatsApp Outreach';

  // Prioridade número 1 do dia com Módulo Relacionado
  let topPriorityTitle = "Manter Pipeline Aquecido";
  let topPriorityHref = "/dashboard";
  let topPriorityAction = "Verificar Status";
  let topPriorityReason = "Todas as operações estão rodando perfeitamente e as finanças estão sob controle.";
  let topPriorityImpact = "Nenhum risco financeiro ou operacional detectado.";
  let topPriorityActionDesc = "Acesse Outreach & Marketing e analise novas ideias de campanhas.";
  let topPriorityModule = "Geral";

  if (errorStats && errorStats.criticalToday > 0) {
    topPriorityTitle = `Corrigir ${errorStats.criticalToday} Erro(s) Crítico(s) no Sistema`;
    topPriorityHref = "/logs";
    topPriorityAction = "Investigar";
    topPriorityReason = "Falhas graves detectadas nas últimas 24 horas que podem impedir integrações essenciais.";
    topPriorityImpact = "Risco de paralisação nas automações n8n e perda de leads capturados.";
    topPriorityActionDesc = "Acesse a aba Monitoramento & Ops, investigue os logs e reinicie a fila.";
    topPriorityModule = "Operações / Ops";
  } else if (staleLeads.length > 0) {
    const mainStaleLead = staleLeads[0];
    const estVal = getEstimatedValue(mainStaleLead.status);
    topPriorityTitle = `Follow-up: ${mainStaleLead.name} (${mainStaleLead.companyName || 'Lead Ocioso'})`;
    topPriorityHref = "/leads";
    topPriorityAction = "Contatar";
    topPriorityReason = `Lead qualificado sem contato há mais de 7 dias (${mainStaleLead.status === 3 ? 'Negociação' : mainStaleLead.status === 2 ? 'Qualificado' : 'Ativo'}).`;
    topPriorityImpact = `Risco de perda de R$ ${estVal.toLocaleString('pt-BR')} em receita potencial ociosa.`;
    topPriorityActionDesc = "Envie uma mensagem rápida por WhatsApp ou E-mail para reengajar o lead.";
    topPriorityModule = "CRM / Comercial";
  } else if (tasksSafe.filter((t: any) => t && t.priority === 'Urgent').length > 0) {
    const urgentTask = tasksSafe.find((t: any) => t && t.priority === 'Urgent');
    if (urgentTask) {
      topPriorityTitle = `Tarefa Urgente: ${urgentTask.title}`;
      topPriorityHref = "/tasks";
      topPriorityAction = "Resolver";
      topPriorityReason = "Tarefa operacional ou comercial de prioridade crítica aguardando conclusão.";
      topPriorityImpact = "Gargalo operacional no pipeline comercial ou entrega de serviço.";
      topPriorityActionDesc = "Abra a central de tarefas e conclua o item antes do encerramento.";
      topPriorityModule = "Operações / Agenda";
    }
  } else if (unpaidExpenses > 0) {
    topPriorityTitle = `Contas Pendentes: Liquidar Despesas`;
    topPriorityHref = "/finance";
    topPriorityAction = "Efetuar Pagamento";
    topPriorityReason = "Despesas recorrentes ou pontuais em aberto vencendo no mês corrente.";
    topPriorityImpact = `Acúmulo de passivos no valor de R$ ${unpaidExpenses.toLocaleString('pt-BR')} em aberto.`;
    topPriorityActionDesc = "Acesse a aba Financeiro, confira as despesas pendentes e liquide a conta.";
    topPriorityModule = "Financeiro / Caixa";
  }

  // Foco Dinâmico do dia
  let dayFocusTitle = "Foco em Prospecção & Vendas";
  let dayFocusDesc = "O pipeline está ativo e com oportunidades abertas. Priorize iniciar contatos e fechar negócios hoje.";
  let dayFocusBadge = "Comercial";
  let dayFocusColor = "text-teal-400 bg-teal-500/10 border-teal-500/20";

  const simulation = data.simulation;
  if (cashFlow < 0 || simulation?.hasNegativeBalanceRisk) {
    dayFocusTitle = "Foco em Controle de Caixa & Redução de Riscos";
    dayFocusDesc = "Alerta: Despesas altas ou risco de caixa detectado para este mês. Foco total em cobrar entradas pendentes e conter gastos.";
    dayFocusBadge = "Financeiro";
    dayFocusColor = "text-red-400 bg-red-500/10 border-red-500/20";
  } else if (errorStats && errorStats.criticalToday > 0) {
    dayFocusTitle = "Foco em Estabilidade & Ops";
    dayFocusDesc = "Erros críticos detectados na data de hoje. Priorize a correção e verificação de logs para evitar falhas em automações.";
    dayFocusBadge = "Operações";
    dayFocusColor = "text-amber-400 bg-amber-500/10 border-amber-500/20";
  } else if (tasksSafe.length > 5 || checklistsSafe.length > 8) {
    dayFocusTitle = "Foco Operacional: Fila de Atividades";
    dayFocusDesc = "Você tem um volume considerável de tarefas e itens de checklist pendentes. Limpe sua fila operacional para não acumular.";
    dayFocusBadge = "Operações";
    dayFocusColor = "text-blue-400 bg-blue-500/10 border-blue-500/20";
  }

  // KPIs (Money Radar metrics)
  const kpis = [
    { label: 'Receita Potencial Aberta', value: potentialPipelineValue, prefix: 'R$ ', spark: [funnelSafe.lead, funnelSafe.contacted, funnelSafe.qualified, funnelSafe.negotiating], color: C.primary, icon: <DollarSign size={14} /> },
    { label: 'Valor Ocioso Parado', value: staleValue, prefix: 'R$ ', spark: staleLeads.length > 0 ? [staleLeads.length] : undefined, color: C.warn, icon: <AlertTriangle size={14} /> },
    { label: 'Contas a Receber (Pendente)', value: unpaidIncomes, prefix: 'R$ ', color: C.success, icon: <TrendingUp size={14} /> },
    { label: 'Contas a Pagar (Pendente)', value: unpaidExpenses, prefix: 'R$ ', color: C.loss, icon: <TrendingDown size={14} /> },
  ];

  // Radar Health
  const maxTrend = trendSafe.length > 0 ? Math.max(0, ...trendSafe.map(t => t?.income || 0)) : 0;
  const convRate = (totalLeads + funnelSafe.customer) > 0 ? (funnelSafe.customer / (totalLeads + funnelSafe.customer)) * 100 : 0;
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
    { lbl: 'Lead→Contato', v: funnelSafe.lead > 0 ? (1 - funnelSafe.contacted / funnelSafe.lead) * 100 : 0 },
    { lbl: 'Contato→Qualif', v: funnelSafe.contacted > 0 ? (1 - funnelSafe.qualified / funnelSafe.contacted) * 100 : 0 },
    { lbl: 'Qualif.→Neg.', v: funnelSafe.qualified > 0 ? (1 - funnelSafe.negotiating / funnelSafe.qualified) * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b, { lbl: 'None', v: 0 });

  const answers = [
    { q: 'O que funciona?', icon: <CheckCircle size={14} />, c: revMoM >= 0 ? C.primary : C.warn, text: revMoM > 0 ? `Receita +${revMoM.toFixed(1)}% vs mês ant.` : 'Receita estável', href: '/finance' },
    { q: 'O que está quebrado?', icon: <AlertTriangle size={14} />, c: worstDrop.v > 50 ? C.loss : C.warn, text: totalLeads > 0 ? `${worstDrop.v.toFixed(0)}% drop em ${worstDrop.lbl}` : 'Pipeline vazio', href: '/leads' },
    { q: 'Onde perde dinheiro?', icon: <DollarSign size={14} />, c: cashFlow < 0 ? C.loss : C.warn, text: cashFlow < 0 ? `Fluxo negativo: ${S(cashFlow)}` : expensesSafe.length > 0 ? `${expensesSafe[0].name}: ${expensesSafe[0].pct.toFixed(0)}% das despesas` : 'Verifique despesas', href: '/finance' },
    { q: 'O que fazer agora?', icon: <Zap size={14} />, c: C.info, text: totalLeads > 0 ? `${totalLeads} leads aguardam ação` : 'Adicione leads', href: '/leads' },
    { q: 'Qual ação gera receita?', icon: <Target size={14} />, c: C.primary, text: emailSafe.openRate > 0 ? `Email: ${emailSafe.openRate.toFixed(1)}% abertura` : convRate > 0 ? `Conversão: ${convRate.toFixed(1)}%` : 'Analise canais', href: '/outreach' },
  ];

  return (
    <div className="space-y-6">
      {/* Hero Header */}
      <DashboardHero data={data} loading={false} mounted={mounted} />

      {/* Cockpit de Foco: O que Fazer Hoje */}
      <div className="p-5 bg-zinc-950/40 border border-zinc-900/60 rounded-2xl backdrop-blur-xl relative overflow-hidden shadow-2xl">
        {/* Glow effect */}
        <div className="absolute -right-20 -top-20 w-80 h-80 bg-teal-500/10 rounded-full blur-[100px] pointer-events-none" />
        
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 relative z-10">
          {/* Foco Dinâmico */}
          <div className="lg:col-span-6 space-y-3">
            <div className="flex items-center gap-2">
              <Zap className="h-5 w-5 text-teal-400 animate-pulse" />
              <h3 className="text-sm font-bold text-zinc-400 uppercase tracking-wider">Cockpit de Foco</h3>
            </div>
            
            <div className="space-y-1.5">
              <div className="flex items-center gap-2.5">
                <span className={cn("px-2 py-0.5 text-[10px] font-bold uppercase rounded-md border shrink-0", dayFocusColor)}>
                  {dayFocusBadge}
                </span>
                <h2 className="text-lg font-extrabold text-zinc-100">{dayFocusTitle}</h2>
              </div>
              <p className="text-xs text-zinc-400 leading-relaxed max-w-xl">
                {dayFocusDesc}
              </p>
            </div>

            {/* Canal de Maior ROI Banner inside Foco */}
            <div className="flex items-center gap-2 p-2 bg-zinc-900/30 border border-zinc-850 rounded-xl max-w-md">
              <TrendingUp className="h-4 w-4 text-emerald-400 shrink-0" />
              <span className="text-[10px] text-zinc-400 font-semibold">
                Melhor desempenho atual: <strong className="text-zinc-200">{betterChannel}</strong> (conversão estimada de R$ {openRate >= 20 ? '0.15' : '0.45'} por envio).
              </span>
            </div>
          </div>

          {/* Ações Recomendadas / Next Best Actions */}
          <div className="lg:col-span-6 space-y-3 border-t lg:border-t-0 lg:border-l border-zinc-800/60 pt-4 lg:pt-0 lg:pl-6">
            <div className="flex items-center justify-between">
              <h3 className="text-xs font-bold text-zinc-500 uppercase tracking-wider">Ações Prioritárias</h3>
              <span className="text-[10px] font-bold text-teal-400 animate-pulse">P0</span>
            </div>

            <div className="space-y-2.5">
              {/* Prioridade Principal P0 Enriquecida */}
              <div className="p-4 rounded-xl bg-teal-500/5 border border-teal-500/15 hover:border-teal-500/30 transition-all space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="relative flex h-2 w-2">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-teal-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-teal-400"></span>
                    </span>
                    <span className="text-[10px] text-teal-400 font-extrabold uppercase tracking-wider">Ação Recomendada P0 ({topPriorityModule})</span>
                  </div>
                  <span className="px-2 py-0.5 text-[9px] font-bold bg-teal-500/10 text-teal-300 border border-teal-500/20 rounded-md shrink-0">
                    {topPriorityTitle.includes('Erro') ? 'Crítico' : 'Urgente'}
                  </span>
                </div>

                <div className="space-y-1">
                  <h4 className="text-xs font-extrabold text-zinc-150">{topPriorityTitle}</h4>
                  <p className="text-xs text-zinc-400 leading-relaxed">
                    <strong className="text-zinc-300">Motivo:</strong> {topPriorityReason}
                  </p>
                  <p className="text-xs text-zinc-400 leading-relaxed">
                    <strong className="text-zinc-300">Impacto:</strong> <span className={cn(topPriorityImpact.includes('perda') || topPriorityImpact.includes('paralisação') || topPriorityImpact.includes('Gargalo') || topPriorityImpact.includes('Acúmulo') ? 'text-red-400 font-medium' : 'text-teal-400 font-medium')}>{topPriorityImpact}</span>
                  </p>
                </div>

                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-2 border-t border-zinc-800/60">
                  <span className="text-xs text-zinc-500 leading-relaxed">
                    <strong className="text-zinc-400">Ação:</strong> {topPriorityActionDesc}
                  </span>
                  <Button asChild size="sm" className="h-8 bg-teal-400 hover:bg-teal-300 text-zinc-950 font-bold shrink-0 text-xs border-none shadow-[0_2px_10px_rgba(20,184,166,0.2)] cursor-pointer">
                    <Link href={topPriorityHref} className="flex items-center gap-1">
                      <span>{topPriorityAction}</span>
                      <ArrowRight className="h-3 w-3" />
                    </Link>
                  </Button>
                </div>
              </div>

              {/* Sugestões Operacionais */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {staleLeads.length > 0 && (
                  <Link href="/leads" className="p-2.5 bg-zinc-900/40 hover:bg-zinc-900/60 border border-zinc-850 rounded-xl flex items-center justify-between group transition-all">
                    <div className="min-w-0">
                      <span className="text-[9px] text-zinc-500 font-bold uppercase">Leads Ociosos</span>
                      <span className="text-xs font-bold text-zinc-300 block truncate">{staleLeads.length} leads sem contato</span>
                    </div>
                    <ArrowRight className="h-3.5 w-3.5 text-zinc-500 group-hover:text-zinc-300 transition-transform group-hover:translate-x-0.5 shrink-0" />
                  </Link>
                )}
                {unpaidIncomes > 0 && (
                  <Link href="/finance" className="p-2.5 bg-zinc-900/40 hover:bg-zinc-900/60 border border-zinc-850 rounded-xl flex items-center justify-between group transition-all">
                    <div className="min-w-0">
                      <span className="text-[9px] text-zinc-500 font-bold uppercase">A Receber</span>
                      <span className="text-xs font-bold text-zinc-300 block truncate">{R(unpaidIncomes)} pendentes</span>
                    </div>
                    <ArrowRight className="h-3.5 w-3.5 text-zinc-500 group-hover:text-zinc-300 transition-transform group-hover:translate-x-0.5 shrink-0" />
                  </Link>
                )}
                {unpaidExpenses > 0 && (
                  <Link href="/finance" className="p-2.5 bg-zinc-900/40 hover:bg-zinc-900/60 border border-zinc-850 rounded-xl flex items-center justify-between group transition-all">
                    <div className="min-w-0">
                      <span className="text-[9px] text-zinc-500 font-bold uppercase">A Pagar</span>
                      <span className="text-xs font-bold text-zinc-300 block truncate">{R(unpaidExpenses)} pendentes</span>
                    </div>
                    <ArrowRight className="h-3.5 w-3.5 text-zinc-500 group-hover:text-zinc-300 transition-transform group-hover:translate-x-0.5 shrink-0" />
                  </Link>
                )}
                {errorStats && errorStats.totalToday > 0 && (
                  <Link href="/logs" className="p-2.5 bg-zinc-900/40 hover:bg-zinc-900/60 border border-zinc-850 rounded-xl flex items-center justify-between group transition-all">
                    <div className="min-w-0">
                      <span className="text-[9px] text-zinc-500 font-bold uppercase">Erros de Sistema</span>
                      <span className="text-xs font-bold text-zinc-300 block truncate">{errorStats.totalToday} falhas hoje</span>
                    </div>
                    <ArrowRight className="h-3.5 w-3.5 text-zinc-500 group-hover:text-zinc-300 transition-transform group-hover:translate-x-0.5 shrink-0" />
                  </Link>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* CommandCenter */}
      <div className="grid grid-cols-1 sm:grid-cols-3 lg:grid-cols-5 gap-3.5">
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

      {/* Money Radar (Métricas Financeiras Potenciais) */}
      <div className="space-y-3">
        <div>
          <h3 className="text-sm font-bold text-zinc-100">Money Radar</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Visão de valor potencial do pipeline e contas pendentes</p>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
          {kpis.map((k, idx) => (
            <MetricCard key={k.label} {...k} idx={idx} />
          ))}
        </div>
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
              { name: 'Receita', type: 'area', data: trendSafe.map(t => Math.round(t.income)) },
              { name: 'Despesas', type: 'line', data: trendSafe.map(t => Math.round(t.expense)) },
            ],
            options: {
              chart: { toolbar: { show: false }, background: 'transparent' },
              dataLabels: { enabled: false },
              colors: [C.primary, C.warn],
              stroke: { curve: 'smooth', width: [3, 2], dashArray: [0, 4] },
              fill: { type: ['gradient', 'none'], gradient: { opacityFrom: 0.2, opacityTo: 0 } },
              xaxis: { categories: trendSafe.map(t => t.label), labels: { style: { colors: C.muted } } },
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
            series: [{ name: 'Contatos', data: [funnelSafe.lead, funnelSafe.contacted, funnelSafe.qualified, funnelSafe.negotiating, funnelSafe.customer] }],
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
                Agenda ({agendaSafe.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveSubTab('tasks')}
                className={cn(
                  "px-2.5 py-1.5 rounded-md transition-all cursor-pointer",
                  activeSubTab === 'tasks' ? "bg-teal-500 text-zinc-950 shadow-sm" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                Tarefas ({tasksSafe.length})
              </button>
              <button
                type="button"
                onClick={() => setActiveSubTab('checklist')}
                className={cn(
                  "px-2.5 py-1.5 rounded-md transition-all cursor-pointer",
                  activeSubTab === 'checklist' ? "bg-teal-500 text-zinc-950 shadow-sm" : "text-zinc-400 hover:text-zinc-200"
                )}
              >
                Checklist ({checklistsSafe.length})
              </button>
            </div>
          </div>

          {activeSubTab === 'agenda' && (
            <div className="space-y-2.5">
              {agendaSafe.length > 0 ? (
                agendaSafe.slice(0, 5).map((item, idx) => (
                  <div key={idx} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors min-w-0">
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <span className="px-2.5 py-1 text-[10px] font-mono font-bold text-teal-400 bg-teal-500/10 rounded-lg shrink-0">
                        {item.time}
                      </span>
                      <span className="text-xs font-bold text-zinc-100 truncate flex-1">{item.title}</span>
                    </div>
                    <Link href="/agenda" className="text-zinc-500 hover:text-zinc-300 shrink-0 ml-2">
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
              {tasksSafe.length > 0 ? (
                tasksSafe.slice(0, 5).map((task) => (
                  <div key={task.id} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors min-w-0">
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <span className={cn(
                        "px-2 py-0.5 text-[9px] font-bold uppercase rounded-md shrink-0",
                        task.priority === 'Urgent' && "bg-red-500/15 text-red-400 border border-red-500/20",
                        task.priority === 'High' && "bg-amber-500/15 text-amber-400 border border-amber-500/20",
                        task.priority === 'Medium' && "bg-blue-500/15 text-blue-400 border border-blue-500/20",
                        task.priority === 'Low' && "bg-zinc-800 text-zinc-400"
                      )}>
                        {task.priority === 'Urgent' ? 'Urgente' : task.priority === 'High' ? 'Alta' : task.priority === 'Medium' ? 'Média' : 'Baixa'}
                      </span>
                      <span className="text-xs font-bold text-zinc-100 truncate flex-1">{task.title}</span>
                    </div>
                    <Link href="/tasks" className="text-zinc-500 hover:text-zinc-300 shrink-0 ml-2">
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
              {checklistsSafe.length > 0 ? (
                checklistsSafe.slice(0, 5).map((item) => (
                  <div key={item.id} className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/30 border border-zinc-800/40 hover:bg-zinc-900/50 transition-colors min-w-0">
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <span className={cn(
                        "w-2 h-2 rounded-full shrink-0",
                        item.priority === 3 && "bg-red-500", // Urgent
                        item.priority === 2 && "bg-amber-500", // High
                        item.priority === 1 && "bg-blue-500", // Medium
                        item.priority === 0 && "bg-zinc-500" // Low
                      )} />
                      <div className="min-w-0 flex-1">
                        <span className="text-xs font-bold text-zinc-100 block truncate">{item.title}</span>
                        <span className="text-[9px] text-zinc-500 font-bold uppercase tracking-wider block truncate">{item.categoryName || 'Geral'}</span>
                      </div>
                    </div>
                    <Link href="/household/checklists" className="text-zinc-500 hover:text-zinc-300 shrink-0 ml-2">
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
            impact={totalLeads > 0 ? `Estimado: -R$ ${staleValue.toLocaleString('pt-BR')} em receita potencial ociosa.` : "Impossibilidade de traçar taxas de conversão de leads."}
            actionRecommended={totalLeads > 0 ? "Dispare follow-ups ou campanhas de reengajamento focadas." : "Cadastre ou importe novos leads para iniciar prospecção."}
            actionText="Ver Leads Relacionados"
            actionHref="/leads"
            color="amber"
          />
          <InsightCard
            title="Fluxo de Caixa do Mês"
            badgeText="Gestão Financeira"
            context={cashFlow >= 0 ? `Saldo líquido operacional de ${R(cashFlow)} no azul.` : `Déficit líquido operacional de ${R(Math.abs(cashFlow))}.`}
            impact={cashFlow >= 0 ? `Estimado: +R$ ${cashFlow.toLocaleString('pt-BR')} de saldo líquido operacional.` : `Estimado: -R$ ${Math.abs(cashFlow).toLocaleString('pt-BR')} de déficit operacional.`}
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
              impact={`Estimado: -R$ ${(tasks.length * 1500).toLocaleString('pt-BR')} em receita potencial em atraso.`}
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
