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
  PersonalControlBillingFrequency,
  PersonalControlExpenseItem,
  PersonalControlIncomeItem,
  PersonalControlMonthView,
  PersonalControlPaymentType,
  PersonalControlSubscriptionItem,
  TogglePersonalControlStatusRequest,
  personalControlService,
} from '@/services/personalControlService';
import {
  ArrowDownCircle,
  ArrowLeft,
  ArrowRight,
  ArrowUpCircle,
  Building2,
  Calendar,
  CheckCircle2,
  CreditCard as CreditCardIcon,
  PencilLine,
  Plus,
  RotateCcw,
  TrendingUp,
  Trash2,
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
    editingId: null,
  };
}

function StatusBadge({ paid }: { paid: boolean }) {
  return paid ? (
    <Badge className="border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-50">Pago</Badge>
  ) : (
    <Badge className="border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-50">Pendente</Badge>
  );
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

function PatrimonioWidget({ accounts }: { accounts: FinancialAccount[] }) {
  if (accounts.length === 0) return null;
  const total = accounts.reduce((sum, a) => sum + a.balance, 0);
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
          <span className={cn('font-semibold tabular-nums', a.balance >= 0 ? 'text-emerald-700' : 'text-rose-600')}>
            {formatCurrency(a.balance)}
          </span>
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
  salaryDates,
  onChangeDates,
}: {
  monthView: PersonalControlMonthView;
  salaryDates: number[];
  onChangeDates: (dates: number[]) => void;
}) {
  const [editingDates, setEditingDates] = useState(false);
  const [draftDates, setDraftDates] = useState(salaryDates.join(', '));

  const saveDates = () => {
    const parsed = draftDates
      .split(',')
      .map((s) => parseInt(s.trim(), 10))
      .filter((n) => !isNaN(n) && n >= 1 && n <= 31)
      .sort((a, b) => a - b);
    if (parsed.length > 0) {
      onChangeDates(parsed);
      setEditingDates(false);
    }
  };

  const buckets = salaryDates.map((day, i) => {
    const nextDay = salaryDates[i + 1] ?? 32;
    const incomes = monthView.incomes.filter((item) => item.dayOfMonth >= day && item.dayOfMonth < nextDay);
    const expenses = monthView.expenses.filter((item) => item.dueDay >= day && item.dueDay < nextDay);
    const subscriptions = i === 0 ? monthView.subscriptions : ([] as typeof monthView.subscriptions);
    const totalIncome = incomes.reduce((s, item) => s + item.amount, 0);
    const totalExpense =
      expenses.reduce((s, item) => s + item.amount, 0) +
      subscriptions.reduce((s, item) => s + item.amount, 0);
    const balance = totalIncome - totalExpense;
    const investSuggestion = balance > 0 ? balance * 0.2 : 0;
    return { day, nextDay, incomes, expenses, subscriptions, totalIncome, totalExpense, balance, investSuggestion };
  });

  return (
    <Card className="shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-3">
          <div>
            <CardTitle className="text-lg">Planner de Salário</CardTitle>
            <CardDescription>Distribuição das contas por data de recebimento.</CardDescription>
          </div>
          {editingDates ? (
            <div className="flex items-center gap-2">
              <Input
                className="h-8 w-40 text-xs"
                value={draftDates}
                onChange={(e) => setDraftDates(e.target.value)}
                placeholder="1, 4, 10, 15"
              />
              <Button size="sm" onClick={saveDates}>Salvar</Button>
              <Button size="sm" variant="ghost" onClick={() => setEditingDates(false)}>Cancelar</Button>
            </div>
          ) : (
            <Button size="sm" variant="outline" onClick={() => { setDraftDates(salaryDates.join(', ')); setEditingDates(true); }}>
              Editar datas
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {buckets.map(({ day, nextDay, incomes, expenses, subscriptions, totalIncome, totalExpense, balance, investSuggestion }) => (
            <div key={day} className={cn('flex flex-col gap-3 rounded-2xl border p-4', balance >= 0 ? 'bg-emerald-50/40 border-emerald-100' : 'bg-rose-50/40 border-rose-100')}>
              <div className="flex items-center justify-between">
                <span className="text-sm font-semibold text-slate-700">
                  Dia {day}{nextDay < 32 ? `–${nextDay - 1}` : '+'}
                </span>
                <span className={cn('text-xs font-bold tabular-nums', balance >= 0 ? 'text-emerald-700' : 'text-rose-600')}>
                  {balance >= 0 ? '+' : ''}{formatCurrency(balance)}
                </span>
              </div>

              {incomes.length > 0 && (
                <div className="space-y-1">
                  <p className="text-[11px] font-semibold uppercase tracking-wide text-emerald-600">Entradas</p>
                  {incomes.map((item) => (
                    <div key={item.id} className="flex justify-between text-xs text-slate-700">
                      <span className="truncate max-w-[110px]">{item.name}</span>
                      <span className="tabular-nums font-medium">{formatCurrency(item.amount)}</span>
                    </div>
                  ))}
                </div>
              )}

              {(expenses.length > 0 || subscriptions.length > 0) && (
                <div className="space-y-1">
                  <p className="text-[11px] font-semibold uppercase tracking-wide text-rose-500">Saídas</p>
                  {expenses.map((item) => (
                    <div key={item.id} className="flex justify-between text-xs text-slate-600">
                      <span className="truncate max-w-[110px]">{item.name}</span>
                      <span className="tabular-nums">{formatCurrency(item.amount)}</span>
                    </div>
                  ))}
                  {subscriptions.map((item) => (
                    <div key={item.id} className="flex justify-between text-xs text-slate-600">
                      <span className="truncate max-w-[110px]">{item.name}</span>
                      <span className="tabular-nums">{formatCurrency(item.amount)}</span>
                    </div>
                  ))}
                </div>
              )}

              <div className="mt-auto space-y-1 border-t border-current/10 pt-2">
                <div className="flex justify-between text-xs text-slate-500">
                  <span>Receitas</span><span className="text-emerald-600 font-medium">{formatCurrency(totalIncome)}</span>
                </div>
                <div className="flex justify-between text-xs text-slate-500">
                  <span>Despesas</span><span className="text-rose-500 font-medium">{formatCurrency(totalExpense)}</span>
                </div>
                {investSuggestion > 0 && (
                  <div className="flex items-center justify-between rounded-lg bg-sky-50 px-2 py-1 text-xs text-sky-700">
                    <div className="flex items-center gap-1"><TrendingUp className="h-3 w-3" />Investir 20%</div>
                    <span className="font-semibold">{formatCurrency(investSuggestion)}</span>
                  </div>
                )}
              </div>
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
  const [salaryDates, setSalaryDates] = useState<number[]>([1, 4, 10, 15]);
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

      <PatrimonioWidget accounts={accounts} />

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

      {monthView && (
        <SalaryPlannerSection
          monthView={monthView}
          salaryDates={salaryDates}
          onChangeDates={setSalaryDates}
        />
      )}

      <SectionShell title="Ações" description="Abra os formulários só quando precisar criar ou editar um lançamento.">
        <div className="flex flex-wrap gap-3">
          <Button className="gap-2" onClick={openNewIncomeDialog}>
            <Plus className="h-4 w-4" />
            Adicionar receita
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

      <div className="grid gap-6 xl:grid-cols-2">
        <SectionShell title="Receitas do mês" description="Lançamentos do período selecionado.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow><TableHead>Nome</TableHead><TableHead>Valor</TableHead><TableHead>Dia</TableHead><TableHead>Recorrente</TableHead><TableHead>Status</TableHead><TableHead className="text-right">Ações</TableHead></TableRow></TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : monthView?.incomes.length ? monthView.incomes.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell><div className="space-y-1"><p className="font-medium">{item.name}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell>Dia {item.dayOfMonth}</TableCell>
                    <TableCell><Badge variant="outline">{item.isRecurring ? 'Sim' : 'Não'}</Badge></TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editIncome(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('income', item.id, !item.isPaid)} disabled={savingKey === `income-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'income', id: item.id, name: item.name })} disabled={savingKey === `delete-income-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Nenhuma receita encontrada.</TableCell></TableRow>}
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
                    <TableCell><div className="space-y-1"><p className="font-medium">{item.name}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell><Badge variant="outline">{item.paymentType === 'credit' ? 'Crédito' : 'Débito'}</Badge></TableCell>
                    <TableCell>Dia {item.dueDay}</TableCell>
                    <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 hover:bg-blue-50">{item.creditCardName}</Badge> : <span className="text-sm text-muted-foreground">Sem cartão</span>}</TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editExpense(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('expense', item.id, !item.isPaid)} disabled={savingKey === `expense-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'expense', id: item.id, name: item.name })} disabled={savingKey === `delete-expense-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
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
                    <TableCell><div className="space-y-1"><p className="font-medium">{item.name}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell><Badge variant="outline">{billingFrequencyOptions.find((option) => option.value === item.billingFrequency)?.label || item.billingFrequency}</Badge></TableCell>
                    <TableCell><Badge variant="outline">{item.paymentType === 'credit' ? 'Crédito' : 'Débito'}</Badge></TableCell>
                    <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 hover:bg-blue-50">{item.creditCardName}</Badge> : <span className="text-sm text-muted-foreground">Sem cartão</span>}</TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editSubscription(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('subscription', item.id, !item.isPaid)} disabled={savingKey === `subscription-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'subscription', id: item.id, name: item.name })} disabled={savingKey === `delete-subscription-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Nenhuma assinatura encontrada.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>

        <SectionShell title="Cartões do mês" description="Agregação mensal das despesas por cartão.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow><TableHead>Cartão</TableHead><TableHead>Total</TableHead><TableHead>Pago</TableHead><TableHead>Pendente</TableHead><TableHead>Itens</TableHead></TableRow></TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={5} className="py-12 text-center text-muted-foreground">Carregando cartões...</TableCell></TableRow> : monthView?.cardSummaries.length ? monthView.cardSummaries.map((item) => (
                  <TableRow key={item.creditCardId}>
                    <TableCell className="font-medium">{item.creditCardName}</TableCell>
                    <TableCell>{formatCurrency(item.totalAmount)}</TableCell>
                    <TableCell>{formatCurrency(item.paidAmount)}</TableCell>
                    <TableCell>{formatCurrency(item.pendingAmount)}</TableCell>
                    <TableCell><Badge variant="outline">{item.itemCount} lançamentos</Badge></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={5} className="py-12 text-center text-muted-foreground">Nenhum cartão com lançamentos neste mês.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>
      </div>

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
              <div className="space-y-2"><Label htmlFor="income-day">Dia</Label><Input id="income-day" type="number" min="1" max="31" value={incomeForm.dayOfMonth} onChange={(event) => setIncomeForm((current) => ({ ...current, dayOfMonth: Number(event.target.value) }))} /></div>
            </div>
            <div className="space-y-2"><Label htmlFor="income-details">Detalhes</Label><Input id="income-details" value={incomeForm.details || ''} onChange={(event) => setIncomeForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm"><input type="checkbox" checked={Boolean(incomeForm.isRecurring)} onChange={(event) => setIncomeForm((current) => ({ ...current, isRecurring: event.target.checked }))} />Receita recorrente</label>
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
