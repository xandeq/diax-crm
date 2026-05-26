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
        <div className="min-h-screen">
            <div className="px-5 md:px-8 py-8 max-w-[1400px] mx-auto space-y-6">

                {/* ── Header ── */}
                <motion.div {...fadeUp(0)} className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <h1 className="text-[1.65rem] font-bold tracking-tight leading-none" style={{ color: '#F9FAFB' }}>Visão Geral</h1>
                        <p className="text-sm mt-1.5" style={{ color: '#9CA3AF' }}>Controle financeiro pessoal</p>
                    </div>

                    {/* Segmented period selector */}
                    <div className="flex gap-1 rounded-xl p-1 w-fit" style={{ background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.09)' }}>
                        {(['current', 'last_month', 'year'] as PeriodKey[]).map(p => (
                            <button
                                key={p}
                                onClick={() => setPeriod(p)}
                                className="px-3.5 py-1.5 rounded-lg text-sm font-medium transition-all"
                                style={period === p
                                    ? { background: 'rgba(16,185,129,0.2)', color: '#34d399' }
                                    : { color: '#9CA3AF' }
                                }
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
                        className="rounded-2xl p-7 flex flex-col justify-between min-h-[180px]"
                        style={{
                            background: isPositive
                                ? 'linear-gradient(135deg, rgba(16,185,129,0.18) 0%, rgba(5,150,105,0.10) 100%)'
                                : 'linear-gradient(135deg, rgba(239,68,68,0.18) 0%, rgba(185,28,28,0.10) 100%)',
                            border: `1px solid ${isPositive ? 'rgba(16,185,129,0.25)' : 'rgba(239,68,68,0.25)'}`,
                        }}
                    >
                        <div className="flex items-center justify-between">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em]" style={{ color: 'rgba(255,255,255,0.4)' }}>
                                Saldo Líquido
                            </p>
                            <div className="flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full" style={{
                                background: isPositive ? 'rgba(16,185,129,0.15)' : 'rgba(239,68,68,0.15)',
                                color: isPositive ? '#34d399' : '#f87171',
                            }}>
                                {isPositive
                                    ? <TrendingUp className="h-3 w-3" />
                                    : <TrendingDown className="h-3 w-3" />
                                }
                                {isPositive ? 'Positivo' : 'Negativo'}
                            </div>
                        </div>

                        {isLoading ? (
                            <SkeletonBlock className="h-11 w-48" />
                        ) : (
                            <p className="text-4xl md:text-5xl font-bold tracking-tight" style={{ color: '#F9FAFB' }}>
                                {formatCurrency(netFlow)}
                            </p>
                        )}

                        <div className="flex items-center gap-3 text-xs" style={{ color: 'rgba(255,255,255,0.3)' }}>
                            <Calendar className="h-3.5 w-3.5" />
                            <span>{PERIOD_LABELS[period]}</span>
                        </div>
                    </div>

                    {/* Income */}
                    <div className="rounded-2xl p-6 flex flex-col justify-between" style={CARD}>
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em]" style={{ color: '#9CA3AF' }}>Entradas</p>
                            <div className="p-1.5 rounded-lg" style={{ background: 'rgba(16,185,129,0.12)' }}>
                                <TrendingUp className="h-3.5 w-3.5" style={{ color: '#34d399' }} />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold tracking-tight" style={{ color: '#F9FAFB' }}>{formatCurrency(income)}</p>
                                <AnimatedBar pct={(income / maxVal) * 100} color="bg-emerald-500" />
                                <p className="text-xs" style={{ color: '#9CA3AF' }}>receitas do período</p>
                            </div>
                        )}
                    </div>

                    {/* Expenses */}
                    <div className="rounded-2xl p-6 flex flex-col justify-between" style={CARD}>
                        <div className="flex items-center justify-between mb-4">
                            <p className="text-[11px] font-semibold uppercase tracking-[0.12em]" style={{ color: '#9CA3AF' }}>Saídas</p>
                            <div className="p-1.5 rounded-lg" style={{ background: 'rgba(244,63,94,0.12)' }}>
                                <TrendingDown className="h-3.5 w-3.5" style={{ color: '#fb7185' }} />
                            </div>
                        </div>

                        {isLoading ? (
                            <div className="space-y-3">
                                <SkeletonBlock className="h-7 w-32" />
                                <SkeletonBlock className="h-1 w-full" />
                            </div>
                        ) : (
                            <div className="space-y-3">
                                <p className="text-2xl font-bold tracking-tight" style={{ color: '#F9FAFB' }}>{formatCurrency(expenses)}</p>
                                <AnimatedBar pct={(expenses / maxVal) * 100} color="bg-rose-500" />
                                <div className="flex gap-3 text-xs">
                                    <span className="font-medium" style={{ color: '#34d399' }}>Pago {formatCurrency(paidExp)}</span>
                                    <span className="font-medium" style={{ color: '#fbbf24' }}>Pendente {formatCurrency(pendingExp)}</span>
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
                    <div className="rounded-2xl overflow-hidden" style={CARD}>
                        <div className="px-6 py-4 flex items-center justify-between" style={{ borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
                            <div className="flex items-center gap-2">
                                <Repeat2 className="h-4 w-4" style={{ color: '#6B7280' }} />
                                <h2 className="text-sm font-semibold" style={{ color: '#D1D5DB' }}>Próximos Recorrentes</h2>
                            </div>
                            <Link
                                href="/finance/planner/recurring"
                                className="flex items-center gap-1 text-xs transition-colors"
                                style={{ color: '#6B7280' }}
                            >
                                Ver todos <ArrowRight className="h-3 w-3" />
                            </Link>
                        </div>

                        {upcoming.length === 0 ? (
                            <div className="py-14 flex flex-col items-center gap-3">
                                <div className="p-3 rounded-2xl" style={{ background: 'rgba(255,255,255,0.06)' }}>
                                    <Repeat2 className="h-6 w-6" style={{ color: '#4B5563' }} />
                                </div>
                                <p className="text-sm" style={{ color: '#9CA3AF' }}>Nenhum item recorrente ativo</p>
                                <Link
                                    href="/finance/planner/recurring"
                                    className="text-xs font-medium flex items-center gap-1"
                                    style={{ color: '#60a5fa' }}
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
                                            className="flex items-center gap-4 px-6 py-3.5 transition-colors"
                                            style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}
                                        >
                                            {/* Day badge */}
                                            <div
                                                className="w-9 h-9 rounded-xl flex items-center justify-center text-xs font-bold flex-shrink-0"
                                                style={{
                                                    background: isIncome ? 'rgba(16,185,129,0.12)' : 'rgba(244,63,94,0.12)',
                                                    color: isIncome ? '#34d399' : '#fb7185',
                                                }}
                                            >
                                                {r.dayOfMonth}
                                            </div>

                                            <div className="min-w-0 flex-1">
                                                <p className="text-sm font-medium truncate" style={{ color: '#D1D5DB' }}>{r.description}</p>
                                                <div className="flex items-center gap-2 mt-0.5">
                                                    <span
                                                        className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full"
                                                        style={r.itemKind === RecurringItemKind.Subscription
                                                            ? { background: 'rgba(139,92,246,0.12)', color: '#a78bfa' }
                                                            : { background: 'rgba(255,255,255,0.08)', color: '#9CA3AF' }
                                                        }
                                                    >
                                                        {r.itemKind === RecurringItemKind.Subscription ? 'Assinatura' : 'Padrão'}
                                                    </span>
                                                    {r.hasVariableAmount && (
                                                        <span className="text-[10px] font-medium" style={{ color: '#fbbf24' }}>Valor variável</span>
                                                    )}
                                                </div>
                                            </div>

                                            <div className="flex flex-col items-end flex-shrink-0">
                                                <span className="text-sm font-bold" style={{ color: isIncome ? '#34d399' : '#D1D5DB' }}>
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
                    <div className="rounded-2xl p-7 flex flex-col justify-between" style={{ background: 'rgba(5,15,10,0.8)', border: '1px solid rgba(255,255,255,0.07)' }}>
                        <div>
                            <div className="flex items-center gap-2 mb-8">
                                <Wallet className="h-4 w-4" style={{ color: '#4B5563' }} />
                                <p className="text-xs font-semibold uppercase tracking-[0.12em]" style={{ color: '#4B5563' }}>Projeção</p>
                            </div>

                            <div className="space-y-6">
                                <div>
                                    <p className="text-[11px] uppercase tracking-wider mb-1.5" style={{ color: '#4B5563' }}>A Receber</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36" />
                                        : <p className="text-xl font-bold" style={{ color: '#F9FAFB' }}>{formatCurrency(pendingIn)}</p>
                                    }
                                </div>

                                <div>
                                    <p className="text-[11px] uppercase tracking-wider mb-1.5" style={{ color: '#4B5563' }}>A Pagar</p>
                                    {isLoading
                                        ? <SkeletonBlock className="h-7 w-36" />
                                        : <p className="text-xl font-bold" style={{ color: '#F9FAFB' }}>{formatCurrency(pendingExp)}</p>
                                    }
                                </div>
                            </div>
                        </div>

                        <div className="pt-6 mt-6" style={{ borderTop: '1px solid rgba(255,255,255,0.08)' }}>
                            <p className="text-[11px] uppercase tracking-wider mb-2" style={{ color: '#4B5563' }}>Saldo Projetado</p>
                            {isLoading ? (
                                <SkeletonBlock className="h-10 w-44" />
                            ) : (
                                <p className={`text-3xl font-bold tracking-tight`} style={{ color: projFlow >= 0 ? '#34d399' : '#f87171' }}>
                                    {formatCurrency(projFlow)}
                                </p>
                            )}
                            <p className="text-[11px] mt-1.5" style={{ color: '#4B5563' }}>após todos os pagamentos</p>
                        </div>
                    </div>
                </motion.div>

                {/* ── Quick access: horizontal scroll strip ── */}
                <motion.div {...fadeUp(0.18)}>
                    <p className="text-[11px] font-semibold uppercase tracking-[0.12em] mb-3" style={{ color: '#6B7280' }}>Acesso Rápido</p>
                    <div className="flex gap-2.5 overflow-x-auto pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
                        {QUICK_LINKS.map(({ href, label, Icon, tint, fg }) => (
                            <Link
                                key={href}
                                href={href}
                                className="flex-shrink-0 flex items-center gap-2.5 px-4 py-2.5 rounded-xl active:scale-[0.98] transition-all duration-150"
                                style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.09)' }}
                            >
                                <div className="p-1.5 rounded-lg" style={{ background: tint }}>
                                    <Icon className="h-3.5 w-3.5" style={{ color: fg }} />
                                </div>
                                <span className="text-sm font-medium whitespace-nowrap" style={{ color: '#D1D5DB' }}>{label}</span>
                            </Link>
                        ))}
                    </div>
                </motion.div>

            </div>
        </div>
    );
}
