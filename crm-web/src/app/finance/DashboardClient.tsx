'use client';

import { useFinancialSummary, useRecurringTransactions } from '@/hooks/finance';
import { cn, formatCurrency } from '@/lib/utils';
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
import { StatusBadge } from '@/components/dashboard/StatusBadge';

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
    return <div className={`animate-pulse rounded ${className}`} style={{ background: 'rgba(255,255,255,0.08)' }} />;
}

function AnimatedBar({ pct, color }: { pct: number; color: string }) {
    return (
        <div className="h-1 w-full rounded-full overflow-hidden" style={{ background: 'rgba(255,255,255,0.08)' }}>
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
    { href: '/finance/morning-briefing', label: 'Morning Briefing',   Icon: Sun,           tint: 'rgba(245,158,11,0.12)',  fg: '#fbbf24' },
    { href: '/finance/personal-control', label: 'Planilha Financeira',Icon: BookOpen,       tint: 'rgba(16,185,129,0.12)', fg: '#34d399' },
    { href: '/finance/transactions',     label: 'Transações',          Icon: ReceiptText,   tint: 'rgba(59,130,246,0.12)', fg: '#60a5fa' },
    { href: '/finance/planner',          label: 'Planner',             Icon: LayoutDashboard,tint:'rgba(148,163,184,0.12)',fg: '#94a3b8' },
    { href: '/finance/credit-cards',     label: 'Cartões',             Icon: CreditCard,    tint: 'rgba(244,63,94,0.12)',  fg: '#fb7185' },
    { href: '/finance/accounts',         label: 'Contas',              Icon: Building2,     tint: 'rgba(14,165,233,0.12)', fg: '#38bdf8' },
    { href: '/finance/planner/recurring',label: 'Recorrentes',         Icon: Repeat2,       tint: 'rgba(139,92,246,0.12)', fg: '#a78bfa' },
    { href: '/finance/imports',          label: 'Importar Extrato',    Icon: FileInput,     tint: 'rgba(249,115,22,0.12)', fg: '#fb923c' },
] as const;

// ── Fade-up animation factory ──────────────────────────────────────────────────

const fadeUp = (delay = 0) => ({
    initial: { opacity: 0, y: 14 },
    animate: { opacity: 1, y: 0 },
    transition: { duration: 0.42, delay, ease: [0.16, 1, 0.3, 1] as const },
});

// ── Shared card style ──────────────────────────────────────────────────────────

const CARD = {
    background: 'rgba(255,255,255,0.04)',
    border: '1px solid rgba(255,255,255,0.09)',
    borderRadius: '1rem',
} as const;

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
        <div className="min-h-screen bg-transparent">
            <div className="px-5 md:px-8 py-8 max-w-[1400px] mx-auto space-y-6">

                {/* ── Header ── */}
                <motion.div {...fadeUp(0)} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <h1 className="text-[1.65rem] font-bold tracking-tight leading-none text-zinc-100">Visão Geral</h1>
                        <p className="text-sm mt-1.5 text-zinc-400">Controle financeiro pessoal</p>
                    </div>

                    {/* Segmented period selector */}
                    <div className="flex gap-1 rounded-xl p-1 w-fit bg-white/[0.04] border border-white/5">
                        {(['current', 'last_month', 'year'] as PeriodKey[]).map(p => (
                            <button
                                key={p}
                                onClick={() => setPeriod(p)}
                                className={cn(
                                    "px-3.5 py-1.5 rounded-lg text-sm font-medium transition-all duration-200",
                                    period === p
                                        ? "bg-emerald-500/15 border border-emerald-500/25 text-[#00D4AA] shadow-sm shadow-emerald-500/5"
                                        : "text-zinc-400 hover:text-zinc-200 hover:bg-white/5 border border-transparent"
                                )}
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
                    {/* Net Cash Flow — hero card */}
                    <div
                        className={cn(
                            "rounded-2xl p-7 flex flex-col justify-between min-h-[180px] border transition-all duration-300 shadow-md hover:shadow-lg",
                            isPositive
                                ? "bg-gradient-to-br from-emerald-500/10 to-emerald-600/[0.02] border-emerald-500/20 hover:border-emerald-500/30"
                                : "bg-gradient-to-br from-red-500/10 to-red-600/[0.02] border-red-500/20 hover:border-red-500/30"
                        )}
                    >
                        <div className="flex items-center justify-between">
                            <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-zinc-400">
                                Saldo Líquido
                            </p>
                            <StatusBadge
                                variant={isPositive ? "success" : "destructive"}
                                trending={isPositive ? "up" : "down"}
                                pulse
                            >
                                {isPositive ? 'Positivo' : 'Negativo'}
                            </StatusBadge>
                        </div>

                        {isLoading ? (
                            <SkeletonBlock className="h-11 w-48" />
                        ) : (
                            <p className="text-4xl md:text-5xl font-bold tracking-tight text-zinc-100">
                                {formatCurrency(netFlow)}
                            </p>
                        )}

                        <div className="flex items-center gap-3 text-xs text-zinc-400">
                            <Calendar className="h-3.5 w-3.5 text-zinc-400" />
                            <span>{PERIOD_LABELS[period]}</span>
                        </div>
                    </div>

                    {/* Income */}
                    <div className="rounded-2xl p-6 flex flex-col justify-between border border-white/5 bg-white/[0.03] hover:border-white/10 hover:bg-white/[0.05] transition-all duration-300 shadow-md hover:shadow-lg">
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-zinc-400">Entradas</p>
                            <div className="p-1.5 rounded-lg bg-emerald-500/10">
                                <TrendingUp className="h-3.5 w-3.5 text-emerald-400" />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold tracking-tight text-zinc-100">{formatCurrency(income)}</p>
                                <AnimatedBar pct={(income / maxVal) * 100} color="bg-emerald-500" />
                                <p className="text-xs text-zinc-400">receitas do período</p>
                            </div>
                        )}
                    </div>

                    {/* Expenses */}
                    <div className="rounded-2xl p-6 flex flex-col justify-between border border-white/5 bg-white/[0.03] hover:border-white/10 hover:bg-white/[0.05] transition-all duration-300 shadow-md hover:shadow-lg">
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-zinc-400">Saídas</p>
                            <div className="p-1.5 rounded-lg bg-rose-500/10">
                                <TrendingDown className="h-3.5 w-3.5 text-rose-400" />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold tracking-tight text-zinc-100">{formatCurrency(expenses)}</p>
                                <AnimatedBar pct={(expenses / maxVal) * 100} color="bg-rose-500" />
                                <div className="flex gap-3 text-xs">
                                    <span className="font-semibold text-emerald-400">Pago {formatCurrency(paidExp)}</span>
                                    <span className="font-semibold text-amber-400">Pendente {formatCurrency(pendingExp)}</span>
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
                    <div className="rounded-2xl overflow-hidden border border-white/5 bg-white/[0.03] hover:border-white/10 transition-all duration-300">
                        <div className="px-6 py-4 flex items-center justify-between border-b border-white/5">
                            <div className="flex items-center gap-2">
                                <Repeat2 className="h-4 w-4 text-zinc-400" />
                                <h2 className="text-sm font-semibold text-zinc-100">Próximos Recorrentes</h2>
                            </div>
                            <Link
                                href="/finance/planner/recurring"
                                className="flex items-center gap-1 text-xs text-zinc-400 hover:text-zinc-200 transition-colors"
                            >
                                Ver todos <ArrowRight className="h-3 w-3" />
                            </Link>
                        </div>

                        {upcoming.length === 0 ? (
                            <div className="py-14 flex flex-col items-center gap-3">
                                <div className="p-3 rounded-2xl bg-white/[0.04]">
                                    <Repeat2 className="h-6 w-6 text-zinc-500" />
                                </div>
                                <p className="text-sm text-zinc-400">Nenhum item recorrente ativo</p>
                                <Link
                                    href="/finance/planner/recurring"
                                    className="text-xs font-semibold flex items-center gap-1 text-[#00D4AA] hover:text-[#00d4aa]/80 transition-colors"
                                >
                                    Cadastrar <ArrowUpRight className="h-3 w-3" />
                                </Link>
                            </div>
                        ) : (
                            <div>
                                {upcoming.map((r, i) => {
                                    const isIncome = r.type === TransactionType.Income;
                                    return (
                                        <motion.div
                                            key={r.id}
                                            initial={{ opacity: 0, x: -8 }}
                                            animate={{ opacity: 1, x: 0 }}
                                            transition={{ delay: 0.18 + i * 0.05, duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
                                            className="flex items-center gap-4 px-6 py-3.5 hover:bg-white/[0.02] border-b border-white/[0.03] last:border-0 transition-colors"
                                        >
                                            {/* Day badge */}
                                            <div
                                                className={cn(
                                                    "w-9 h-9 rounded-xl flex items-center justify-center text-xs font-bold flex-shrink-0 border",
                                                    isIncome
                                                        ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20"
                                                        : "bg-rose-500/10 text-rose-400 border-rose-500/20"
                                                )}
                                            >
                                                {r.dayOfMonth}
                                            </div>

                                            <div className="min-w-0 flex-1">
                                                <p className="text-sm font-semibold truncate text-zinc-200">{r.description}</p>
                                                <div className="flex items-center gap-2 mt-0.5">
                                                    <span
                                                        className={cn(
                                                            "text-[10px] font-bold px-2 py-0.5 rounded-full border",
                                                            r.itemKind === RecurringItemKind.Subscription
                                                                ? "bg-purple-500/10 text-purple-400 border-purple-500/20"
                                                                : "bg-zinc-800/50 text-zinc-400 border-zinc-700/50"
                                                        )}
                                                    >
                                                        {r.itemKind === RecurringItemKind.Subscription ? 'Assinatura' : 'Padrão'}
                                                    </span>
                                                    {r.hasVariableAmount && (
                                                        <span className="text-[10px] font-semibold text-amber-400">Valor variável</span>
                                                    )}
                                                </div>
                                            </div>

                                            <div className="flex flex-col items-end flex-shrink-0">
                                                <span className={cn("text-sm font-bold", isIncome ? "text-[#00D4AA]" : "text-zinc-200")}>
                                                    {isIncome ? '+' : '-'}{formatCurrency(r.amount)}
                                                </span>
                                            </div>
                                        </motion.div>
                                    );
                                })}
                            </div>
                        )}
                    </div>

                    {/* Projections panel */}
                    <div className="rounded-2xl p-7 flex flex-col justify-between border border-emerald-500/10 bg-emerald-950/[0.04] hover:border-emerald-500/20 transition-all duration-300">
                        <div>
                            <div className="flex items-center gap-2 mb-8">
                                <Wallet className="h-4 w-4 text-zinc-400" />
                                <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-zinc-400">Projeção</p>
                            </div>

                            <div className="space-y-6 border-l border-emerald-500/10 pl-4">
                                <div>
                                    <p className="text-[10px] uppercase tracking-wider mb-1 text-zinc-400">A Receber</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36" />
                                        : <p className="text-xl font-bold text-zinc-200">{formatCurrency(pendingIn)}</p>
                                    }
                                </div>

                                <div>
                                    <p className="text-[10px] uppercase tracking-wider mb-1 text-zinc-400">A Pagar</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36" />
                                        : <p className="text-xl font-bold text-zinc-200">{formatCurrency(pendingExp)}</p>
                                    }
                                </div>
                            </div>
                        </div>

                        <div className="pt-6 mt-6 border-t border-white/5">
                            <p className="text-[10px] uppercase tracking-wider mb-1.5 text-zinc-400">Saldo Projetado</p>
                            {isLoading ? (
                                <SkeletonBlock className="h-10 w-44" />
                            ) : (
                                <p className={cn("text-3xl font-extrabold tracking-tight", projFlow >= 0 ? "text-[#00D4AA]" : "text-rose-400")}>
                                    {formatCurrency(projFlow)}
                                </p>
                            )}
                            <p className="text-[10px] mt-1 text-zinc-400">após todos os pagamentos</p>
                        </div>
                    </div>
                </motion.div>

                {/* ── Quick access: horizontal scroll strip ── */}
                <motion.div {...fadeUp(0.18)}>
                    <p className="text-[10px] font-semibold uppercase tracking-[0.15em] mb-3 text-zinc-400">Acesso Rápido</p>
                    <div className="flex gap-2.5 overflow-x-auto pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
                        {QUICK_LINKS.map(({ href, label, Icon, tint, fg }) => (
                            <Link
                                key={href}
                                href={href}
                                className="flex-shrink-0 flex items-center gap-2.5 px-4 py-2.5 rounded-xl border border-white/5 bg-white/[0.03] hover:border-white/10 hover:bg-white/[0.06] active:scale-[0.98] transition-all duration-150"
                            >
                                <div className="p-1.5 rounded-lg" style={{ background: tint }}>
                                    <Icon className="h-3.5 w-3.5" style={{ color: fg }} />
                                </div>
                                <span className="text-sm font-semibold whitespace-nowrap text-zinc-200">{label}</span>
                            </Link>
                        ))}
                    </div>
                </motion.div>

            </div>
        </div>
    );
}
