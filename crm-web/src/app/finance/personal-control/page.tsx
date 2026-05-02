'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn, formatCurrency } from '@/lib/utils';
import { financeService, type CreditCard, type FinancialAccount } from '@/services/finance';
import {
  CreatePersonalControlExpenseRequest,
  CreatePersonalControlIncomeRequest,
  CreatePersonalControlSubscriptionRequest,
  InvestIQPortfolioSummary,
  PersonalControlBillingFrequency,
  PersonalControlExpenseItem,
  PersonalControlIncomeItem,
  PersonalControlMonthView,
  PersonalControlPaymentType,
  PersonalControlSubscriptionItem,
  TogglePersonalControlStatusRequest,
  investiqService,
  personalControlService,
} from '@/services/personalControlService';
import {
  ArrowDownCircle,
  ArrowLeft,
  ArrowRight,
  ArrowUpCircle,
  Banknote,
  BarChart3,
  Building2,
  Calendar,
  CheckCircle2,
  CreditCard as CreditCardIcon,
  Download,
  FileText,
  PencilLine,
  Plus,
  RotateCcw,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Trash2,
  Upload,
  Wallet,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useEffect, useState, useTransition } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { toast } from 'sonner';

type EditingState<T> = T & { editingId: string | null };

const months = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
];

const paymentTypeOptions: { value: PersonalControlPaymentType; label: string }[] = [
  { value: 'debit', label: 'Débito' },
  { value: 'credit', label: 'Crédito' },
];

const billingFrequencyOptions: { value: PersonalControlBillingFrequency; label: string }[] = [
  { value: 'weekly', label: 'Semanal' },
  { value: 'monthly', label: 'Mensal' },
  { value: 'quarterly', label: 'Trimestral' },
  { value: 'yearly', label: 'Anual' },
];

function currentPeriod() {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}

function monthTitle(year: number, month: number) {
  return `${months[month - 1]} de ${year}`;
}

function incomeFormReset(): EditingState<CreatePersonalControlIncomeRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    dayOfMonth: new Date().getDate(),
    isRecurring: true,
    isPaid: true,
    paymentDate: undefined,
    details: '',
    editingId: null,
  };
}

function expenseFormReset(): EditingState<CreatePersonalControlExpenseRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    paymentType: 'debit',
    dueDay: new Date().getDate(),
    isPaid: false,
    paymentDate: undefined,
    creditCardId: '',
    details: '',
    hasVariableAmount: false,
    editingId: null,
  };
}

function subscriptionFormReset(): EditingState<CreatePersonalControlSubscriptionRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    billingFrequency: 'monthly',
    paymentType: 'credit',
    isPaid: false,
    paymentDate: undefined,
    creditCardId: '',
    details: '',
    hasVariableAmount: false,
    editingId: null,
  };
}

function StatusBadge({ paid, onClick, loading }: { paid: boolean; onClick?: () => void; loading?: boolean }) {
  const interactive = !!onClick;
  const base = 'select-none transition-opacity ' +
    (interactive ? 'cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-emerald-500 ' : '') +
    (loading ? 'opacity-50 pointer-events-none' : '');

  const handleKeyDown = interactive
    ? (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.();
        }
      }
    : undefined;

  const a11y = interactive
    ? {
        role: 'button' as const,
        tabIndex: 0,
        'aria-pressed': paid,
        'aria-busy': loading || undefined,
        'aria-label': paid ? 'Marcar como pendente' : 'Marcar como pago',
        onKeyDown: handleKeyDown,
      }
    : {};

  return paid ? (
    <Badge onClick={onClick} className={cn('border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100', base)} {...a11y}>
      Pago ✓
    </Badge>
  ) : (
    <Badge onClick={onClick} className={cn('border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100', base)} {...a11y}>
      Pendente
    </Badge>
  );
}

/** Retorna o dia efetivo de pagamento (sexta anterior se cair em sábado/domingo) */
function getEffectivePayDay(dayOfMonth: number, year: number, month: number): { effectiveDay: number; adjusted: boolean; label: string } {
  const safeDay = Math.min(dayOfMonth, new Date(year, month, 0).getDate());
  const date = new Date(year, month - 1, safeDay);
  const dow = date.getDay(); // 0=Dom, 6=Sab
  const shift = dow === 6 ? 1 : dow === 0 ? 2 : 0;
  if (shift > 0) {
    const fri = new Date(date);
    fri.setDate(safeDay - shift);
    // Se a antecipação cruzar para o mês anterior, manter o dia original
    // (o pagamento real cai no mês anterior, mas no planner do mês atual
    // a entrada deve aparecer no primeiro bucket, não no último).
    if (fri.getMonth() !== date.getMonth()) {
      return { effectiveDay: safeDay, adjusted: false, label: `Dia ${safeDay}` };
    }
    return { effectiveDay: fri.getDate(), adjusted: true, label: `Dia ${fri.getDate()} (sex, adj. do ${safeDay})` };
  }
  return { effectiveDay: safeDay, adjusted: false, label: `Dia ${safeDay}` };
}

