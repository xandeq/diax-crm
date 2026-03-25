'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { cn, formatCurrency } from '@/lib/utils';
import { financeService, type CreditCard } from '@/services/finance';
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
  Calendar,
  CheckCircle2,
  CreditCard as CreditCardIcon,
  PencilLine,
  Plus,
  RotateCcw,
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
  return { name: '', amount: 0, dayOfMonth: new Date().getDate(), isRecurring: true, details: '', editingId: null };
}

function expenseFormReset(): EditingState<CreatePersonalControlExpenseRequest> {
  return {
    name: '',
    amount: 0,
    paymentType: 'debit',
    dueDay: new Date().getDate(),
    creditCardId: '',
    details: '',
    editingId: null,
  };
}

function subscriptionFormReset(): EditingState<CreatePersonalControlSubscriptionRequest> {
  return {
    name: '',
    amount: 0,
    billingFrequency: 'monthly',
    paymentType: 'credit',
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

function Page() {
  const [period, setPeriod] = useState(currentPeriod);
  const [isPending, startTransition] = useTransition();
  const [loading, setLoading] = useState(true);
  const [monthView, setMonthView] = useState<PersonalControlMonthView | null>(null);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [incomeForm, setIncomeForm] = useState(incomeFormReset);
  const [expenseForm, setExpenseForm] = useState(expenseFormReset);
  const [subscriptionForm, setSubscriptionForm] = useState(subscriptionFormReset);

  const loadMonth = async (year: number, month: number) => {
    setLoading(true);
    try {
      const [view, cards] = await Promise.all([
        personalControlService.getMonthView(year, month),
        financeService.getCreditCards(),
      ]);
      setMonthView(view);
      setCreditCards(cards);
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

  const removeItem = async (kind: 'income' | 'expense' | 'subscription', id: string) => {
    if (!window.confirm('Excluir este lançamento? A ação não pode ser desfeita.')) return;

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
        name: incomeForm.name,
        amount: Number(incomeForm.amount),
        dayOfMonth: Number(incomeForm.dayOfMonth),
        isRecurring: Boolean(incomeForm.isRecurring),
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
        name: expenseForm.name,
        amount: Number(expenseForm.amount),
        paymentType: expenseForm.paymentType,
        dueDay: Number(expenseForm.dueDay),
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
        name: subscriptionForm.name,
        amount: Number(subscriptionForm.amount),
        billingFrequency: subscriptionForm.billingFrequency,
        paymentType: subscriptionForm.paymentType,
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
      name: item.name,
      amount: item.amount,
      dayOfMonth: item.dayOfMonth,
      isRecurring: item.isRecurring,
      details: item.details || '',
      editingId: item.id,
    });
  };

  const editExpense = (item: PersonalControlExpenseItem) => {
    setExpenseForm({
      name: item.name,
      amount: item.amount,
      paymentType: item.paymentType,
      dueDay: item.dueDay,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      editingId: item.id,
    });
  };

  const editSubscription = (item: PersonalControlSubscriptionItem) => {
    setSubscriptionForm({
      name: item.name,
      amount: item.amount,
      billingFrequency: item.billingFrequency,
      paymentType: item.paymentType,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      editingId: item.id,
    });
  };

  const summary = monthView?.summary;

  return (
    <div className="p-8 max-w-[1700px] mx-auto space-y-8">
      <div className="rounded-3xl border bg-gradient-to-br from-slate-950 via-slate-900 to-slate-800 p-8 text-white shadow-xl">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-3">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-3 py-1 text-xs font-medium text-white/80">
              <Wallet className="h-3.5 w-3.5" />
              Controle financeiro pessoal mensal
            </div>
            <h1 className="text-3xl font-bold tracking-tight md:text-4xl">Visão mensal estilo planilha</h1>
            <p className="max-w-2xl text-sm text-white/70 md:text-base">
              Receitas, despesas, assinaturas e cartões em uma única tela, com marcação de pago/pendente e
              resumo consolidado por mês.
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

      <div className="grid gap-6 xl:grid-cols-3">
        <SectionShell title={incomeForm.editingId ? 'Editar receita' : 'Nova receita'} description="Receitas fixas ou recorrentes do mês.">
          <form onSubmit={submitIncome} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="income-name">Nome</Label><Input id="income-name" value={incomeForm.name} onChange={(event) => setIncomeForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="income-amount">Valor</Label><Input id="income-amount" type="number" min="0" step="0.01" value={incomeForm.amount} onChange={(event) => setIncomeForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2"><Label htmlFor="income-day">Dia</Label><Input id="income-day" type="number" min="1" max="31" value={incomeForm.dayOfMonth} onChange={(event) => setIncomeForm((current) => ({ ...current, dayOfMonth: Number(event.target.value) }))} /></div>
            </div>
            <div className="space-y-2"><Label htmlFor="income-details">Detalhes</Label><Input id="income-details" value={incomeForm.details || ''} onChange={(event) => setIncomeForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm"><input type="checkbox" checked={Boolean(incomeForm.isRecurring)} onChange={(event) => setIncomeForm((current) => ({ ...current, isRecurring: event.target.checked }))} />Receita recorrente</label>
            <div className="flex gap-2">
              <Button type="submit" className="flex-1 gap-2" disabled={savingKey === 'income'}><Plus className="h-4 w-4" />{savingKey === 'income' ? 'Salvando...' : incomeForm.editingId ? 'Atualizar receita' : 'Criar receita'}</Button>
              {incomeForm.editingId && <Button type="button" variant="outline" onClick={() => setIncomeForm(incomeFormReset())}>Limpar</Button>}
            </div>
          </form>
        </SectionShell>

        <SectionShell title={expenseForm.editingId ? 'Editar despesa' : 'Nova despesa'} description="Controle de débito, crédito e vencimentos.">
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
            <div className="flex gap-2">
              <Button type="submit" className="flex-1 gap-2" disabled={savingKey === 'expense'}><Plus className="h-4 w-4" />{savingKey === 'expense' ? 'Salvando...' : expenseForm.editingId ? 'Atualizar despesa' : 'Criar despesa'}</Button>
              {expenseForm.editingId && <Button type="button" variant="outline" onClick={() => setExpenseForm(expenseFormReset())}>Limpar</Button>}
            </div>
          </form>
        </SectionShell>

        <SectionShell title={subscriptionForm.editingId ? 'Editar assinatura' : 'Nova assinatura'} description="Serviços recorrentes e mensalidades.">
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
            <div className="flex gap-2">
              <Button type="submit" className="flex-1 gap-2" disabled={savingKey === 'subscription'}><Plus className="h-4 w-4" />{savingKey === 'subscription' ? 'Salvando...' : subscriptionForm.editingId ? 'Atualizar assinatura' : 'Criar assinatura'}</Button>
              {subscriptionForm.editingId && <Button type="button" variant="outline" onClick={() => setSubscriptionForm(subscriptionFormReset())}>Limpar</Button>}
            </div>
          </form>
        </SectionShell>
      </div>

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
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editIncome(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('income', item.id, !item.isPaid)} disabled={savingKey === `income-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => removeItem('income', item.id)} disabled={savingKey === `delete-income-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
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
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editExpense(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('expense', item.id, !item.isPaid)} disabled={savingKey === `expense-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => removeItem('expense', item.id)} disabled={savingKey === `delete-expense-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
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
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editSubscription(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => saveStatus('subscription', item.id, !item.isPaid)} disabled={savingKey === `subscription-${item.id}`}><CheckCircle2 className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => removeItem('subscription', item.id)} disabled={savingKey === `delete-subscription-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
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
    </div>
  );
}

export default function PersonalControlPage() {
  return <Page />;
}
