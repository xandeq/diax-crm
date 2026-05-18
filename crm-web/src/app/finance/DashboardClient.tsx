'use client';

import { useFinancialSummary, useRecurringTransactions } from '@/hooks/finance';
import { formatCurrency } from '@/lib/utils';
import { FinancialFilters } from '@/services/finance';
import { RecurringTransaction, RecurringItemKind, TransactionType } from '@/types/planner';
import { motion } from 'framer-motion';
import {
    ArrowRight,
    ArrowUpRight,
    BadgeDollarSign,
    BookOpen,
    Building2,
    Calendar,
    CreditCard,
    FileInput,
    LayoutDashboard,
    ReceiptText,
    Repeat2,
    Sun,
    TrendingDown,
    TrendingUp,
    Wallet,
} from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

// ── Period helpers ─────────────────────────────────────────────────────────────

type PeriodKey = 'current' | 'last_month' | 'year';

const PERIOD_LABELS: Record<PeriodKey, string> = {
    current: 'Este Mês',
    last_month: 'Mês Passado',
    year: 'Este Ano',
};

function getPeriodFilters(period: PeriodKey): FinancialFilters {
    const now = new Date();
    if (period === 'last_month') {
        return {
            startDate: new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString(),
            endDate: new Date(now.getFullYear(), now.getMonth(), 0).toISOString(),
        };
    }
    if (period === 'year') {
        return {
            startDate: new Date(now.getFullYear(), 0, 1).toISOString(),
            endDate: new Date(now.getFullYear(), 11, 31).toISOString(),
        };
    }
    return {
        startDate: new Date(now.getFullYear(), now.getMonth(), 1).toISOString(),
        endDate: new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString(),
    };
}

// ── Upcoming recurring ─────────────────────────────────────────────────────────

function sortUpcoming(items: RecurringTransaction[], limit = 6): RecurringTransaction[] {
    const today = new Date().getDate();
    return [...items]
        .filter(r => r.isActive)
        .sort((a, b) => {
            const offset = (d: number) => d >= today ? d : d + 31;
            return offset(a.dayOfMonth) - offset(b.dayOfMonth);
        })
        .slice(0, limit);
}

// ── Isolated primitives ────────────────────────────────────────────────────────

function SkeletonBlock({ className }: { className?: string }) {
    return <div className={`animate-pulse bg-slate-200/70 rounded ${className}`} />;
}

function AnimatedBar({ pct, color }: { pct: number; color: string }) {
    return (
        <div className="h-1 w-full bg-slate-100 rounded-full overflow-hidden">
            <motion.div
                className={`h-full rounded-full ${color}`}
                initial={{ width: 0 }}
                animate={{ width: `${Math.min(pct, 100)}%` }}
                transition={{ duration: 0.9, ease: [0.16, 1, 0.3, 1] }}
            />
        </div>
    );
}

// ── Quick links data ───────────────────────────────────────────────────────────

const QUICK_LINKS = [
    { href: '/finance/morning-briefing', label: 'Morning Briefing', Icon: Sun, bg: 'bg-amber-50', fg: 'text-amber-600' },
    { href: '/finance/personal-control', label: 'Planilha Financeira', Icon: BookOpen, bg: 'bg-emerald-50', fg: 'text-emerald-600' },
    { href: '/finance/transactions', label: 'Transações', Icon: ReceiptText, bg: 'bg-blue-50', fg: 'text-blue-600' },
    { href: '/finance/planner', label: 'Planner', Icon: LayoutDashboard, bg: 'bg-slate-100', fg: 'text-slate-600' },
    { href: '/finance/credit-cards', label: 'Cartões', Icon: CreditCard, bg: 'bg-rose-50', fg: 'text-rose-600' },
    { href: '/finance/accounts', label: 'Contas', Icon: Building2, bg: 'bg-sky-50', fg: 'text-sky-600' },
    { href: '/finance/planner/recurring', label: 'Recorrentes', Icon: Repeat2, bg: 'bg-violet-50', fg: 'text-violet-600' },
    { href: '/finance/imports', label: 'Importar Extrato', Icon: FileInput, bg: 'bg-orange-50', fg: 'text-orange-600' },
] as const;

