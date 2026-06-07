import { getEffectivePayDay } from './date-utils';
import { formatCurrency } from './utils';
import { AccountType, type FinancialAccount } from '@/services/finance';
import type {
  PersonalControlMonthView,
  PersonalControlInvoiceDueThisMonth,
} from '@/services/personalControlService';

/**
 * Liquid cash available to the salary planner: only Checking / Cash / Savings accounts
 * with a positive balance. Excludes investments (e.g. brokerage) and debt-tracking
 * accounts carried with a negative balance (credit cards mislabeled as Checking).
 */
export function liquidCashBalance(accounts: FinancialAccount[]): number {
  const liquidTypes = [AccountType.Checking, AccountType.Cash, AccountType.Savings];
  return accounts
    .filter((a) => a.isActive && a.balance > 0 && liquidTypes.includes(a.accountType))
    .reduce((sum, a) => sum + a.balance, 0);
}

type IncomeItem = PersonalControlMonthView['incomes'][number];
type ExpenseItem = PersonalControlMonthView['expenses'][number] & { _pending?: boolean };
type SubItem = PersonalControlMonthView['subscriptions'][number] & { _pending?: boolean };
type InvoiceItem = PersonalControlInvoiceDueThisMonth & { _pending?: boolean };

export interface SalaryBucket {
  day: number;
  nextDay: number;
  incomeName: string;
  incomes: IncomeItem[];
  expensesAtVista: ExpenseItem[];
  debitSubs: SubItem[];
  invoicesDue: InvoiceItem[];
  totalIncome: number;
  totalCashOut: number;
  pendingTotal: number;
  periodBalance: number;
  runningBalance: number;
  investSuggestion: number;
}

export interface Remanejamento {
  day: number;
  title: string;
  message: string;
}

/**
 * Builds per-salary cash-flow buckets for the Planner de Salário.
 *
 * Cash-walk semantics (projected-cash model):
 * - Bucket 0 opens with the liquid cash balance (`startingBalance`); later buckets
 *   carry the projected cash forward.
 * - Paid bills and received income are already reflected in the balance, so they do
 *   NOT move the projected cash again (no double-counting). Only OPEN items walk the
 *   cash: unreceived income adds, unpaid bills subtract.
 * - An unpaid bill that exceeds available projected cash is flagged `_pending`.
 * - `runningBalance` is the projected liquid cash; display totals (totalIncome/
 *   totalCashOut/periodBalance) still show the FULL month picture (paid + unpaid).
 */
