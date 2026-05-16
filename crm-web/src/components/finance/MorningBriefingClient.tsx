'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MorningBriefingResponse, morningBriefingService } from '@/services/personalControlService';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { AlertTriangle, CheckCircle2, Clock, Loader2, RefreshCw, TrendingUp, Wallet } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

const BRL = (v: number) =>
    new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v);

const MONTH_NAMES = ['', 'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];

export default function MorningBriefingClient() {
    const [data, setData] = useState<MorningBriefingResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const result = await morningBriefingService.get();
            setData(result);
        } catch {
            setError('Não foi possível carregar o briefing. Tente novamente.');
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => { load(); }, [load]);

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-[300px]">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
        );
    }

    if (error || !data) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[300px] gap-4">
                <p className="text-muted-foreground">{error ?? 'Sem dados disponíveis.'}</p>
                <Button variant="outline" size="sm" onClick={load}><RefreshCw className="h-4 w-4 mr-2" />Tentar novamente</Button>
            </div>
        );
    }

    const now = new Date(data.generatedAt);
    const monthName = MONTH_NAMES[data.period.month];
    const { summary, alerts } = data;

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
                <Button variant="ghost" size="icon" onClick={load} title="Atualizar">
                    <RefreshCw className="h-4 w-4" />
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
                        label: e.description,
                        amount: e.amount,
                        meta: e.daysOverdue != null ? `${e.daysOverdue} dia${e.daysOverdue !== 1 ? 's' : ''} em atraso` : undefined,
                        metaClass: 'text-destructive',
                    }))}
                />
            )}

            {/* Due today */}
            {alerts.dueTodayCount > 0 && (
                <AlertSection
                    title="Vence hoje"
                    badge={alerts.dueTodayCount}
                    badgeVariant="destructive"
                    total={alerts.dueTodayAmount}
                    items={alerts.dueToday.map(e => ({ label: e.description, amount: e.amount }))}
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
                        label: e.description,
                        amount: e.amount,
                        meta: e.date ? format(parseISO(e.date), 'dd/MM', { locale: ptBR }) : undefined,
                    }))}
                />
            )}

            {/* Pending subscriptions */}
            {alerts.pendingSubscriptionsCount > 0 && (
                <AlertSection
                    title="Assinaturas pendentes"
                    badge={alerts.pendingSubscriptionsCount}
                    badgeVariant="secondary"
                    items={alerts.pendingSubscriptions.map(s => ({
                        label: s.description,
                        amount: s.amount,
                        meta: s.paymentType === 'credit' ? 'Cartão' : 'Débito',
                    }))}
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

interface AlertItem { label: string; amount: number; meta?: string; metaClass?: string; }

function AlertSection({
    title, badge, badgeVariant = 'secondary', total, items,
}: {
    title: string;
    badge: number;
    badgeVariant?: 'destructive' | 'secondary' | 'outline';
    total?: number;
    items: AlertItem[];
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
                    {items.map((item, i) => (
                        <li key={i} className="flex items-center justify-between text-sm">
                            <span className="truncate mr-3">{item.label}</span>
                            <span className="flex items-center gap-3 flex-shrink-0">
                                {item.meta && (
                                    <span className={`text-xs text-muted-foreground ${item.metaClass ?? ''}`}>{item.meta}</span>
                                )}
                                <span className="font-medium">{BRL(item.amount)}</span>
                            </span>
                        </li>
                    ))}
                </ul>
            </CardContent>
        </Card>
    );
}
