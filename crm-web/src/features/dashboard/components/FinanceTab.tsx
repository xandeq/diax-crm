'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useFinanceDashboard } from '../hooks/useFinanceDashboard';
import { TransactionType } from '@/services/finance';
import { LoadingSkeleton, LoadingGrid } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { MetricCard } from '@/components/dashboard/MetricCard';
import { ChartCard } from '@/components/dashboard/ChartCard';
import { StatusBadge } from '@/components/dashboard/StatusBadge';
import { HealthBadge } from './HealthBadge';
import { Button } from '@/components/ui/button';
import { 
  DollarSign, Wallet, PiggyBank, CreditCard, AlertTriangle, 
  Target, TrendingUp, TrendingDown, ArrowRight, ShieldCheck, 
  Landmark, Activity, Zap
} from 'lucide-react';
import Link from 'next/link';
import dynamic from 'next/dynamic';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

const ACCOUNT_TYPE_LABELS: Record<number, string> = {
  0: 'Conta Corrente',
  1: 'Conta PJ',
  2: 'Poupança',
  3: 'Dinheiro',
  4: 'Investimento',
  5: 'Carteira Digital',
};

const C = {
  primary: '#00D4AA',
  success: '#22C55E',
  loss: '#EF4444',
  warn: '#F59E0B',
  info: '#818CF8',
  accent: '#F472B6',
  expenseColors: ['#F59E0B', '#EF4444', '#8B5CF6', '#6366F1', '#EC4899'],
  grid: 'rgba(255,255,255,0.045)',
  text2: '#b0b0ba',
  muted: '#6e6e7a',
};

const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });

export function FinanceTab() {
  const { data, isLoading, isError, error, refetch } = useFinanceDashboard();
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
        <LoadingGrid cards={4} />
        <LoadingSkeleton rows={1} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
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

  const {
    summary,
    income,
    expenses,
    cashFlow,
    revMoM,
    expMoM,
    categoryExpenses,
    creditCards,
    nearClosingClosingCards,
    accounts,
    goals,
    simulation,
    recentTransactions,
    openInvoicesTotal,
    alerts,
  } = data;

  const kpis = [
    { label: 'Saldo em Contas', value: accounts.reduce((acc, a) => acc + a.balance, 0), prefix: 'R$ ', color: C.primary, icon: <Landmark size={14} /> },
    { label: 'Receitas do Mês', value: income, prefix: 'R$ ', delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, color: C.success, icon: <DollarSign size={14} /> },
    { label: 'Despesas do Mês', value: expenses, prefix: 'R$ ', delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, color: C.warn, icon: <Wallet size={14} /> },
    { label: 'Fluxo de Caixa', value: Math.abs(cashFlow), prefix: cashFlow >= 0 ? 'R$ ' : 'R$ -', color: cashFlow >= 0 ? C.success : C.loss, icon: <PiggyBank size={14} /> },
  ];

  // Config do Fluxo Projetado
  const hasForecast = !!(simulation && simulation.dailyBalances?.length);
  const forecastDates = hasForecast ? simulation!.dailyBalances.map(d => new Date(d.date).getDate()) : [];
  const forecastData = hasForecast ? simulation!.dailyBalances.map(d => Math.round(d.closingBalance)) : [];
  const lowestBalance = simulation?.lowestProjectedBalance ?? 0;
  const endingBalance = simulation?.projectedEndingBalance ?? 0;

  return (
    <div className="space-y-6">
      {/* Alerts banner */}
      {alerts.length > 0 && (
        <div className="space-y-2">
          {alerts.map((alert, idx) => (
            <div key={idx} className="flex items-center gap-3 p-3 bg-amber-500/10 border border-amber-500/20 rounded-xl text-amber-400 text-xs font-semibold">
              <AlertTriangle className="h-4 w-4 shrink-0" />
              <span>{alert}</span>
            </div>
          ))}
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
        {kpis.map((k, idx) => (
          <MetricCard key={k.label} {...k} idx={idx} />
        ))}
      </div>

      {/* Cash Flow Forecast area */}
      <ChartCard
        title="Projeção de Fluxo de Caixa Diário"
        subtitle="Variação estimada do saldo em conta durante o mês corrente"
        loading={false}
        hasData={hasForecast}
        chartConfig={mounted && hasForecast ? {
          type: 'area',
          height: 280,
          series: [{ name: 'Saldo Projetado', data: forecastData }],
          options: {
            chart: { toolbar: { show: false }, background: 'transparent' },
            colors: [lowestBalance < 0 ? C.loss : C.primary],
            stroke: { curve: 'smooth', width: 3 },
            fill: { type: 'gradient', gradient: { opacityFrom: 0.25, opacityTo: 0 } },
            xaxis: { categories: forecastDates, labels: { style: { colors: C.muted } }, title: { text: 'Dia do Mês', style: { color: C.muted } } },
            yaxis: { labels: { style: { colors: C.muted }, formatter: (v: number) => `R$ ${(v / 1000).toFixed(0)}k` } },
            grid: { borderColor: C.grid, strokeDashArray: 4 },
            tooltip: { theme: 'dark', y: { formatter: (v: number) => R(v) } },
          }
        } : null}
        height={280}
        footer={
          hasForecast ? (
            <div className="grid grid-cols-3 gap-4 pt-3 border-t border-zinc-900/60 w-full text-xs">
              <div>
                <div className="text-[10px] text-zinc-500 font-bold uppercase tracking-wider">Menor Saldo Estimado</div>
                <div className={`font-mono font-bold text-base mt-0.5 ${lowestBalance < 0 ? 'text-red-400' : 'text-emerald-400'}`}>
                  {R(lowestBalance)}
                </div>
              </div>
              <div>
                <div className="text-[10px] text-zinc-500 font-bold uppercase tracking-wider">Saldo Final Estimado</div>
                <div className={`font-mono font-bold text-base mt-0.5 ${endingBalance < 0 ? 'text-red-400' : 'text-teal-400'}`}>
                  {R(endingBalance)}
                </div>
              </div>
              <div>
                <div className="text-[10px] text-zinc-500 font-bold uppercase tracking-wider">Primeiro Risco de Caixa</div>
                <div className="font-bold text-zinc-200 text-base mt-0.5">
                  {simulation?.firstNegativeBalanceDate 
                    ? new Date(simulation.firstNegativeBalanceDate).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) 
                    : 'Sem Risco'}
                </div>
              </div>
            </div>
          ) : undefined
        }
      />

      {/* Credit cards & accounts */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Contas bancárias */}
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-sm font-bold text-zinc-100">Contas Financeiras</h3>
            <Button asChild size="icon" variant="ghost" className="h-6 w-6 text-zinc-500 hover:text-zinc-300">
              <Link href="/finance/accounts"><ArrowRight className="h-4 w-4" /></Link>
            </Button>
          </div>
          <div className="space-y-2.5">
            {accounts.map(acc => (
              <div key={acc.id} className="p-3 bg-zinc-900/30 border border-zinc-850 rounded-xl flex justify-between items-center text-xs">
                <div>
                  <div className="font-bold text-zinc-250">{acc.name}</div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">{ACCOUNT_TYPE_LABELS[acc.accountType] || 'Carteira'}</div>
                </div>
                <span className="font-mono font-bold text-zinc-100">{R(acc.balance)}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Cartões de crédito */}
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-sm font-bold text-zinc-100">Faturas & Cartões</h3>
            <Button asChild size="icon" variant="ghost" className="h-6 w-6 text-zinc-500 hover:text-zinc-300">
              <Link href="/finance/credit-cards"><ArrowRight className="h-4 w-4" /></Link>
            </Button>
          </div>
          <div className="space-y-2.5">
            {nearClosingClosingCards.map(card => (
              <div key={card.id} className="p-3 bg-zinc-900/30 border border-zinc-850 rounded-xl flex justify-between items-center text-xs">
                <div>
                  <div className="font-bold text-zinc-250">{card.name}</div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">
                    Fecha em {card.closingDay} · Vence em {card.dueDay}
                  </div>
                </div>
                <div className="text-right shrink-0">
                  <span className={`px-2 py-0.5 rounded-full text-[9px] font-bold ${
                    card.daysToClosing <= 3 ? 'bg-red-500/10 text-red-400' : 'bg-zinc-800 text-zinc-400'
                  }`}>
                    {card.daysToClosing} dias
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Metas financeiras */}
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-sm font-bold text-zinc-100">Metas Ativas</h3>
            <Button asChild size="icon" variant="ghost" className="h-6 w-6 text-zinc-500 hover:text-zinc-300">
              <Link href="/finance/planner/goals"><ArrowRight className="h-4 w-4" /></Link>
            </Button>
          </div>
          <div className="space-y-3">
            {goals.slice(0, 3).map(goal => {
              const pct = goal.targetAmount > 0 ? (goal.currentAmount / goal.targetAmount) * 100 : 0;
              return (
                <div key={goal.id} className="space-y-1.5 text-xs">
                  <div className="flex justify-between font-medium">
                    <span className="font-bold text-zinc-300">{goal.name}</span>
                    <span className="font-mono font-bold text-zinc-100">{pct.toFixed(0)}%</span>
                  </div>
                  <div className="w-full bg-zinc-900 h-2 rounded-full overflow-hidden">
                    <div className="bg-teal-400 h-full rounded-full" style={{ width: `${pct}%` }} />
                  </div>
                  <div className="flex justify-between text-[10px] text-zinc-500">
                    <span>{R(goal.currentAmount)} acumulado</span>
                    <span>Meta: {R(goal.targetAmount)}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Categories & Transactions row */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Despesas por categoria */}
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Distribuição de Gastos</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Mapeamento de custos agrupados por categoria</p>
          <div className="space-y-3.5">
            {categoryExpenses.slice(0, 5).map((cat, idx) => (
              <div key={cat.name} className="space-y-1 text-xs">
                <div className="flex justify-between">
                  <span className="font-bold text-zinc-300">{cat.name}</span>
                  <span className="font-mono font-bold text-zinc-100">{R(cat.total)} ({cat.pct.toFixed(0)}%)</span>
                </div>
                <div className="w-full bg-zinc-900 h-2 rounded-full overflow-hidden">
                  <div 
                    className="h-full rounded-full" 
                    style={{ 
                      width: `${cat.pct}%`, 
                      backgroundColor: C.expenseColors[idx % C.expenseColors.length] 
                    }} 
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Transações recentes */}
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Transações Recentes</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Últimos lançamentos financeiros processados</p>
            </div>
            <Button asChild size="sm" variant="outline" className="h-8 border-zinc-800 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 font-semibold text-xs">
              <Link href="/finance/transactions">Ver Extrato</Link>
            </Button>
          </div>
          <div className="space-y-2.5">
            {recentTransactions.slice(0, 5).map(t => (
              <div key={t.id} className="p-2.5 bg-zinc-900/20 border border-zinc-850 rounded-lg flex items-center justify-between text-xs">
                <div className="truncate pr-2 max-w-[70%]">
                  <div className="font-bold text-zinc-200 truncate">{t.description}</div>
                  <div className="text-[10px] text-zinc-500 truncate">{t.categoryName || 'Geral'}</div>
                </div>
                <div className="text-right shrink-0">
                  <span className={`font-mono font-bold ${t.type === TransactionType.Income ? 'text-emerald-400' : 'text-zinc-250'}`}>
                    {t.type === TransactionType.Income ? '+' : '-'} {R(Math.abs(t.amount))}
                  </span>
                  <div className="text-[9px] text-zinc-500 font-bold uppercase tracking-wider mt-0.5">
                    {new Date(t.date).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
