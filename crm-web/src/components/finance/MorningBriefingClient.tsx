'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useMarkExpensePaid, useMorningBriefing } from '@/hooks/finance';
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
} from 'lucide-react';
import { toast } from 'sonner';

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
            <div className="flex items-center justify-center min-h-[300px]">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
        );
    }

    if (error || !data) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[300px] gap-4">
                <p className="text-muted-foreground">Não foi possível carregar o briefing.</p>
                <Button variant="outline" size="sm" onClick={() => refetch()}>
                    <RefreshCw className="h-4 w-4 mr-2" />Tentar novamente
                </Button>
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
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Bom dia!</h1>
                    <p className="text-muted-foreground text-sm">
                        {format(now, "EEEE, d 'de' MMMM 'de' yyyy", { locale: ptBR })} — {monthName} {data.period.year}
                    </p>
                </div>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => refetch()}
                    disabled={isFetching}
                    title="Atualizar"
                >
                    <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
                </Button>
            </div>

            {/* Urgent banner */}
            {alerts.hasUrgentItems && (
                <div className="rounded-lg border border-destructive/40 bg-destructive/5 p-4 flex items-start gap-3">
                    <AlertTriangle className="h-5 w-5 text-destructive mt-0.5 flex-shrink-0" />
                    <div>
                        <p className="font-semibold text-destructive text-sm">Ação necessária</p>
                        <p className="text-sm text-muted-foreground mt-0.5">
                            {alerts.overdueCount > 0 && `${alerts.overdueCount} conta${alerts.overdueCount > 1 ? 's' : ''} em atraso (${BRL(alerts.overdueAmount)}). `}
                            {alerts.dueTodayCount > 0 && `${alerts.dueTodayCount} vence${alerts.dueTodayCount === 1 ? '' : 'm'} hoje (${BRL(alerts.dueTodayAmount)}).`}
                        </p>
                    </div>
                </div>
            )}

            {/* Summary cards */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <Card>
                    <CardHeader className="pb-1 pt-4 px-4">
                        <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                            <TrendingUp className="h-3.5 w-3.5" />Receitas
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="px-4 pb-4">
                        <p className="text-lg font-bold text-emerald-600">{BRL(summary.totalIncome)}</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-1 pt-4 px-4">
                        <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                            <CheckCircle2 className="h-3.5 w-3.5" />Pago
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="px-4 pb-4">
                        <p className="text-lg font-bold">{BRL(summary.totalPaid)}</p>
                        <p className="text-xs text-muted-foreground">{summary.paidCount} transações</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-1 pt-4 px-4">
                        <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                            <Clock className="h-3.5 w-3.5" />Pendente
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="px-4 pb-4">
                        <p className="text-lg font-bold text-amber-600">{BRL(summary.totalPending)}</p>
                        <p className="text-xs text-muted-foreground">{summary.unpaidCount} transações</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="pb-1 pt-4 px-4">
                        <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
                            <Wallet className="h-3.5 w-3.5" />Disponível
                        </CardTitle>
                    </CardHeader>
                    <CardContent className="px-4 pb-4">
                        <p className={`text-lg font-bold ${summary.availableToInvest >= 0 ? 'text-emerald-600' : 'text-destructive'}`}>
                            {BRL(summary.availableToInvest)}
                        </p>
                        <p className="text-xs text-muted-foreground">para investir</p>
                    </CardContent>
                </Card>
            </div>

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
                        meta: e.daysOverdue != null ? `${e.daysOverdue} dia${e.daysOverdue !== 1 ? 's' : ''} em atraso` : undefined,
                        metaClass: 'text-destructive',
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
                        meta: e.date ? format(parseISO(e.date), 'dd/MM', { locale: ptBR }) : undefined,
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
                        meta: s.paymentType === 'credit' ? 'Cartão' : 'Débito',
                        canPay: true,
                    }))}
                    payingId={isPaying ? payingId : undefined}
                    onPay={handleMarkPaid}
                />
            )}

            {/* All clear */}
            {!alerts.hasUrgentItems && alerts.dueThisWeekCount === 0 && alerts.pendingSubscriptionsCount === 0 && (
                <div className="flex flex-col items-center justify-center py-10 gap-2 text-muted-foreground">
                    <CheckCircle2 className="h-10 w-10 text-emerald-500" />
                    <p className="font-medium">Tudo em dia!</p>
                    <p className="text-sm">Nenhuma conta pendente ou em atraso.</p>
                </div>
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
        <Card>
            <CardHeader className="pb-2 pt-4 px-4">
                <CardTitle className="text-sm font-semibold flex items-center justify-between">
                    <span className="flex items-center gap-2">
                        {title}
                        <Badge variant={badgeVariant} className="text-xs">{badge}</Badge>
                    </span>
                    {total != null && <span className="text-sm font-bold">{BRL(total)}</span>}
                </CardTitle>
            </CardHeader>
            <CardContent className="px-4 pb-4">
                <ul className="space-y-2">
                    {items.map(item => (
                        <li key={item.id ?? item.label} className="flex items-center justify-between text-sm gap-2">
                            <span className="truncate mr-2 min-w-0">{item.label}</span>
                            <span className="flex items-center gap-2 flex-shrink-0">
                                {item.meta && (
                                    <span className={`text-xs text-muted-foreground ${item.metaClass ?? ''}`}>{item.meta}</span>
                                )}
                                <span className="font-medium">{BRL(item.amount)}</span>
                                {item.canPay && item.id && (
                                    <Button
                                        variant="outline"
                                        size="sm"
                                        className="h-7 px-2 text-xs text-emerald-700 border-emerald-200 hover:bg-emerald-50"
                                        disabled={payingId === item.id}
                                        onClick={() => onPay(item.id!)}
                                        title="Marcar como pago"
                                    >
                                        {payingId === item.id
                                            ? <Loader2 className="h-3 w-3 animate-spin" />
                                            : <CheckSquare className="h-3 w-3" />}
                                    </Button>
                                )}
                            </span>
                        </li>
                    ))}
                </ul>
            </CardContent>
        </Card>
    );
}