export function buildSalaryBuckets(
  monthView: PersonalControlMonthView,
  startingBalance: number,
): SalaryBucket[] {
  const sortedIncomes = [...monthView.incomes].sort((a, b) => {
    const ea = getEffectivePayDay(a.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay;
    const eb = getEffectivePayDay(b.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay;
    return ea - eb;
  });

  const startDays = sortedIncomes.map((inc) =>
    getEffectivePayDay(inc.dayOfMonth, monthView.period.year, monthView.period.month).effectiveDay,
  );

  // CC subscriptions live inside the invoice expand; only debit subscriptions affect the cash walk.
  const debitSubscriptions = monthView.subscriptions.filter((s) => s.paymentType !== 'credit');

  return sortedIncomes.reduce<SalaryBucket[]>((acc, inc, i) => {
    const day = startDays[i];
    const nextDay = startDays[i + 1] ?? 32;
    const incomes = [inc];
    const inRange = (dueDay: number) => dueDay >= day && dueDay < nextDay;

    // Layer 1: debit expenses (dueDay in range)
    const expensesAtVistaRaw = monthView.expenses
      .filter((item) => inRange(item.dueDay) && item.paymentType !== 'credit')
      .sort((a, b) => a.dueDay - b.dueDay);

    // Layer 1: debit subscriptions only in bucket 0
    const debitSubsRaw = i === 0 ? debitSubscriptions : ([] as typeof debitSubscriptions);

    // Layer 2: invoices whose DueDate falls in this bucket
    const invoicesDueRaw = (monthView.invoicesDueThisMonth ?? [])
      .filter((inv) => {
        const dueDay = new Date(inv.dueDate).getUTCDate();
        return dueDay >= day && dueDay < nextDay;
      })
      .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime());

    const totalIncome = inc.amount;
    const prevRunning = acc.length > 0 ? acc[acc.length - 1].runningBalance : startingBalance;

    // Projected-cash model: paid expenses and received income are ALREADY reflected in the
    // account balance, so they must not move the projected cash again (avoids double-counting).
    // Only OPEN items walk the cash: unreceived income adds, unpaid bills subtract.
    const openIncome = inc.isPaid ? 0 : inc.amount;
    let available = prevRunning + openIncome;

    // A PAID item never moves projected cash (already settled) and is never pending.
    const settle = (isPaid: boolean, amount: number): boolean => {
      if (isPaid) return false;
      const pending = available - amount < 0;
      available -= amount;
      return pending;
    };

    const debitSubs: SubItem[] = debitSubsRaw.map((s) => ({ ...s, _pending: settle(s.isPaid, s.amount) }));
    const expensesAtVista: ExpenseItem[] = expensesAtVistaRaw.map((e) => ({ ...e, _pending: settle(e.isPaid, e.amount) }));
    const invoicesDue: InvoiceItem[] = invoicesDueRaw.map((inv) => {
      const amount = inv.statementAmount ?? inv.totalTransactionsAmount;
      if (amount <= 0) return { ...inv, _pending: false };
      return { ...inv, _pending: settle(inv.isPaid, amount) };
    });

    // Display totals show the FULL month picture (paid + unpaid).
    const totalCashOut =
      debitSubs.reduce((s, x) => s + x.amount, 0) +
      expensesAtVista.reduce((s, x) => s + x.amount, 0) +
      invoicesDue.reduce((s, x) => s + (x.statementAmount ?? x.totalTransactionsAmount), 0);

    const pendingTotal =
      debitSubs.filter((x) => x._pending).reduce((s, x) => s + x.amount, 0) +
      expensesAtVista.filter((x) => x._pending).reduce((s, x) => s + x.amount, 0) +
      invoicesDue.filter((x) => x._pending).reduce((s, x) => s + (x.statementAmount ?? x.totalTransactionsAmount), 0);

    const periodBalance = totalIncome - totalCashOut;
    // `available` now holds the projected cash after settling this bucket's OPEN items.
    const runningBalance = available;
    const periodCash = runningBalance - prevRunning;
    const investSuggestion = runningBalance > 0 && periodCash > 0 ? periodCash * 0.2 : 0;

    acc.push({
      day, nextDay, incomeName: inc.name, incomes,
      expensesAtVista, debitSubs, invoicesDue,
      totalIncome, totalCashOut, pendingTotal,
      periodBalance, runningBalance, investSuggestion,
    });
    return acc;
  }, []);
}

/**
 * For each bucket that ends negative or has an uncovered bill, suggests deferring
 * the UNPAID debit bills (you cannot reschedule what is already paid) to the next
 * salary date until the gap is covered. Pure suggestion — never mutates data.
 */
export function buildRemanejamentos(buckets: SalaryBucket[]): Remanejamento[] {
  return buckets.flatMap((b, i) => {
    const gap = Math.max(b.pendingTotal, b.runningBalance < 0 ? -b.runningBalance : 0);
    if (gap <= 0) return [] as Remanejamento[];

    const movable = [
      ...b.expensesAtVista.filter((e) => !e.isPaid).map((e) => ({ name: e.name, amount: e.amount })),
      ...b.debitSubs.filter((s) => !s.isPaid).map((s) => ({ name: s.name, amount: s.amount })),
    ].sort((x, y) => y.amount - x.amount);

    const picked: { name: string; amount: number }[] = [];
    let covered = 0;
    for (const m of movable) {
      if (covered >= gap) break;
      picked.push(m);
      covered += m.amount;
    }

    const next = buckets[i + 1];
    if (picked.length > 0 && next) {
      return [{
        day: b.day,
        title: `Dia ${b.day}: faltam ${formatCurrency(gap)}`,
        message: `Adie ${picked.map((p) => `${p.name} (${formatCurrency(p.amount)})`).join(', ')} para o salário de dia ${next.day} — cobre ${formatCurrency(covered)} e fecha o período no positivo.`,
      }];
    }
    return [{
      day: b.day,
      title: `Dia ${b.day}: faltam ${formatCurrency(gap)}`,
      message: picked.length === 0
        ? 'Nenhuma conta não-paga deste período pode ser remanejada. Antecipe uma entrada ou use a reserva.'
        : `Sem próximo salário no mês para realocar. Antecipe uma entrada de ${formatCurrency(gap)} ou priorize as despesas essenciais.`,
    }];
  });
}