// ── Fade-up animation factory ──────────────────────────────────────────────────

const fadeUp = (delay = 0) => ({
    initial: { opacity: 0, y: 14 },
    animate: { opacity: 1, y: 0 },
    transition: { duration: 0.42, delay, ease: [0.16, 1, 0.3, 1] as const },
});

// ── Main component ─────────────────────────────────────────────────────────────

export function DashboardClient() {
    const [period, setPeriod] = useState<PeriodKey>('current');
    const filters = useMemo(() => getPeriodFilters(period), [period]);

    const { data: summary, isLoading } = useFinancialSummary(filters);
    const { data: recurring = [] } = useRecurringTransactions();

    const upcoming = useMemo(() => sortUpcoming(recurring), [recurring]);

    const income       = summary?.totalIncome ?? 0;
    const expenses     = summary?.totalExpenses ?? 0;
    const paidExp      = summary?.totalPaidExpenses ?? 0;
    const pendingExp   = summary?.totalPendingExpenses ?? 0;
    const netFlow      = summary?.netCashFlow ?? 0;
    const projFlow     = summary?.projectedCashFlow ?? 0;
    const pendingIn    = summary?.pendingCash ?? 0;
    const maxVal       = Math.max(income, expenses, 1);
    const isPositive   = netFlow >= 0;

    return (
        <div className="min-h-screen bg-[#f8f9fb]">
            <div className="px-5 md:px-8 py-8 max-w-[1400px] mx-auto space-y-6">

                {/* ── Header ── */}
                <motion.div {...fadeUp(0)} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <h1 className="text-[1.65rem] font-bold text-zinc-900 tracking-tight leading-none">Visão Geral</h1>
                        <p className="text-sm text-zinc-400 mt-1.5">Controle financeiro pessoal</p>
                    </div>

                    {/* Segmented period selector */}
                    <div className="flex gap-1 bg-white border border-slate-200 rounded-xl p-1 w-fit shadow-sm">
                        {(['current', 'last_month', 'year'] as PeriodKey[]).map(p => (
                            <button
                                key={p}
                                onClick={() => setPeriod(p)}
                                className={`px-3.5 py-1.5 rounded-lg text-sm font-medium transition-all ${
                                    period === p
                                        ? 'bg-zinc-900 text-white shadow-sm'
                                        : 'text-slate-500 hover:text-slate-800'
                                }`}
                            >
                                {PERIOD_LABELS[p]}
                            </button>
                        ))}
                    </div>
                </motion.div>

                {/* ── KPI strip: 2fr hero | 1fr income | 1fr expenses ── */}
                <motion.div
                    {...fadeUp(0.06)}
                    className="grid grid-cols-1 md:grid-cols-[2fr_1fr_1fr] gap-4"
                >
                    {/* Net Cash Flow — dark hero card */}
                    <div className={`rounded-2xl p-7 flex flex-col justify-between min-h-[180px] ${
                        isPositive
                            ? 'bg-zinc-900'
                            : 'bg-rose-950'
                    }`}>
                        <div className="flex items-center justify-between">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-white/40">
                                Saldo Líquido
                            </p>
                            <div className={`flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full ${
                                isPositive ? 'bg-emerald-500/15 text-emerald-400' : 'bg-rose-400/15 text-rose-300'
                            }`}>
                                {isPositive
                                    ? <TrendingUp className="h-3 w-3" />
                                    : <TrendingDown className="h-3 w-3" />
                                }
                                {isPositive ? 'Positivo' : 'Negativo'}
                            </div>
                        </div>

                        {isLoading ? (
                            <SkeletonBlock className="h-11 w-48 bg-white/10" />
                        ) : (
                            <p className="text-4xl md:text-5xl font-bold text-white tracking-tight">
                                {formatCurrency(netFlow)}
                            </p>
                        )}

                        <div className="flex items-center gap-3 text-xs text-white/30">
                            <Calendar className="h-3.5 w-3.5" />
                            <span>{PERIOD_LABELS[period]}</span>
                        </div>
                    </div>

                    {/* Income */}
                    <div className="bg-white border border-slate-200/80 rounded-2xl p-6 flex flex-col justify-between">
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">Entradas</p>
                            <div className="p-1.5 bg-emerald-50 rounded-lg">
                                <TrendingUp className="h-3.5 w-3.5 text-emerald-600" />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold text-zinc-900 tracking-tight">{formatCurrency(income)}</p>
                                <AnimatedBar pct={(income / maxVal) * 100} color="bg-emerald-500" />
                                <p className="text-xs text-slate-400">receitas do período</p>
                            </div>
                        )}
                    </div>

                    {/* Expenses */}
                    <div className="bg-white border border-slate-200/80 rounded-2xl p-6 flex flex-col justify-between">
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em] text-slate-400">Saídas</p>
                            <div className="p-1.5 bg-rose-50 rounded-lg">
                                <TrendingDown className="h-3.5 w-3.5 text-rose-600" />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold text-zinc-900 tracking-tight">{formatCurrency(expenses)}</p>
                                <AnimatedBar pct={(expenses / maxVal) * 100} color="bg-rose-500" />
                                <div className="flex gap-3 text-xs">
                                    <span className="text-emerald-600 font-medium">Pago {formatCurrency(paidExp)}</span>
                                    <span className="text-amber-600 font-medium">Pendente {formatCurrency(pendingExp)}</span>
                                </div>
                            </div>
                        )}
                    </div>
                </motion.div>

                {/* ── Secondary row: Upcoming recurring (3fr) + Projections (2fr) ── */}
                <motion.div
                    {...fadeUp(0.12)}
                    className="grid grid-cols-1 lg:grid-cols-[3fr_2fr] gap-4"
                >
                    {/* Upcoming recurring payments */}
                    <div className="bg-white border border-slate-200/80 rounded-2xl overflow-hidden">
                        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <Repeat2 className="h-4 w-4 text-slate-400" />
                                <h2 className="text-sm font-semibold text-zinc-800">Próximos Recorrentes</h2>
                            </div>
                            <Link
                                href="/finance/planner/recurring"
                                className="flex items-center gap-1 text-xs text-slate-400 hover:text-slate-700 transition-colors"
                            >
                                Ver todos <ArrowRight className="h-3 w-3" />
                            </Link>
                        </div>

                        {upcoming.length === 0 ? (
                            <div className="py-14 flex flex-col items-center gap-3">
                                <div className="p-3 bg-slate-50 rounded-2xl">
                                    <Repeat2 className="h-6 w-6 text-slate-300" />
                                </div>
                                <p className="text-sm text-slate-400">Nenhum item recorrente ativo</p>
                                <Link
                                    href="/finance/planner/recurring"
                                    className="text-xs font-medium text-blue-600 hover:text-blue-700 flex items-center gap-1"
                                >
                                    Cadastrar <ArrowUpRight className="h-3 w-3" />
                                </Link>
                            </div>
                        ) : (
                            <div className="divide-y divide-slate-100">
                                {upcoming.map((r, i) => {
                                    const isIncome = r.type === TransactionType.Income;
                                    return (
                                        <motion.div
                                            key={r.id}
                                            initial={{ opacity: 0, x: -8 }}
                                            animate={{ opacity: 1, x: 0 }}
                                            transition={{ delay: 0.18 + i * 0.05, duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
                                            className="flex items-center gap-4 px-6 py-3.5 hover:bg-slate-50/60 transition-colors"
                                        >
                                            {/* Day badge */}
                                            <div className={`w-9 h-9 rounded-xl flex items-center justify-center text-xs font-bold flex-shrink-0 ${
                                                isIncome ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700'
                                            }`}>
                                                {r.dayOfMonth}
                                            </div>

                                            <div className="min-w-0 flex-1">
                                                <p className="text-sm font-medium text-zinc-800 truncate">{r.description}</p>
                                                <div className="flex items-center gap-2 mt-0.5">
                                                    <span className={`text-[10px] font-semibold px-1.5 py-0.5 rounded-full ${
                                                        r.itemKind === RecurringItemKind.Subscription
                                                            ? 'bg-violet-50 text-violet-600'
                                                            : 'bg-slate-100 text-slate-500'
                                                    }`}>
                                                        {r.itemKind === RecurringItemKind.Subscription ? 'Assinatura' : 'Padrão'}
                                                    </span>
                                                    {r.hasVariableAmount && (
                                                        <span className="text-[10px] text-amber-600 font-medium">Valor variável</span>
                                                    )}
                                                </div>
                                            </div>

                                            <div className="flex flex-col items-end flex-shrink-0">
                                                <span className={`text-sm font-bold ${isIncome ? 'text-emerald-600' : 'text-zinc-800'}`}>
                                                    {isIncome ? '+' : '-'}{formatCurrency(r.amount)}
                                                </span>
                                            </div>
                                        </motion.div>
                                    );
                                })}
                            </div>
                        )}
                    </div>

                    {/* Projections panel — dark */}
                    <div className="bg-zinc-950 rounded-2xl p-7 flex flex-col justify-between">
                        <div>
                            <div className="flex items-center gap-2 mb-8">
                                <Wallet className="h-4 w-4 text-zinc-500" />
                                <p className="text-xs font-semibold uppercase tracking-[0.12em] text-zinc-500">Projeção</p>
                            </div>

                            <div className="space-y-6">
                                <div>
                                    <p className="text-[11px] text-zinc-600 uppercase tracking-wider mb-1.5">A Receber</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36 bg-white/5" />
                                        : <p className="text-xl font-bold text-white">{formatCurrency(pendingIn)}</p>
                                    }
                                </div>

                                <div>
                                    <p className="text-[11px] text-zinc-600 uppercase tracking-wider mb-1.5">A Pagar</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36 bg-white/5" />
                                        : <p className="text-xl font-bold text-white">{formatCurrency(pendingExp)}</p>
                                    }
                                </div>
                            </div>
                        </div>

                        <div className="pt-6 border-t border-zinc-800/60 mt-6">
                            <p className="text-[11px] text-zinc-500 uppercase tracking-wider mb-2">Saldo Projetado</p>
                            {isLoading ? (
                                <SkeletonBlock className="h-10 w-44 bg-white/5" />
                            ) : (
                                <p className={`text-3xl font-bold tracking-tight ${
                                    projFlow >= 0 ? 'text-emerald-400' : 'text-rose-400'
                                }`}>
                                    {formatCurrency(projFlow)}
                                </p>
                            )}
                            <p className="text-[11px] text-zinc-600 mt-1.5">após todos os pagamentos</p>
                        </div>
                    </div>
                </motion.div>

                {/* ── Quick access: horizontal scroll strip ── */}
                <motion.div {...fadeUp(0.18)}>
                    <p className="text-[11px] font-semibold text-slate-400 uppercase tracking-[0.12em] mb-3">Acesso Rápido</p>
                    <div className="flex gap-2.5 overflow-x-auto pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
                        {QUICK_LINKS.map(({ href, label, Icon, bg, fg }) => (
                            <Link
                                key={href}
                                href={href}
                                className="flex-shrink-0 flex items-center gap-2.5 px-4 py-2.5 bg-white border border-slate-200 rounded-xl hover:border-slate-300 hover:shadow-sm active:scale-[0.98] transition-all duration-150"
                            >
                                <div className={`p-1.5 rounded-lg ${bg}`}>
                                    <Icon className={`h-3.5 w-3.5 ${fg}`} />
                                </div>
                                <span className="text-sm font-medium text-zinc-700 whitespace-nowrap">{label}</span>
                            </Link>
                        ))}
                    </div>
                </motion.div>

            </div>
        </div>
    );
}
