'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useMarkExpensePaid, useMorningBriefing } from '@/hooks/finance';
import { FinanceNav } from '@/components/finance/FinanceNav';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import {
    AlertTriangle,
    CheckCircle2,
    CheckSquare,
    Clock,
    Loader2,
    RefreshCw,
    TrendingUp,
    Wallet,
    Calendar,
    ChevronRight,
    ArrowRight
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import { motion, AnimatePresence } from 'framer-motion';

const BRL = (v: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v);

const MONTH_NAMES = ['', 'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];

export default function MorningBriefingClient() {
    const { data, isLoading, error, refetch, isFetching } = useMorningBriefing();
    const markPaid = useMarkExpensePaid();

    const handleMarkPaid = (id: string) => {
        markPaid.mutate(
            { id, paymentDate: new Date().toISOString().split('T')[0] },
            {
                onError: () => toast.error('Erro ao marcar como pago. Tente novamente.'),
                onSuccess: () => toast.success('Marcado como pago'),
            },
        );
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <FinanceNav />
                <div className="flex flex-col items-center justify-center min-h-[350px] gap-3">
                    <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
                    <p className="text-zinc-400 text-sm animate-pulse">Compilando seu resumo financeiro...</p>
                </div>
            </div>
        );
    }

    if (error || !data) {
        return (
            <div className="space-y-6">
                <FinanceNav />
                <div className="flex flex-col items-center justify-center min-h-[300px] gap-4 p-8 border border-zinc-800 border-dashed rounded-2xl bg-zinc-900/10">
                    <AlertTriangle className="h-10 w-10 text-rose-400 animate-bounce" />
                    <p className="text-zinc-400 text-sm text-center">Não foi possível carregar o briefing matinal.</p>
                    <Button 
                      variant="outline" 
                      size="sm" 
                      onClick={() => refetch()}
                      className="border-zinc-800 text-zinc-300 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
                    >
                        <RefreshCw className="h-4 w-4 mr-2" />Tentar novamente
                    </Button>
                </div>
            </div>
        );
    }

    const now = new Date(data.generatedAt);
    const monthName = MONTH_NAMES[data.period.month];
    const { summary, alerts } = data;
    const payingId = markPaid.variables?.id;
    const isPaying = markPaid.isPending;

    return (
        <div className="space-y-6">
            <FinanceNav />

            {/* Header */}
            <div className="flex items-center justify-between pb-2">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-zinc-100">Morning Briefing</h1>
                    <p className="text-zinc-400 text-xs mt-1">
                        {format(now, "EEEE, d 'de' MMMM 'de' yyyy", { locale: ptBR })} — {monthName} de {data.period.year}
                    </p>
                </div>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => refetch()}
                    disabled={isFetching}
                    className="rounded-full text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50"
                    title="Atualizar Briefing"
                >
                    <RefreshCw className={`h-4.5 w-4.5 ${isFetching ? 'animate-spin' : ''}`} />
                </Button>
            </div>

            {/* Urgent banner */}
            {alerts.hasUrgentItems && (
                <motion.div 
                    initial={{ opacity: 0, scale: 0.98 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="rounded-2xl border border-rose-500/20 bg-rose-500/5 p-4 flex items-start gap-3.5 relative overflow-hidden shadow-lg shadow-rose-950/5"
                >
                    <div className="absolute inset-0 bg-gradient-to-r from-rose-500/5 to-transparent pointer-events-none" />
                    <div className="p-2 rounded-xl bg-rose-500/10 border border-rose-500/20 shrink-0">
                      <AlertTriangle className="h-5 w-5 text-rose-400" />
                    </div>
                    <div>
                        <p className="font-bold text-rose-400 text-sm">Ação necessária pendente</p>
                        <p className="text-xs text-zinc-300 mt-1 leading-relaxed">
                            {alerts.overdueCount > 0 && (
                              <span>
                                Existem <strong>{alerts.overdueCount} contas em atraso</strong> totalizando <strong className="text-rose-400 font-bold">{BRL(alerts.overdueAmount)}</strong>.{' '}
                              </span>
                            )}
                            {alerts.dueTodayCount > 0 && (
                              <span>
                                E <strong>{alerts.dueTodayCount} contas que vencem hoje</strong> no total de <strong className="text-amber-400 font-bold">{BRL(alerts.dueTodayAmount)}</strong>.
                              </span>
                            )}
                        </p>
                    </div>
                </motion.div>
            )}

            {/* Summary cards */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0 }}
                  className="rounded-2xl p-4 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
                >
                  <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                  <p className="text-[10px] font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-1.5">
                    <TrendingUp className="h-3.5 w-3.5 text-emerald-400" />Receitas
                  </p>
                  <p className="mt-2 text-lg font-extrabold text-emerald-400 tabular-nums leading-tight">{BRL(summary.totalIncome)}</p>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.05 }}
                  className="rounded-2xl p-4 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
                >
                  <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                  <p className="text-[10px] font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-1.5">
                    <CheckCircle2 className="h-3.5 w-3.5 text-emerald-400" />Contas Pagas
                  </p>
                  <p className="mt-2 text-lg font-extrabold text-zinc-100 tabular-nums leading-tight">{BRL(summary.totalPaid)}</p>
                  <p className="text-[10px] text-zinc-500 mt-1">{summary.paidCount} transações</p>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.1 }}
                  className="rounded-2xl p-4 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
                >
                  <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                  <p className="text-[10px] font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-1.5">
                    <Clock className="h-3.5 w-3.5 text-amber-400" />Pendente
                  </p>
                  <p className="mt-2 text-lg font-extrabold text-amber-400 tabular-nums leading-tight">{BRL(summary.totalPending)}</p>
                  <p className="text-[10px] text-zinc-500 mt-1">{summary.unpaidCount} transações</p>
                </motion.div>

                <motion.div
                  initial={{ opacity: 0, y: 10 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: 0.15 }}
                  className="rounded-2xl p-4 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
                >
                  <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                  <p className="text-[10px] font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-1.5">
                    <Wallet className="h-3.5 w-3.5 text-emerald-400" />Disponível
                  </p>
                  <p className={cn(
                    "mt-2 text-lg font-extrabold tabular-nums leading-tight",
                    summary.availableToInvest >= 0 ? 'text-[#00D4AA]' : 'text-rose-400'
                  )}>
                    {BRL(summary.availableToInvest)}
                  </p>
                  <p className="text-[10px] text-zinc-500 mt-1">Livre para investir</p>
                </motion.div>
            </div>

            {/* Alerts Area */}
            <div className="space-y-4 pt-2">
              <AnimatePresence>
                {/* Overdue */}
                {alerts.overdueCount > 0 && (
                    <AlertSection
                        title="Em atraso"
                        badge={alerts.overdueCount}
                        badgeVariant="destructive"
                        total={alerts.overdueAmount}
                        items={alerts.overdue.map(e => ({
                            id: e.id,
                            label: e.description,
                            amount: e.amount,
                            meta: e.daysOverdue != null ? `${e.daysOverdue} dia${e.daysOverdue !== 1 ? 's' : ''} vencidos` : undefined,
                            metaClass: 'text-rose-400',
                            canPay: true,
                        }))}
                        payingId={isPaying ? payingId : undefined}
                        onPay={handleMarkPaid}
                    />
                )}

                {/* Due today */}
                {alerts.dueTodayCount > 0 && (
                    <AlertSection
                        title="Vence hoje"
                        badge={alerts.dueTodayCount}
                        badgeVariant="destructive"
                        total={alerts.dueTodayAmount}
                        items={alerts.dueToday.map(e => ({
                            id: e.id,
                            label: e.description,
                            amount: e.amount,
                            canPay: true,
                        }))}
                        payingId={isPaying ? payingId : undefined}
                        onPay={handleMarkPaid}
                    />
                )}

                {/* Due this week */}
                {alerts.dueThisWeekCount > 0 && (
                    <AlertSection
                        title="Vence essa semana"
                        badge={alerts.dueThisWeekCount}
                        badgeVariant="secondary"
                        total={alerts.dueThisWeekAmount}
                        items={alerts.dueThisWeek.map(e => ({
                            id: e.id,
                            label: e.description,
                            amount: e.amount,
                            meta: e.date ? format(parseISO(e.date), "dd/MM 'venc.'", { locale: ptBR }) : undefined,
                        }))}
                        payingId={isPaying ? payingId : undefined}
                        onPay={handleMarkPaid}
                    />
                )}

                {/* Pending subscriptions */}
                {alerts.pendingSubscriptionsCount > 0 && (
                    <AlertSection
                        title="Assinaturas pendentes"
                        badge={alerts.pendingSubscriptionsCount}
                        badgeVariant="secondary"
                        items={alerts.pendingSubscriptions.map(s => ({
                            id: s.transactionId,
                            label: s.description,
                            amount: s.amount,
                            meta: s.paymentType === 'credit' ? 'Cartão de Crédito' : 'Débito Automático',
                            canPay: true,
                        }))}
                        payingId={isPaying ? payingId : undefined}
                        onPay={handleMarkPaid}
                    />
                )}
              </AnimatePresence>
            </div>

            {/* All clear */}
            {!alerts.hasUrgentItems && alerts.dueThisWeekCount === 0 && alerts.pendingSubscriptionsCount === 0 && (
                <motion.div 
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  className="flex flex-col items-center justify-center py-16 gap-3 rounded-2xl border border-dashed border-zinc-800 bg-[#0a130f]/20"
                >
                    <div className="w-12 h-12 rounded-full bg-emerald-500/10 text-[#00D4AA] border border-emerald-500/20 flex items-center justify-center relative">
                      <CheckCircle2 className="h-6 w-6" />
                      <span className="absolute inset-0 rounded-full bg-emerald-500/5 blur-sm" />
                    </div>
                    <h3 className="text-sm font-semibold text-zinc-100 tracking-tight">Tudo em dia!</h3>
                    <p className="text-xs text-zinc-400">Você não possui nenhuma conta pendente ou em atraso para o período.</p>
                </motion.div>
            )}
        </div>
    );
}