function MetricCard({
  title,
  value,
  description,
  icon: Icon,
  tone,
}: {
  title: string;
  value: string;
  description: string;
  icon: LucideIcon;
  tone: 'green' | 'red' | 'blue' | 'slate';
}) {
  const tones = {
    green: 'from-emerald-50 to-white border-emerald-100 text-emerald-700',
    red: 'from-rose-50 to-white border-rose-100 text-rose-700',
    blue: 'from-sky-50 to-white border-sky-100 text-sky-700',
    slate: 'from-slate-50 to-white border-slate-200 text-slate-700',
  };

  return (
    <Card className={cn('bg-gradient-to-br shadow-sm', tones[tone])}>
      <CardContent className="p-5">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className="mt-2 text-2xl font-bold tracking-tight">{value}</p>
            <p className="mt-1 text-xs text-muted-foreground">{description}</p>
          </div>
          <div className="rounded-xl bg-white/80 p-3 shadow-sm">
            <Icon className="h-5 w-5" />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function SectionShell({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <Card className="shadow-sm">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

const ASSET_CLASS_LABELS: Record<string, string> = {
  acao: 'Ações',
  fii: 'FIIs',
  renda_fixa: 'Renda Fixa',
  bdr: 'BDRs',
  etf: 'ETFs',
};

function InvestIQWidget({ data, loading }: { data: InvestIQPortfolioSummary | null; loading: boolean }) {
  if (loading) {
    return (
      <div className="flex items-center gap-3 rounded-2xl border bg-gradient-to-r from-violet-50 to-indigo-50 px-5 py-4 shadow-sm text-sm text-muted-foreground">
        <BarChart3 className="h-4 w-4 text-violet-400 animate-pulse" />
        Carregando InvestIQ...
      </div>
    );
  }

  if (!data || data.configured === false) return null;

  const pnl = data.unrealized_pnl + data.realized_pnl;
  const pnlPositive = pnl >= 0;
  const retPct = data.total_return_pct;

  return (
    <div className="rounded-2xl border border-violet-200 bg-gradient-to-r from-violet-50 via-indigo-50 to-white px-5 py-4 shadow-sm">
      <div className="flex flex-wrap items-center gap-4">
        <div className="flex items-center gap-2 text-sm font-semibold text-violet-700">
          <BarChart3 className="h-4 w-4" />
          InvestIQ
        </div>
        <div className="h-4 w-px bg-violet-200" />

        {/* Portfolio value */}
        <div className="flex items-center gap-1.5 rounded-lg border border-violet-100 bg-white px-3 py-1.5 text-sm shadow-xs">
          <span className="text-muted-foreground">Carteira</span>
          <span className="font-bold tabular-nums text-violet-700">{formatCurrency(data.portfolio_value)}</span>
        </div>

        {/* P&L */}
        <div className="flex items-center gap-1.5 rounded-lg border bg-white px-3 py-1.5 text-sm shadow-xs">
          {pnlPositive ? (
            <TrendingUp className="h-3.5 w-3.5 text-emerald-600" />
          ) : (
            <TrendingDown className="h-3.5 w-3.5 text-rose-500" />
          )}
          <span className="text-muted-foreground">P&L</span>
          <span className={cn('font-semibold tabular-nums', pnlPositive ? 'text-emerald-600' : 'text-rose-500')}>
            {pnlPositive ? '+' : ''}{formatCurrency(pnl)}
            {retPct !== null && (
              <span className="ml-1 text-xs opacity-75">({retPct >= 0 ? '+' : ''}{retPct?.toFixed(2)}%)</span>
            )}
          </span>
        </div>

        {/* Dividends last 30d */}
        {data.monthly_dividends > 0 && (
          <div className="flex items-center gap-1.5 rounded-lg border bg-white px-3 py-1.5 text-sm shadow-xs">
            <span className="text-muted-foreground">Dividendos/30d</span>
            <span className="font-semibold tabular-nums text-emerald-600">+{formatCurrency(data.monthly_dividends)}</span>
          </div>
        )}

        {/* Allocation pills */}
        {data.asset_allocation.map((a) => (
          <div key={a.asset_class} className="flex items-center gap-1 rounded-full border border-indigo-100 bg-indigo-50 px-2.5 py-1 text-xs text-indigo-700">
            <span className="font-medium">{ASSET_CLASS_LABELS[a.asset_class] ?? a.asset_class}</span>
            <span className="opacity-60">{a.percentage.toFixed(1)}%</span>
          </div>
        ))}

        <div className="ml-auto flex items-center gap-1 text-[11px] text-muted-foreground">
          <span>{data.position_count} posições</span>
        </div>
      </div>
    </div>
  );
}

function PatrimonioTotalCard({
  accountsTotal,
  accountsCount,
  investiqValue,
  investiqConfigured,
  investiqLoading,
}: {
  accountsTotal: number;
  accountsCount: number;
  investiqValue: number | null;
  investiqConfigured: boolean;
  investiqLoading: boolean;
}) {
  if (accountsCount === 0 && !investiqConfigured && !investiqLoading) return null;

  const investiqAmount = investiqConfigured ? (investiqValue ?? 0) : 0;
  const total = accountsTotal + investiqAmount;
  const accountsPct = total > 0 ? Math.round((accountsTotal / total) * 100) : 0;
  const investiqPct = total > 0 ? Math.round((investiqAmount / total) * 100) : 0;

  return (
    <div className="rounded-2xl border bg-gradient-to-r from-emerald-50 via-white to-violet-50 p-5 shadow-sm dark:from-emerald-950/40 dark:via-slate-950 dark:to-violet-950/40">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <div className="rounded-xl bg-gradient-to-br from-emerald-500 to-violet-500 p-2.5 text-white">
            <Wallet className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Patrimônio Total</p>
            <p className="text-3xl font-semibold tracking-tight">{formatCurrency(total)}</p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <div className="rounded-lg border bg-white/70 px-3 py-2 text-xs shadow-xs dark:bg-slate-900/70">
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Building2 className="h-3.5 w-3.5" />
              <span>Contas</span>
              {accountsPct > 0 && <span className="font-medium text-foreground">{accountsPct}%</span>}
            </div>
            <p className="font-semibold tabular-nums">{formatCurrency(accountsTotal)}</p>
          </div>
          <div className="rounded-lg border bg-white/70 px-3 py-2 text-xs shadow-xs dark:bg-slate-900/70">
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <BarChart3 className="h-3.5 w-3.5" />
              <span>InvestIQ</span>
              {investiqConfigured && investiqPct > 0 && <span className="font-medium text-foreground">{investiqPct}%</span>}
            </div>
            <p className="font-semibold tabular-nums">
              {investiqLoading ? '—' : investiqConfigured ? formatCurrency(investiqAmount) : 'não configurado'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function PatrimonioWidget({
  accounts,
  onUpdateBalance,
}: {
  accounts: FinancialAccount[];
  onUpdateBalance: (id: string, balance: number) => Promise<void>;
}) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [saving, setSaving] = useState(false);

  if (accounts.length === 0) return null;
  const total = accounts.reduce((sum, a) => sum + a.balance, 0);

  const startEdit = (a: FinancialAccount) => {
    setEditingId(a.id);
    setDraft(String(a.balance));
  };

  const cancelEdit = () => {
    setEditingId(null);
    setDraft('');
  };

  const saveEdit = async (id: string) => {
    const parsed = parseFloat(draft.replace(',', '.'));
    if (isNaN(parsed)) { toast.error('Valor inválido.'); return; }
    setSaving(true);
    try {
      await onUpdateBalance(id, parsed);
    } catch (err) {
      console.error('saveEdit error:', err);
      toast.error('Erro ao salvar saldo: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      setSaving(false);
      setEditingId(null);
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-2xl border bg-gradient-to-r from-slate-50 to-white px-5 py-4 shadow-sm">
      <div className="flex items-center gap-2 text-sm font-semibold text-slate-700">
        <Building2 className="h-4 w-4 text-slate-500" />
        Patrimônio
      </div>
      <div className="h-4 w-px bg-slate-200" />
      {accounts.map((a) => (
        <div key={a.id} className="flex items-center gap-1.5 rounded-lg border bg-white px-3 py-1.5 text-sm shadow-xs">
          <span className="text-muted-foreground">{a.name}</span>
          {editingId === a.id ? (
            <div className="flex items-center gap-1">
              <Input
                className="h-6 w-28 text-xs px-1.5"
                value={draft}
                autoFocus
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') void saveEdit(a.id);
                  if (e.key === 'Escape') cancelEdit();
                }}
              />
              <button
                onClick={() => void saveEdit(a.id)}
                disabled={saving}
                className="text-emerald-600 hover:text-emerald-700 text-xs font-medium disabled:opacity-50"
              >
                ✓
              </button>
              <button onClick={cancelEdit} className="text-slate-400 hover:text-slate-600 text-xs">✕</button>
            </div>
          ) : (
            <button
              onClick={() => startEdit(a)}
              className={cn(
                'font-semibold tabular-nums hover:underline cursor-pointer',
                a.balance >= 0 ? 'text-emerald-700' : 'text-rose-600'
              )}
            >
              {formatCurrency(a.balance)}
            </button>
          )}
        </div>
      ))}
      <div className="ml-auto flex items-center gap-1.5 rounded-xl border border-slate-200 bg-slate-900 px-4 py-1.5 text-sm">
        <span className="text-slate-300">Total</span>
        <span className={cn('font-bold tabular-nums', total >= 0 ? 'text-emerald-400' : 'text-rose-400')}>
          {formatCurrency(total)}
        </span>
      </div>
    </div>
  );
}

function SalaryPlannerSection({
  monthView,
}: {
  monthView: PersonalControlMonthView;
}) {
  // Cada salário forma um bucket próprio, ordenado pelo dia efetivo de pagamento.
  // Despesas pertencem ao bucket do salário cujo dia <= dueDay < próximo salário.
  // Despesas que excedem o caixa disponível do bucket são marcadas como PENDENTE.
  type IncomeItem = (typeof monthView.incomes)[number];
  type ExpenseItem = (typeof monthView.expenses)[number] & { _pending?: boolean };
  type SubItem = (typeof monthView.subscriptions)[number] & { _pending?: boolean };

  const sortedIncomes = [...monthView.incomes].sort((a, b) => {
    const ea = getEffectivePayDay(a.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay;
    const eb = getEffectivePayDay(b.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay;
    return ea - eb;
  });

  const startDays = sortedIncomes.map((inc) =>
    getEffectivePayDay(inc.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay,
  );

  const buckets = sortedIncomes.reduce<{
    day: number; nextDay: number; incomeName: string;
    incomes: IncomeItem[];
    expensesAtVista: ExpenseItem[]; expensesCredito: ExpenseItem[];
    subscriptions: SubItem[];
    totalIncome: number; totalExpenseAtVista: number; totalExpenseCredito: number;
    pendingTotal: number;
    periodBalance: number; runningBalance: number; investSuggestion: number;
  }[]>((acc, inc, i) => {
    const day = startDays[i];
    const nextDay = startDays[i + 1] ?? 32;
    const incomes = [inc];
    // Separa despesas por tipo: apenas 'debit' afeta o caixa; 'credit' é informativa
    const inRange = (dueDay: number) => dueDay >= day && dueDay < nextDay;
    const expensesAtVistaRaw = monthView.expenses.filter((item) => inRange(item.dueDay) && item.paymentType !== 'credit');
    const expensesCreditoRaw = monthView.expenses.filter((item) => inRange(item.dueDay) && item.paymentType === 'credit');
    const subscriptionsRaw = i === 0 ? monthView.subscriptions : ([] as typeof monthView.subscriptions);
    const totalIncome = inc.amount;

    // Walk apenas despesas à vista + subscriptions (afetam o caixa).
    // Crédito nunca participa do walking e nunca é marcado como PENDENTE.
    const prevRunning = acc.length > 0 ? acc[acc.length - 1].runningBalance : 0;
    let available = prevRunning + totalIncome;
    const subscriptions: SubItem[] = subscriptionsRaw.map((s) => {
      if (available - s.amount < 0) return { ...s, _pending: true };
      available -= s.amount;
      return { ...s, _pending: false };
    });
    const expensesAtVistaSorted = [...expensesAtVistaRaw].sort((a, b) => a.dueDay - b.dueDay);
    const expensesAtVista: ExpenseItem[] = expensesAtVistaSorted.map((e) => {
      if (available - e.amount < 0) return { ...e, _pending: true };
      available -= e.amount;
      return { ...e, _pending: false };
    });
    const expensesCredito: ExpenseItem[] = [...expensesCreditoRaw]
      .sort((a, b) => a.dueDay - b.dueDay)
      .map((e) => ({ ...e, _pending: false }));

    const totalExpenseAtVista =
      expensesAtVista.reduce((s, item) => s + item.amount, 0) +
      subscriptions.reduce((s, item) => s + item.amount, 0);
    const totalExpenseCredito = expensesCredito.reduce((s, item) => s + item.amount, 0);
    const pendingTotal =
      expensesAtVista.filter((e) => e._pending).reduce((s, e) => s + e.amount, 0) +
      subscriptions.filter((e) => e._pending).reduce((s, e) => s + e.amount, 0);
    // Saldo do salário considera APENAS saídas à vista (crédito é ignorado)
    const periodBalance = totalIncome - totalExpenseAtVista;
    const runningBalance = prevRunning + periodBalance;
    const investSuggestion = runningBalance > 0 && periodBalance > 0 ? periodBalance * 0.2 : 0;
    acc.push({
      day, nextDay, incomeName: inc.name, incomes,
      expensesAtVista, expensesCredito, subscriptions,
      totalIncome, totalExpenseAtVista, totalExpenseCredito, pendingTotal,
      periodBalance, runningBalance, investSuggestion,
    });
    return acc;
  }, []);

  return (
    <Card className="shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-3">
          <div>
            <CardTitle className="text-lg">Planner de Salário</CardTitle>
            <CardDescription>Cada salário cobre as despesas até o próximo recebimento. Despesas que excedem o caixa são marcadas como PENDENTE.</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-2 gap-3">
          {buckets.map(({ day, nextDay, incomes, expensesAtVista, expensesCredito, subscriptions, totalIncome, totalExpenseAtVista, totalExpenseCredito, pendingTotal, periodBalance, runningBalance, investSuggestion }) => (
            <div key={day} className={cn('rounded-2xl border p-4', runningBalance >= 0 ? 'bg-emerald-50/40 border-emerald-100' : 'bg-rose-50/40 border-rose-100')}>
              {/* Cabeçalho da linha */}
              <div className="flex flex-wrap items-center gap-4 mb-3">
                <span className="text-sm font-semibold text-slate-700 w-24 shrink-0">
                  Dia {day}{nextDay < 32 ? `–${nextDay - 1}` : '+'}
                </span>

                {/* Entradas inline */}
                {incomes.length > 0 && (
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-[11px] font-semibold uppercase tracking-wide text-emerald-600">Entradas:</span>
                    {incomes.map((item) => (
                      <span key={item.id} className="rounded-md bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800">
                        {item.name} {formatCurrency(item.amount)}
                      </span>
                    ))}
                  </div>
                )}

                {/* Totais e saldo à direita */}
                <div className="ml-auto flex items-center gap-4 text-xs">
                  <span className="text-slate-500">Receitas <span className="text-emerald-600 font-semibold">{formatCurrency(totalIncome)}</span></span>
                  <span className="text-slate-500">À vista <span className="text-rose-500 font-semibold">{formatCurrency(totalExpenseAtVista)}</span></span>
                  {totalExpenseCredito > 0 && (
                    <span className="text-slate-400">Crédito <span className="text-slate-500 font-semibold">{formatCurrency(totalExpenseCredito)}</span></span>
                  )}
                  <span className={cn('font-bold tabular-nums text-xs', periodBalance >= 0 ? 'text-emerald-600' : 'text-rose-500')}>
                    Período: {periodBalance >= 0 ? '+' : ''}{formatCurrency(periodBalance)}
                  </span>
                  <span className={cn('font-bold tabular-nums text-sm border-l pl-3', runningBalance >= 0 ? 'text-emerald-700' : 'text-rose-600')}>
                    Acum: {runningBalance >= 0 ? '+' : ''}{formatCurrency(runningBalance)}
                  </span>
                  {investSuggestion > 0 && (
                    <span className="flex items-center gap-1 rounded-lg bg-sky-50 px-2 py-1 text-sky-700">
                      <TrendingUp className="h-3 w-3" />Investir 20%: <strong>{formatCurrency(investSuggestion)}</strong>
                    </span>
                  )}
                </div>
              </div>

              {/* Saídas à Vista (debit/pix/etc) — afetam o saldo */}
              {(expensesAtVista.length > 0 || subscriptions.length > 0) && (
                <div className="flex flex-col gap-1 mt-1">
                  <span className="text-[11px] font-semibold uppercase tracking-wide text-rose-500">Saídas à vista:</span>
                  {expensesAtVista.map((item) => (
                    <span key={item.id} className={cn('rounded-md px-2 py-0.5 text-xs border', item._pending ? 'bg-amber-100 border-amber-300 text-amber-900 font-semibold' : 'bg-rose-50 border-rose-100 text-slate-600')}>
                      {item._pending && '⚠ PENDENTE — '}{item.name} <span className="font-medium">{formatCurrency(item.amount)}</span>
                    </span>
                  ))}
                  {subscriptions.map((item) => (
                    <span key={item.id} className={cn('rounded-md px-2 py-0.5 text-xs border flex items-center gap-1', item._pending ? 'bg-amber-100 border-amber-300 text-amber-900 font-semibold' : 'bg-rose-50 border-rose-100 text-slate-600')}>
                      {item._pending && '⚠ PENDENTE — '}{item.name} <span className="font-medium">{formatCurrency(item.amount)}</span>
                      {item.paymentType === 'credit'
                        ? <span className="ml-1 inline-flex items-center gap-0.5 rounded bg-blue-100 px-1 text-[10px] text-blue-700"><CreditCardIcon className="h-2.5 w-2.5" />{item.creditCardName || 'Crédito'}</span>
                        : <span className="ml-1 inline-flex items-center gap-0.5 rounded bg-emerald-100 px-1 text-[10px] text-emerald-700"><Banknote className="h-2.5 w-2.5" />PIX</span>}
                    </span>
                  ))}
                  {pendingTotal > 0 && (
                    <span className="mt-1 text-[11px] text-amber-700">
                      Total não coberto por este salário: <strong>{formatCurrency(pendingTotal)}</strong> — será paga vencida ou com o próximo salário.
                    </span>
                  )}
                </div>
              )}

              {/* Saídas a Crédito — apenas informativas, não afetam o saldo */}
              {expensesCredito.length > 0 && (
                <div className="flex flex-col gap-1 mt-2 pt-2 border-t border-slate-200/70">
                  <span className="text-[11px] font-semibold uppercase tracking-wide text-slate-400">💳 Saídas no crédito (não afetam o saldo):</span>
                  {expensesCredito.map((item) => (
                    <span key={item.id} className="rounded-md bg-slate-50 border border-slate-200 px-2 py-0.5 text-xs text-slate-500">
                      {item.name}
                      {item.creditCardName && <span className="text-slate-400"> · {item.creditCardName}</span>}
                      <span className="font-medium"> {formatCurrency(item.amount)}</span>
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function Page() {
  const [period, setPeriod] = useState(currentPeriod);
  const [isPending, startTransition] = useTransition();
  const [loading, setLoading] = useState(true);
  const [monthView, setMonthView] = useState<PersonalControlMonthView | null>(null);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [investiq, setInvestiq] = useState<InvestIQPortfolioSummary | null>(null);
  const [investiqLoading, setInvestiqLoading] = useState(true);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [incomeForm, setIncomeForm] = useState(incomeFormReset);
  const [expenseForm, setExpenseForm] = useState(expenseFormReset);
  const [subscriptionForm, setSubscriptionForm] = useState(subscriptionFormReset);
  const [incomeDialogOpen, setIncomeDialogOpen] = useState(false);
  const [expenseDialogOpen, setExpenseDialogOpen] = useState(false);
  const [subscriptionDialogOpen, setSubscriptionDialogOpen] = useState(false);
  const [deleteDialog, setDeleteDialog] = useState<{
    kind: 'income' | 'expense' | 'subscription';
    id: string;
    name: string;
  } | null>(null);
  const [importingSheet, setImportingSheet] = useState(false);
  const [copyingRecurring, setCopyingRecurring] = useState(false);
  const [editingStatementId, setEditingStatementId] = useState<string | null>(null);
  const [statementDraft, setStatementDraft] = useState('');
  const [payingInvoiceId, setPayingInvoiceId] = useState<string | null>(null);

  // Feature 2 — PDF statement import
  type ParsedStatementTx = { description: string; amount: number; date: string; selected: boolean };
  const [pdfImportCard, setPdfImportCard] = useState<{ creditCardId: string; creditCardName: string } | null>(null);
  const [parsedTxList, setParsedTxList] = useState<ParsedStatementTx[]>([]);
  const [pdfParsing, setPdfParsing] = useState(false);
  const [pdfImporting, setPdfImporting] = useState(false);

  const loadMonth = async (year: number, month: number) => {
    setLoading(true);
    try {
      const [view, cards, accts] = await Promise.all([
        personalControlService.getMonthView(year, month),
        financeService.getCreditCards(),
        financeService.getFinancialAccounts(),
      ]);
      setMonthView(view);
      setCreditCards(cards);
      setAccounts(accts);
    } catch (error) {
      console.error('Erro ao carregar controle mensal', error);
      setMonthView(null);
      toast.error('Não foi possível carregar o controle mensal.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setInvestiqLoading(true);
    void investiqService.getPortfolioSummary().then((data) => {
      setInvestiq(data);
      setInvestiqLoading(false);
    });
  }, []);

  useEffect(() => {
    void loadMonth(period.year, period.month);
  }, [period.year, period.month]);

  const changeMonth = (delta: number) => {
    startTransition(() => {
      setPeriod((current) => {
        const next = new Date(current.year, current.month - 1 + delta, 1);
        return { year: next.getFullYear(), month: next.getMonth() + 1 };
      });
    });
  };

  const resetToCurrentMonth = () => {
    startTransition(() => setPeriod(currentPeriod()));
  };

  const refresh = async () => {
    await loadMonth(period.year, period.month);
  };

  const saveStatus = async (
    kind: 'income' | 'expense' | 'subscription',
    id: string,
    isPaid: boolean,
  ) => {
      const payload: TogglePersonalControlStatusRequest = {
      year: period.year,
      month: period.month,
      isPaid,
      paymentDate: isPaid ? new Date().toISOString() : undefined,
    };

    setSavingKey(`${kind}-${id}`);
    try {
      if (kind === 'income') {
        await personalControlService.toggleIncomeStatus(id, payload);
      } else if (kind === 'expense') {
        await personalControlService.toggleExpenseStatus(id, payload);
      } else {
        await personalControlService.toggleSubscriptionStatus(id, payload);
      }
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao atualizar status.');
    } finally {
      setSavingKey(null);
    }
  };

  const confirmDelete = async () => {
    if (!deleteDialog) return;

    const { kind, id } = deleteDialog;
    setSavingKey(`delete-${kind}-${id}`);
    try {
      if (kind === 'income') {
        await personalControlService.deleteIncome(id);
      } else if (kind === 'expense') {
        await personalControlService.deleteExpense(id);
      } else {
        await personalControlService.deleteSubscription(id);
      }
      toast.success('Registro excluído.');
      setDeleteDialog(null);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao excluir registro.');
    } finally {
      setSavingKey(null);
    }
  };

  const importFromSheet = async () => {
    setImportingSheet(true);
    try {
      const result = await personalControlService.importFromSheet(period.year, period.month);
      toast.success(`Importado: ${result.matchedCards} cartões correspondidos, ${result.unmatchedCards} sem correspondência.`);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao importar da planilha.');
    } finally {
      setImportingSheet(false);
    }
  };

  const handleCopyRecurring = async () => {
    setCopyingRecurring(true);
    try {
      const result = await personalControlService.copyRecurring(period.year, period.month);
      const createdCount = result.created.length;
      const skippedCount = result.skipped.length;
      const cardSkipped = result.skipped.filter((s) => s.skipReason === 'CreditCardSkipped').length;
      const variableAmount = result.created.filter((c) => c.hasVariableAmount);

      if (createdCount === 0 && skippedCount === 0) {
        toast.info('Nenhuma recorrência aplicável a este mês.');
      } else if (createdCount === 0) {
        toast.info(`Nenhuma recorrência criada (${skippedCount} já existem ou não puderam ser geradas).`);
      } else {
        const cardSuffix = cardSkipped > 0 ? ` ${cardSkipped} de cartão precisam ser geradas manualmente.` : '';
        toast.success(`${createdCount} lançamento${createdCount !== 1 ? 's' : ''} gerado${createdCount !== 1 ? 's' : ''} a partir das recorrências.${cardSuffix}`);

        // Variable-amount items (condomínio com taxa extra, salário dolarizado, por dias úteis):
        // surface a separate warning so the user remembers to update the actual value.
        if (variableAmount.length > 0) {
          const sample = variableAmount.slice(0, 3).map((v) => v.description).join(', ');
          const more = variableAmount.length > 3 ? ` e mais ${variableAmount.length - 3}` : '';
          toast.warning(
            `${variableAmount.length} item${variableAmount.length !== 1 ? 's' : ''} com valor variável: ${sample}${more}. Confirme o valor real do mês.`,
            { duration: 8000 },
          );
        }
      }
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao copiar recorrências.');
    } finally {
      setCopyingRecurring(false);
    }
  };

  const handlePdfUpload = async (file: File, card: { creditCardId: string; creditCardName: string }) => {
    setPdfImportCard(card);
    setParsedTxList([]);
    setPdfParsing(true);
    try {
      const result = await personalControlService.parseStatement(file);
      const txs: ParsedStatementTx[] = result.transactions.map((t: { description: string; amount: number; date: string }) => ({
        ...t,
        amount: Math.abs(t.amount),
        selected: t.amount < 0,
      }));
      setParsedTxList(txs);
      if (txs.length === 0) toast.info('Nenhuma transação encontrada no PDF.');
    } catch {
      toast.error('Falha ao analisar o PDF. Verifique se o arquivo não está protegido por senha.');
      setPdfImportCard(null);
    } finally {
      setPdfParsing(false);
    }
  };

  const importSelectedPdfTxs = async () => {
    if (!pdfImportCard) return;
    const selected = parsedTxList.filter((t) => t.selected);
    if (selected.length === 0) { toast.info('Nenhuma transação selecionada.'); return; }
    setPdfImporting(true);
    let ok = 0; let fail = 0;
    for (const tx of selected) {
      try {
        const d = new Date(tx.date + 'T12:00:00Z');
        await personalControlService.createExpense({
          year: period.year,
          month: period.month,
          name: tx.description,
          amount: tx.amount,
          paymentType: 'credit',
          dueDay: d.getUTCDate(),
          isPaid: false,
          creditCardId: pdfImportCard.creditCardId,
        });
        ok++;
      } catch { fail++; }
    }
    await loadMonth(period.year, period.month);
    setPdfImportCard(null);
    setParsedTxList([]);
    if (fail === 0) toast.success(`${ok} despesa${ok !== 1 ? 's' : ''} importada${ok !== 1 ? 's' : ''} com sucesso.`);
    else toast.warning(`${ok} importadas, ${fail} falhas.`);
    setPdfImporting(false);
  };

  const saveStatementAmount = async (invoiceId: string) => {
    const amount = parseFloat(statementDraft.replace(',', '.'));
    if (isNaN(amount)) {
      toast.error('Valor inválido.');
      return;
    }
    setSavingKey(`statement-${invoiceId}`);
    try {
      await personalControlService.setCardStatementAmount(invoiceId, amount);
      toast.success('Valor da fatura atualizado.');
      setEditingStatementId(null);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao atualizar valor da fatura.');
    } finally {
      setSavingKey(null);
    }
  };

  const payInvoice = async (invoiceId: string, paymentDate: string) => {
    setSavingKey(`pay-invoice-${invoiceId}`);
    try {
      await personalControlService.payCardInvoice(invoiceId, paymentDate);
      toast.success('Fatura marcada como paga.');
      setPayingInvoiceId(null);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao marcar fatura como paga.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitIncome = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('income');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: incomeForm.name,
        amount: Number(incomeForm.amount),
        dayOfMonth: Number(incomeForm.dayOfMonth),
        isRecurring: Boolean(incomeForm.isRecurring),
        isPaid: Boolean(incomeForm.isPaid),
        paymentDate: incomeForm.paymentDate || undefined,
        details: incomeForm.details || undefined,
      };

      if (incomeForm.editingId) {
        await personalControlService.updateIncome(incomeForm.editingId, payload);
        toast.success('Receita atualizada.');
      } else {
        await personalControlService.createIncome(payload);
        toast.success('Receita criada.');
      }
      setIncomeForm(incomeFormReset());
      setIncomeDialogOpen(false);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar receita.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitExpense = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('expense');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: expenseForm.name,
        amount: Number(expenseForm.amount),
        paymentType: expenseForm.paymentType,
        dueDay: Number(expenseForm.dueDay),
        isPaid: Boolean(expenseForm.isPaid),
        paymentDate: expenseForm.paymentDate || undefined,
        details: expenseForm.details || undefined,
        creditCardId: expenseForm.paymentType === 'credit' && expenseForm.creditCardId ? expenseForm.creditCardId : undefined,
      };

      if (expenseForm.editingId) {
        await personalControlService.updateExpense(expenseForm.editingId, payload);
        toast.success('Despesa atualizada.');
      } else {
        await personalControlService.createExpense(payload);
        toast.success('Despesa criada.');
      }
      setExpenseForm(expenseFormReset());
      setExpenseDialogOpen(false);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar despesa.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitSubscription = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('subscription');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: subscriptionForm.name,
        amount: Number(subscriptionForm.amount),
        billingFrequency: subscriptionForm.billingFrequency,
        paymentType: subscriptionForm.paymentType,
        isPaid: Boolean(subscriptionForm.isPaid),
        paymentDate: subscriptionForm.paymentDate || undefined,
        details: subscriptionForm.details || undefined,
        creditCardId: subscriptionForm.paymentType === 'credit' && subscriptionForm.creditCardId ? subscriptionForm.creditCardId : undefined,
      };

      if (subscriptionForm.editingId) {
        await personalControlService.updateSubscription(subscriptionForm.editingId, payload);
        toast.success('Assinatura atualizada.');
      } else {
        await personalControlService.createSubscription(payload);
        toast.success('Assinatura criada.');
      }
      setSubscriptionForm(subscriptionFormReset());
      setSubscriptionDialogOpen(false);
      await refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar assinatura.');
    } finally {
      setSavingKey(null);
    }
  };

  const editIncome = (item: PersonalControlIncomeItem) => {
    setIncomeForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      dayOfMonth: item.dayOfMonth,
      isRecurring: item.isRecurring,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      details: item.details || '',
      editingId: item.id,
    });
    setIncomeDialogOpen(true);
  };

  const editExpense = (item: PersonalControlExpenseItem) => {
    setExpenseForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      paymentType: item.paymentType,
      dueDay: item.dueDay,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      hasVariableAmount: item.hasVariableAmount ?? false,
      editingId: item.id,
    });
    setExpenseDialogOpen(true);
  };

  const editSubscription = (item: PersonalControlSubscriptionItem) => {
    setSubscriptionForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      billingFrequency: item.billingFrequency,
      paymentType: item.paymentType,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      hasVariableAmount: item.hasVariableAmount ?? false,
      editingId: item.id,
    });
    setSubscriptionDialogOpen(true);
  };

  const openNewIncomeDialog = () => {
    setIncomeForm(incomeFormReset());
    setIncomeDialogOpen(true);
  };

  const openNewExpenseDialog = () => {
    setExpenseForm(expenseFormReset());
    setExpenseDialogOpen(true);
  };

  const openNewSubscriptionDialog = () => {
    setSubscriptionForm(subscriptionFormReset());
    setSubscriptionDialogOpen(true);
  };

  // FASE 2A — update account balance
  const updateAccountBalance = async (id: string, balance: number) => {
    await financeService.updateAccountBalance(id, balance);
    toast.success('Saldo atualizado.');
    await refresh();
  };

  // FASE 2C — quick expense
  type QuickPaymentType = 'pix' | 'debit' | 'credit' | 'auto';
  const [quickExpenseOpen, setQuickExpenseOpen] = useState(false);
  const [quickExpense, setQuickExpense] = useState({
    name: '',
    amount: '' as string | number,
    date: new Date().toISOString().slice(0, 10),
    paymentKind: 'pix' as QuickPaymentType,
    creditCardId: '',
  });

  const submitQuickExpense = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const amount = parseFloat(String(quickExpense.amount).replace(',', '.'));
    if (isNaN(amount) || amount <= 0) { toast.error('Valor inválido.'); return; }
    const date = new Date(quickExpense.date + 'T12:00:00');
    const paymentType = quickExpense.paymentKind === 'credit' ? 'credit' : 'debit';
    setSavingKey('quick-expense');
    try {
      await personalControlService.createExpense({
        year: date.getFullYear(),
        month: date.getMonth() + 1,
        name: quickExpense.name,
        amount,
        paymentType,
        dueDay: date.getDate(),
        isPaid: false,
        creditCardId: paymentType === 'credit' && quickExpense.creditCardId ? quickExpense.creditCardId : undefined,
      });
      toast.success('Despesa criada.');
      setQuickExpenseOpen(false);
      setQuickExpense({ name: '', amount: '', date: new Date().toISOString().slice(0, 10), paymentKind: 'pix', creditCardId: '' });
      await refresh();
    } catch (err) {
      console.error(err);
      toast.error('Falha ao criar despesa.');
    } finally {
      setSavingKey(null);
    }
  };

  const summary = monthView?.summary;

  return (
    <div className="p-8 max-w-[1700px] mx-auto space-y-8">
      <div className="rounded-3xl border bg-gradient-to-br from-slate-950 via-slate-900 to-slate-800 p-8 text-white shadow-xl">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-3">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-3 py-1 text-xs font-medium text-white/80">
              <Wallet className="h-3.5 w-3.5" />
              Planilha Financeira
            </div>
            <h1 className="text-3xl font-bold tracking-tight md:text-4xl">Planilha Financeira</h1>
            <p className="max-w-2xl text-sm text-white/70 md:text-base">
              Receitas, despesas, assinaturas e cartões em uma visão mensal unificada, com marcação de
              pago/pendente e resumo consolidado do período.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Button variant="outline" onClick={() => changeMonth(-1)} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              <ArrowLeft className="h-4 w-4" />
              Mês anterior
            </Button>
            <Button variant="outline" onClick={resetToCurrentMonth} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              <RotateCcw className="h-4 w-4" />
              Mês atual
            </Button>
            <Button variant="outline" onClick={() => changeMonth(1)} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              Próximo mês
              <ArrowRight className="h-4 w-4" />
            </Button>
            <div className="rounded-xl border border-white/15 bg-white/10 px-4 py-2 text-sm font-medium text-white/90">
              {isPending ? 'Atualizando...' : monthTitle(period.year, period.month)}
            </div>
          </div>
        </div>
      </div>

      <PatrimonioTotalCard
        accountsTotal={accounts.reduce((sum, a) => sum + a.balance, 0)}
        accountsCount={accounts.length}
        investiqValue={investiq?.portfolio_value ?? null}
        investiqConfigured={investiq !== null && investiq.configured !== false}
        investiqLoading={investiqLoading}
      />

      <PatrimonioWidget accounts={accounts} onUpdateBalance={updateAccountBalance} />

      <InvestIQWidget data={investiq} loading={investiqLoading} />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Total de receitas" value={formatCurrency(summary?.totalIncome || 0)} description="Entradas fixas e recorrentes" icon={ArrowUpCircle} tone="green" />
        <MetricCard title="Total de despesas" value={formatCurrency(summary?.totalExpenses || 0)} description="Saídas do mês selecionado" icon={ArrowDownCircle} tone="red" />
        <MetricCard title="Despesas no cartão" value={formatCurrency(summary?.totalCreditExpenses || 0)} description="Lançamentos vinculados ao crédito" icon={CreditCardIcon} tone="blue" />
        <MetricCard title="Saldo restante" value={formatCurrency(summary?.remainingBalance || 0)} description="Resultado consolidado do período" icon={Wallet} tone="slate" />
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Pago" value={formatCurrency(summary?.paidAmount || 0)} description={`${summary?.paidCount || 0} itens quitados`} icon={CheckCircle2} tone="green" />
        <MetricCard title="Pendente" value={formatCurrency(summary?.unpaidAmount || 0)} description={`${summary?.unpaidCount || 0} itens em aberto`} icon={Calendar} tone="red" />
        <MetricCard title="Com cartão" value={formatCurrency(summary?.withCardAmount || 0)} description="Itens com cartão de crédito" icon={CreditCardIcon} tone="blue" />
        <MetricCard title="Sem cartão" value={formatCurrency(summary?.withoutCardAmount || 0)} description="Itens pagos sem cartão" icon={Wallet} tone="slate" />
      </div>

      {/* KPIs de Cartões e Projeção Financeira */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Total faturas cartões"
          value={formatCurrency(summary?.totalCardStatements || 0)}
          description={`${summary?.cardsPendingCount || 0} pendentes · ${summary?.cardsPaidCount || 0} pagas`}
          icon={CreditCardIcon}
          tone="blue"
        />
        <MetricCard
          title="Faturas pendentes"
          value={formatCurrency(summary?.totalCardPending || 0)}
          description="Cartões ainda não pagos"
          icon={Calendar}
          tone="red"
        />
        <MetricCard
          title="Total a pagar"
          value={formatCurrency(summary?.totalToPay || 0)}
          description="Débitos + faturas pendentes"
          icon={ArrowDownCircle}
          tone="red"
        />
        <MetricCard
          title="Disponível p/ investir"
          value={formatCurrency(summary?.availableToInvest || 0)}
          description="Receita − despesas − cartões"
          icon={Wallet}
          tone={(summary?.availableToInvest ?? 0) > 0 ? 'green' : 'red'}
        />
      </div>

      {monthView && (
        <SalaryPlannerSection monthView={monthView} />
      )}

      {monthView
        && monthView.incomes.length === 0
        && monthView.expenses.length === 0
        && monthView.subscriptions.length === 0
        && (
          <div className="rounded-lg border border-violet-200 bg-violet-50 p-4 dark:border-violet-900 dark:bg-violet-950/30">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-start gap-3">
                <Sparkles className="mt-0.5 h-5 w-5 shrink-0 text-violet-600 dark:text-violet-400" />
                <div className="space-y-1">
                  <p className="font-medium text-violet-900 dark:text-violet-100">
                    Mês ainda vazio
                  </p>
                  <p className="text-sm text-violet-800/80 dark:text-violet-200/80">
                    Gere automaticamente as receitas, despesas e assinaturas recorrentes ativas para {monthView.period.label}. Despesas no cartão precisam ser lançadas manualmente.
                  </p>
                </div>
              </div>
              <Button
                onClick={handleCopyRecurring}
                disabled={copyingRecurring}
                className="gap-2 self-start bg-violet-600 hover:bg-violet-700 text-white sm:self-auto"
              >
                <Sparkles className="h-4 w-4" />
                {copyingRecurring ? 'Gerando...' : 'Copiar recorrências'}
              </Button>
            </div>
          </div>
        )}

      <SectionShell title="Ações" description="Abra os formulários só quando precisar criar ou editar um lançamento.">
        <div className="flex flex-wrap gap-3">
          <Button className="gap-2" onClick={openNewIncomeDialog}>
            <Plus className="h-4 w-4" />
            Adicionar receita
          </Button>
          <Button className="gap-2" onClick={() => setQuickExpenseOpen(true)} variant="outline">
            <Plus className="h-4 w-4" />
            Despesa rápida
          </Button>
          <Button className="gap-2" onClick={openNewExpenseDialog}>
            <Plus className="h-4 w-4" />
            Adicionar despesa
          </Button>
          <Button className="gap-2" onClick={openNewSubscriptionDialog}>
            <Plus className="h-4 w-4" />
            Adicionar assinatura
          </Button>
        </div>
      </SectionShell>

      <div className="grid gap-6 xl:grid-cols-[2fr_3fr]">
        <SectionShell title="Receitas do mês" description="Clique no badge de status para marcar como pago/pendente.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow><TableHead>Nome</TableHead><TableHead>Valor</TableHead><TableHead>Dia pagamento</TableHead><TableHead>Recorrente</TableHead><TableHead>Status (clicável)</TableHead><TableHead className="text-right">Ações</TableHead></TableRow></TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : monthView?.incomes.length ? monthView.incomes.map((item) => {
                  const pay = getEffectivePayDay(item.dayOfMonth, period.year, period.month);
                  return (
                    <TableRow key={item.id}>
                      <TableCell><div className="space-y-1"><p className="font-medium">{item.name}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                      <TableCell>{formatCurrency(item.amount)}</TableCell>
                      <TableCell>
                        <div className="space-y-0.5">
                          <p className="text-sm font-medium">{pay.label}</p>
                          {pay.adjusted && <p className="text-xs text-amber-600">Ajustado (fim de semana)</p>}
                          {item.paymentDate && (() => {
                            const pd = new Date(item.paymentDate);
                            const actualDay = pd.getUTCDate();
                            if (actualDay !== pay.effectiveDay) {
                              const d = pd.getUTCDate().toString().padStart(2, '0');
                              const m = (pd.getUTCMonth() + 1).toString().padStart(2, '0');
                              return <p className="text-xs text-sky-600 font-medium">Pago em {d}/{m}</p>;
                            }
                            return null;
                          })()}
                        </div>
                      </TableCell>
                      <TableCell>
                        {item.isRecurring
                          ? <Badge variant="outline" className="border-emerald-200 text-emerald-700">Recorrente</Badge>
                          : <Badge variant="outline" className="border-orange-200 bg-orange-50 text-orange-700">Encerrado</Badge>
                        }
                      </TableCell>
                      <TableCell>
                        <StatusBadge
                          paid={item.isPaid}
                          loading={savingKey === `income-${item.id}`}
                          onClick={() => saveStatus('income', item.id, !item.isPaid)}
                        />
                      </TableCell>
                      <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editIncome(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'income', id: item.id, name: item.name })} disabled={savingKey === `delete-income-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                    </TableRow>
                  );
                }) : <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Nenhuma receita encontrada.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>

        <SectionShell title="Despesas do mês" description="Débito, crédito, vencimentos e cartão vinculado.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow><TableHead>Nome</TableHead><TableHead>Valor</TableHead><TableHead>Tipo</TableHead><TableHead>Venc.</TableHead><TableHead>Cartão</TableHead><TableHead>Status</TableHead><TableHead className="text-right">Ações</TableHead></TableRow></TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : monthView?.expenses.length ? monthView.expenses.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell><div className="space-y-1"><p className="font-medium flex items-center gap-1">{item.name}{item.hasVariableAmount && <span title="Valor variável — verificar mês a mês" className="inline-flex items-center rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">~valor</span>}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell>{item.paymentType === 'credit' ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 gap-1 hover:bg-blue-50"><CreditCardIcon className="h-3 w-3" />Crédito</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 gap-1 hover:bg-emerald-50"><Banknote className="h-3 w-3" />PIX/Déb</Badge>}</TableCell>
                    <TableCell>Dia {item.dueDay}</TableCell>
                    <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 hover:bg-blue-50"><CreditCardIcon className="mr-1 h-3 w-3 inline" />{item.creditCardName}</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-50"><Banknote className="mr-1 h-3 w-3 inline" />PIX / Débito</Badge>}</TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} loading={savingKey === `expense-${item.id}`} onClick={() => saveStatus('expense', item.id, !item.isPaid)} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editExpense(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'expense', id: item.id, name: item.name })} disabled={savingKey === `delete-expense-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Nenhuma despesa encontrada.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <SectionShell title="Assinaturas" description="Serviços recorrentes consolidados por mês.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow><TableHead>Nome</TableHead><TableHead>Valor</TableHead><TableHead>Frequência</TableHead><TableHead>Tipo</TableHead><TableHead>Cartão</TableHead><TableHead>Status</TableHead><TableHead className="text-right">Ações</TableHead></TableRow></TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : monthView?.subscriptions.length ? monthView.subscriptions.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell><div className="space-y-1"><p className="font-medium flex items-center gap-1">{item.name}{item.hasVariableAmount && <span title="Valor variável — verificar mês a mês" className="inline-flex items-center rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">~valor</span>}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell><Badge variant="outline">{billingFrequencyOptions.find((option) => option.value === item.billingFrequency)?.label || item.billingFrequency}</Badge></TableCell>
                    <TableCell>{item.paymentType === 'credit' ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 gap-1 hover:bg-blue-50"><CreditCardIcon className="h-3 w-3" />Crédito</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 gap-1 hover:bg-emerald-50"><Banknote className="h-3 w-3" />PIX/Déb</Badge>}</TableCell>
                    <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 hover:bg-blue-50"><CreditCardIcon className="mr-1 h-3 w-3 inline" />{item.creditCardName}</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-50"><Banknote className="mr-1 h-3 w-3 inline" />PIX / Débito</Badge>}</TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} loading={savingKey === `subscription-${item.id}`} onClick={() => saveStatus('subscription', item.id, !item.isPaid)} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editSubscription(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'subscription', id: item.id, name: item.name })} disabled={savingKey === `delete-subscription-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Nenhuma assinatura encontrada.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>

        <Card className="shadow-sm">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between gap-2">
              <div>
                <CardTitle className="text-lg">Cartões do mês</CardTitle>
                <CardDescription>Agregação mensal das despesas por cartão.</CardDescription>
              </div>
              <Button
                variant="outline"
                size="sm"
                className="gap-2"
                onClick={() => void importFromSheet()}
                disabled={importingSheet}
              >
                <Download className="h-4 w-4" />
                {importingSheet ? 'Importando...' : 'Importar da planilha'}
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Cartão</TableHead>
                    <TableHead>Fatura</TableHead>
                    <TableHead>Limite</TableHead>
                    <TableHead>Disponível</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Itens</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? (
                    <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Carregando cartões...</TableCell></TableRow>
                  ) : monthView?.cardSummaries.length ? monthView.cardSummaries.map((item) => (
                    <TableRow key={item.creditCardId}>
                      <TableCell className="font-medium">{item.creditCardName}</TableCell>
                      <TableCell>
                        {editingStatementId === item.invoiceId && item.invoiceId ? (
                          <div className="flex items-center gap-2">
                            <Input
                              className="w-28 h-7 text-sm"
                              value={statementDraft}
                              onChange={(e) => setStatementDraft(e.target.value)}
                              placeholder="0,00"
                              onKeyDown={(e) => {
                                if (e.key === 'Enter') void saveStatementAmount(item.invoiceId!);
                                if (e.key === 'Escape') setEditingStatementId(null);
                              }}
                            />
                            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => void saveStatementAmount(item.invoiceId!)} disabled={savingKey === `statement-${item.invoiceId}`}>
                              Salvar
                            </Button>
                            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => setEditingStatementId(null)}>
                              Cancelar
                            </Button>
                          </div>
                        ) : (
                          <div className="flex items-center gap-2">
                            <span className="tabular-nums">
                              {item.statementAmount != null
                                ? formatCurrency(item.statementAmount)
                                : formatCurrency(item.totalAmount)}
                            </span>
                            {item.invoiceId && (
                              <button
                                className="text-muted-foreground hover:text-foreground transition-colors"
                                onClick={() => {
                                  setEditingStatementId(item.invoiceId!);
                                  setStatementDraft(
                                    item.statementAmount != null
                                      ? String(item.statementAmount)
                                      : String(item.totalAmount)
                                  );
                                }}
                              >
                                <PencilLine className="h-3.5 w-3.5" />
                              </button>
                            )}
                          </div>
                        )}
                      </TableCell>
                      <TableCell className="tabular-nums text-sm">
                        {item.creditLimit ? formatCurrency(item.creditLimit) : <span className="text-muted-foreground text-xs">—</span>}
                      </TableCell>
                      <TableCell>
                        {item.creditLimit ? (
                          <div className="flex flex-col gap-1 min-w-[100px]">
                            <span className={cn('tabular-nums text-sm font-medium', (item.availableCredit ?? 0) >= 0 ? 'text-emerald-700' : 'text-rose-600')}>
                              {formatCurrency(item.availableCredit ?? 0)}
                            </span>
                            <div className="h-1.5 w-full rounded-full bg-slate-100 overflow-hidden">
                              <div
                                className={cn('h-full rounded-full', (item.availableCredit ?? 0) >= 0 ? 'bg-emerald-400' : 'bg-rose-400')}
                                style={{ width: `${Math.min(100, Math.max(0, ((item.creditLimit - (item.availableCredit ?? 0)) / item.creditLimit) * 100))}%` }}
                              />
                            </div>
                            <span className="text-[10px] text-muted-foreground">
                              {Math.round(((item.creditLimit - (item.availableCredit ?? 0)) / item.creditLimit) * 100)}% usado
                            </span>
                          </div>
                        ) : <span className="text-muted-foreground text-xs">—</span>}
                      </TableCell>
                      <TableCell>
                        {item.invoicePaid ? (
                          <Badge className="border-emerald-200 bg-emerald-50 text-emerald-700">Pago ✓</Badge>
                        ) : item.invoiceId ? (
                          payingInvoiceId === item.invoiceId ? (
                            <div className="flex items-center gap-2">
                              <Input
                                type="date"
                                className="w-36 h-7 text-sm"
                                defaultValue={new Date().toISOString().slice(0, 10)}
                                id={`pay-date-${item.invoiceId}`}
                              />
                              <Button size="sm" className="h-7 px-2" onClick={() => {
                                const input = document.getElementById(`pay-date-${item.invoiceId}`) as HTMLInputElement;
                                const date = input?.value ? new Date(input.value).toISOString() : new Date().toISOString();
                                void payInvoice(item.invoiceId!, date);
                              }} disabled={savingKey === `pay-invoice-${item.invoiceId}`}>
                                Confirmar
                              </Button>
                              <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => setPayingInvoiceId(null)}>
                                Cancelar
                              </Button>
                            </div>
                          ) : (
                            <Button
                              size="sm"
                              variant="outline"
                              className="h-7 text-xs"
                              onClick={() => setPayingInvoiceId(item.invoiceId!)}
                            >
                              Marcar Pago
                            </Button>
                          )
                        ) : (
                          <Badge variant="outline" className="text-muted-foreground">Sem fatura</Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{item.itemCount} lançamentos</Badge>
                          <label className="cursor-pointer" title="Importar fatura PDF com IA">
                            <input
                              type="file"
                              accept=".pdf"
                              className="sr-only"
                              onChange={(e) => {
                                const file = e.target.files?.[0];
                                if (file) void handlePdfUpload(file, { creditCardId: item.creditCardId, creditCardName: item.creditCardName });
                                e.target.value = '';
                              }}
                            />
                            <Button size="sm" variant="ghost" className="h-7 px-2 pointer-events-none" asChild>
                              <span><Upload className="h-3.5 w-3.5" /></span>
                            </Button>
                          </label>
                        </div>
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Nenhum cartão com lançamentos neste mês.</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Feature 2 — PDF Statement Import Modal */}
      <Dialog open={!!pdfImportCard && !pdfParsing} onOpenChange={(open) => { if (!open) { setPdfImportCard(null); setParsedTxList([]); } }}>
        <DialogContent className="sm:max-w-2xl max-h-[80vh] flex flex-col">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-blue-600" />
              Importar fatura — {pdfImportCard?.creditCardName}
            </DialogTitle>
            <DialogDescription>
              Selecione as transações que deseja importar como despesas de {months[period.month - 1]} {period.year}.
            </DialogDescription>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto">
            {parsedTxList.length === 0 ? (
              <p className="text-sm text-muted-foreground py-8 text-center">Nenhuma transação encontrada.</p>
            ) : (
              <div className="space-y-1">
                <div className="flex items-center gap-2 pb-2 border-b">
                  <input
                    type="checkbox"
                    className="rounded border-gray-300"
                    checked={parsedTxList.every((t) => t.selected)}
                    onChange={(e) => setParsedTxList((prev) => prev.map((t) => ({ ...t, selected: e.target.checked })))}
                  />
                  <span className="text-xs text-muted-foreground font-medium">Selecionar todas ({parsedTxList.filter((t) => t.selected).length}/{parsedTxList.length})</span>
                </div>
                {parsedTxList.map((tx, i) => (
                  <label key={i} className="flex items-center gap-3 py-2 px-1 rounded hover:bg-muted/50 cursor-pointer">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 shrink-0"
                      checked={tx.selected}
                      onChange={(e) => setParsedTxList((prev) => prev.map((t, idx) => idx === i ? { ...t, selected: e.target.checked } : t))}
                    />
                    <span className="flex-1 text-sm truncate">{tx.description}</span>
                    <span className="text-sm tabular-nums text-rose-600 font-medium">{formatCurrency(tx.amount)}</span>
                    <span className="text-xs text-muted-foreground shrink-0">{tx.date}</span>
                  </label>
                ))}
              </div>
            )}
          </div>
          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => { setPdfImportCard(null); setParsedTxList([]); }}>Cancelar</Button>
            <Button
              onClick={() => void importSelectedPdfTxs()}
              disabled={pdfImporting || parsedTxList.filter((t) => t.selected).length === 0}
            >
              {pdfImporting ? 'Importando...' : `Importar ${parsedTxList.filter((t) => t.selected).length} despesa${parsedTxList.filter((t) => t.selected).length !== 1 ? 's' : ''}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* PDF Parsing Loading Overlay */}
      <Dialog open={pdfParsing} onOpenChange={() => {}}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-blue-600 animate-pulse" />
              Analisando PDF com IA...
            </DialogTitle>
            <DialogDescription>Aguarde enquanto extraímos as transações da fatura.</DialogDescription>
          </DialogHeader>
        </DialogContent>
      </Dialog>

      {/* FASE 2C — Despesa Rápida */}
      <Dialog open={quickExpenseOpen} onOpenChange={setQuickExpenseOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nova Despesa Rápida</DialogTitle>
            <DialogDescription>Lance uma despesa sem precisar selecionar o período.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitQuickExpense} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="qe-name">Descrição</Label>
              <Input
                id="qe-name"
                placeholder="Ex: Mercado, Uber, Farmácia..."
                value={quickExpense.name}
                onChange={(e) => setQuickExpense((c) => ({ ...c, name: e.target.value }))}
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="qe-amount">Valor (R$)</Label>
                <Input
                  id="qe-amount"
                  placeholder="0,00"
                  value={quickExpense.amount}
                  onChange={(e) => setQuickExpense((c) => ({ ...c, amount: e.target.value }))}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="qe-date">Data</Label>
                <Input
                  id="qe-date"
                  type="date"
                  value={quickExpense.date}
                  onChange={(e) => setQuickExpense((c) => ({ ...c, date: e.target.value }))}
                  required
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Tipo de pagamento</Label>
              <div className="grid grid-cols-2 gap-2">
                {([
                  { value: 'pix', label: 'PIX' },
                  { value: 'debit', label: 'Débito em Conta' },
                  { value: 'credit', label: 'Cartão de Crédito' },
                  { value: 'auto', label: 'Débito Automático' },
                ] as { value: QuickPaymentType; label: string }[]).map(({ value, label }) => (
                  <button
                    key={value}
                    type="button"
                    onClick={() => setQuickExpense((c) => ({ ...c, paymentKind: value }))}
                    className={cn(
                      'rounded-lg border px-3 py-2 text-sm font-medium text-left transition-colors',
                      quickExpense.paymentKind === value
                        ? 'border-slate-900 bg-slate-900 text-white'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
            {quickExpense.paymentKind === 'credit' && (
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select
                  value={quickExpense.creditCardId || 'none'}
                  onValueChange={(v) => setQuickExpense((c) => ({ ...c, creditCardId: v === 'none' ? '' : v }))}
                >
                  <SelectTrigger><SelectValue placeholder="Selecionar cartão" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem cartão</SelectItem>
                    {creditCards.map((card) => (
                      <SelectItem key={card.id} value={card.id}>{card.name} •{card.lastFourDigits}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <DialogFooter>
              <Button type="button" variant="ghost" onClick={() => setQuickExpenseOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={savingKey === 'quick-expense'} className="gap-2">
                <Plus className="h-4 w-4" />
                {savingKey === 'quick-expense' ? 'Criando...' : 'Criar despesa'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={incomeDialogOpen} onOpenChange={setIncomeDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{incomeForm.editingId ? 'Editar receita' : 'Nova receita'}</DialogTitle>
            <DialogDescription>Receitas fixas ou recorrentes do mês.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitIncome} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="income-name">Nome</Label><Input id="income-name" value={incomeForm.name} onChange={(event) => setIncomeForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="income-amount">Valor</Label><Input id="income-amount" type="number" min="0" step="0.01" value={incomeForm.amount} onChange={(event) => setIncomeForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2"><Label htmlFor="income-day">Dia previsto</Label><Input id="income-day" type="number" min="1" max="31" value={incomeForm.dayOfMonth} onChange={(event) => setIncomeForm((current) => ({ ...current, dayOfMonth: Number(event.target.value) }))} /></div>
            </div>
            <div className="space-y-2"><Label htmlFor="income-details">Detalhes / Observações</Label><Input id="income-details" value={incomeForm.details || ''} onChange={(event) => setIncomeForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <div className="space-y-3">
              <label className="flex cursor-pointer items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm">
                <input type="checkbox" checked={Boolean(incomeForm.isPaid)} onChange={(event) => setIncomeForm((current) => ({ ...current, isPaid: event.target.checked, paymentDate: event.target.checked ? (current.paymentDate || new Date().toISOString().split('T')[0] + 'T00:00:00Z') : undefined }))} />
                Pago
              </label>
              {incomeForm.isPaid && (
                <div className="space-y-1 pl-1">
                  <Label htmlFor="income-payment-date">Data do pagamento</Label>
                  <Input
                    id="income-payment-date"
                    type="date"
                    value={incomeForm.paymentDate ? incomeForm.paymentDate.split('T')[0] : new Date().toISOString().split('T')[0]}
                    onChange={(event) => setIncomeForm((current) => ({ ...current, paymentDate: event.target.value ? event.target.value + 'T00:00:00Z' : undefined }))}
                  />
                  <p className="text-xs text-muted-foreground">Preencha se pago em data diferente do dia previsto (ex: pagamento antecipado).</p>
                </div>
              )}
            </div>
            <label className="flex cursor-pointer items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm">
              <input type="checkbox" checked={Boolean(incomeForm.isRecurring)} onChange={(event) => setIncomeForm((current) => ({ ...current, isRecurring: event.target.checked }))} />
              <span>Receita recorrente</span>
              {!incomeForm.isRecurring && <span className="ml-auto text-xs font-medium text-orange-600">Contrato encerrado</span>}
            </label>
            <DialogFooter>
              {incomeForm.editingId ? <Button type="button" variant="outline" onClick={() => setIncomeForm(incomeFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'income'}><Plus className="h-4 w-4" />{savingKey === 'income' ? 'Salvando...' : incomeForm.editingId ? 'Atualizar receita' : 'Criar receita'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={expenseDialogOpen} onOpenChange={setExpenseDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{expenseForm.editingId ? 'Editar despesa' : 'Nova despesa'}</DialogTitle>
            <DialogDescription>Controle de débito, crédito e vencimentos.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitExpense} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="expense-name">Nome</Label><Input id="expense-name" value={expenseForm.name} onChange={(event) => setExpenseForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="expense-amount">Valor</Label><Input id="expense-amount" type="number" min="0" step="0.01" value={expenseForm.amount} onChange={(event) => setExpenseForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2"><Label htmlFor="expense-due">Dia do vencimento</Label><Input id="expense-due" type="number" min="1" max="31" value={expenseForm.dueDay} onChange={(event) => setExpenseForm((current) => ({ ...current, dueDay: Number(event.target.value) }))} /></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Pagamento</Label>
                <Select value={expenseForm.paymentType} onValueChange={(value) => setExpenseForm((current) => ({ ...current, paymentType: value as PersonalControlPaymentType }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{paymentTypeOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select value={expenseForm.paymentType === 'credit' ? expenseForm.creditCardId || 'none' : 'none'} onValueChange={(value) => setExpenseForm((current) => ({ ...current, creditCardId: value === 'none' ? '' : value }))}>
                  <SelectTrigger><SelectValue placeholder="Sem vínculo" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem vínculo</SelectItem>
                    {creditCards.map((card) => <SelectItem key={card.id} value={card.id}>{card.name} • {card.lastFourDigits}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label htmlFor="expense-details">Detalhes</Label><Input id="expense-details" value={expenseForm.details || ''} onChange={(event) => setExpenseForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-2 cursor-pointer select-none text-sm">
              <input type="checkbox" className="rounded border-gray-300" checked={!!expenseForm.hasVariableAmount} onChange={(e) => setExpenseForm((c) => ({ ...c, hasVariableAmount: e.target.checked }))} />
              <span className="text-muted-foreground">Valor variável mês a mês <span className="text-amber-600 font-medium">(ex: plano de saúde, condomínio)</span></span>
            </label>
            <DialogFooter>
              {expenseForm.editingId ? <Button type="button" variant="outline" onClick={() => setExpenseForm(expenseFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'expense'}><Plus className="h-4 w-4" />{savingKey === 'expense' ? 'Salvando...' : expenseForm.editingId ? 'Atualizar despesa' : 'Criar despesa'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={subscriptionDialogOpen} onOpenChange={setSubscriptionDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{subscriptionForm.editingId ? 'Editar assinatura' : 'Nova assinatura'}</DialogTitle>
            <DialogDescription>Serviços recorrentes e mensalidades.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitSubscription} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="subscription-name">Nome</Label><Input id="subscription-name" value={subscriptionForm.name} onChange={(event) => setSubscriptionForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="subscription-amount">Valor</Label><Input id="subscription-amount" type="number" min="0" step="0.01" value={subscriptionForm.amount} onChange={(event) => setSubscriptionForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2">
                <Label>Frequência</Label>
                <Select value={subscriptionForm.billingFrequency} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, billingFrequency: value as PersonalControlBillingFrequency }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{billingFrequencyOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Pagamento</Label>
                <Select value={subscriptionForm.paymentType} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, paymentType: value as PersonalControlPaymentType }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{paymentTypeOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select value={subscriptionForm.paymentType === 'credit' ? subscriptionForm.creditCardId || 'none' : 'none'} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, creditCardId: value === 'none' ? '' : value }))}>
                  <SelectTrigger><SelectValue placeholder="Sem vínculo" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem vínculo</SelectItem>
                    {creditCards.map((card) => <SelectItem key={card.id} value={card.id}>{card.name} • {card.lastFourDigits}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label htmlFor="subscription-details">Detalhes</Label><Input id="subscription-details" value={subscriptionForm.details || ''} onChange={(event) => setSubscriptionForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-2 cursor-pointer select-none text-sm">
              <input type="checkbox" className="rounded border-gray-300" checked={!!subscriptionForm.hasVariableAmount} onChange={(e) => setSubscriptionForm((c) => ({ ...c, hasVariableAmount: e.target.checked }))} />
              <span className="text-muted-foreground">Valor variável mês a mês <span className="text-amber-600 font-medium">(ex: plano de saúde, condomínio)</span></span>
            </label>
            <DialogFooter>
              {subscriptionForm.editingId ? <Button type="button" variant="outline" onClick={() => setSubscriptionForm(subscriptionFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'subscription'}><Plus className="h-4 w-4" />{savingKey === 'subscription' ? 'Salvando...' : subscriptionForm.editingId ? 'Atualizar assinatura' : 'Criar assinatura'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={Boolean(deleteDialog)} onOpenChange={(open) => !open && setDeleteDialog(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Excluir registro</DialogTitle>
            <DialogDescription>
              {deleteDialog ? `Tem certeza que deseja excluir "${deleteDialog.name}"? Essa ação não pode ser desfeita.` : ''}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDeleteDialog(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={confirmDelete}
              disabled={deleteDialog ? savingKey === `delete-${deleteDialog.kind}-${deleteDialog.id}` : false}
            >
              {deleteDialog && savingKey === `delete-${deleteDialog.kind}-${deleteDialog.id}` ? 'Excluindo...' : 'Excluir'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default function PersonalControlPage() {
  return <Page />;
}