// ── AlertSection ───────────────────────────────────────────────────────────────

interface AlertItem {
    id?: string;
    label: string;
    amount: number;
    meta?: string;
    metaClass?: string;
    canPay?: boolean;
}

function AlertSection({
    title, badge, badgeVariant = 'secondary', total, items, payingId, onPay,
}: {
    title: string;
    badge: number;
    badgeVariant?: 'destructive' | 'secondary' | 'outline';
    total?: number;
    items: AlertItem[];
    payingId?: string;
    onPay: (id: string) => void;
}) {
    return (
        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, height: 0 }}
          className="rounded-2xl border border-zinc-800 bg-[#0a130f]/60 backdrop-blur-md overflow-hidden"
        >
            <div className="pb-3 pt-4 px-5 flex items-center justify-between border-b border-zinc-900/60">
                <h3 className="text-xs font-bold uppercase tracking-wider text-zinc-300 flex items-center gap-2">
                    {title}
                    <Badge 
                      className={cn(
                        "text-[10px] font-bold px-2 py-0.5 rounded-full border",
                        badgeVariant === 'destructive' 
                          ? 'bg-rose-500/10 text-rose-400 border-rose-500/20' 
                          : 'bg-zinc-800 text-zinc-300 border-zinc-700/50'
                      )}
                    >
                      {badge}
                    </Badge>
                </h3>
                {total != null && <span className="text-sm font-extrabold text-[#00D4AA] tabular-nums">{BRL(total)}</span>}
            </div>
            
            <div className="px-5 py-4">
                <ul className="space-y-3">
                    {items.map((item, idx) => (
                        <li 
                          key={item.id ?? item.label} 
                          className="flex items-center justify-between text-xs gap-3 py-1.5 border-b border-zinc-900/40 last:border-b-0"
                        >
                            <span className="truncate mr-2 font-medium text-zinc-200">{item.label}</span>
                            <span className="flex items-center gap-3 flex-shrink-0">
                                {item.meta && (
                                    <span className={cn("text-[10px] font-semibold tracking-wide uppercase px-1.5 py-0.5 rounded bg-zinc-950 border border-zinc-850", item.metaClass ?? 'text-zinc-500')}>{item.meta}</span>
                                )}
                                <span className="font-bold text-zinc-100 tabular-nums">{BRL(item.amount)}</span>
                                {item.canPay && item.id && (
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        className="h-7 px-2.5 text-xs text-emerald-400 border-emerald-500/20 bg-emerald-500/5 hover:bg-emerald-500/10 hover:text-emerald-300 rounded-lg"
                                        disabled={payingId === item.id}
                                        onClick={() => onPay(item.id!)}
                                        title="Marcar como pago"
                                    >
                                        {payingId === item.id
                                            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                                            : <CheckSquare className="h-3.5 w-3.5" />}
                                    </Button>
                                )}
                            </span>
                        </li>
                    ))}
                </ul>
            </div>
        </motion.div>
    );
}
