import { describe, it, expect } from 'vitest';
import { buildSalaryBuckets, buildRemanejamentos, liquidCashBalance } from '../salary-planner';
import { AccountType, type FinancialAccount } from '@/services/finance';
import type { PersonalControlMonthView } from '@/services/personalControlService';

// Minimal monthView factory — only the fields the planner reads.
function makeMonthView(overrides: Partial<PersonalControlMonthView> = {}): PersonalControlMonthView {
  const base = {
    period: { year: 2026, month: 6 },
    incomes: [],
    expenses: [],
    subscriptions: [],
    invoicesDueThisMonth: [],
  };
  return { ...base, ...overrides } as unknown as PersonalControlMonthView;
}

const income = (id: string, name: string, dayOfMonth: number, amount: number) =>
  ({ id, name, dayOfMonth, amount, isPaid: false }) as PersonalControlMonthView['incomes'][number];

const expense = (
  id: string,
  name: string,
  dueDay: number,
  amount: number,
  isPaid: boolean,
) => ({ id, name, dueDay, amount, isPaid, paymentType: 'debit' }) as PersonalControlMonthView['expenses'][number];

describe('buildSalaryBuckets', () => {
  it('opens bucket 0 with the real checking-account balance', () => {
    const mv = makeMonthView({ incomes: [income('i1', 'Salário', 5, 1000)] });
    const [b] = buildSalaryBuckets(mv, 500);
    // running = startingBalance(500) + income(1000) - cashOut(0) = 1500
    expect(b.runningBalance).toBe(1500);
  });

  it('a PAID expense does NOT move projected cash and is never pending (no double-counting)', () => {
    // startingBalance 1000, unreceived income 100, a paid expense of 300.
    // Paid expense is already in the balance → must not reduce projected cash again.
    const mv = makeMonthView({
      incomes: [income('i1', 'Salário', 5, 100)],
      expenses: [expense('e1', 'Aluguel pago', 6, 300, true)],
    });
    const [b] = buildSalaryBuckets(mv, 1000);
    const paid = b.expensesAtVista.find((e) => e.id === 'e1');
    expect(paid?._pending).toBe(false);
    expect(b.pendingTotal).toBe(0);
    // projected cash = startingBalance(1000) + openIncome(100), paid 300 NOT subtracted
    expect(b.runningBalance).toBe(1100);
    // display total still shows the full obligation
    expect(b.totalCashOut).toBe(300);
  });

  it('a RECEIVED income (isPaid) does not add to projected cash again', () => {
    const received = { id: 'i1', name: 'Salário', dayOfMonth: 5, amount: 5000, isPaid: true } as PersonalControlMonthView['incomes'][number];
    const mv = makeMonthView({ incomes: [received] });
    const [b] = buildSalaryBuckets(mv, 200);
    // already in balance → projected cash stays at startingBalance
    expect(b.runningBalance).toBe(200);
  });

  it('an UNPAID expense exceeding available cash is flagged pending', () => {
    const mv = makeMonthView({
      incomes: [income('i1', 'Salário', 5, 100)],
      expenses: [expense('e1', 'Aluguel', 6, 300, false)],
    });
    const [b] = buildSalaryBuckets(mv, 0);
    const e = b.expensesAtVista.find((x) => x.id === 'e1');
    expect(e?._pending).toBe(true);
    expect(b.pendingTotal).toBe(300);
  });
});

describe('liquidCashBalance', () => {
  const acc = (name: string, accountType: AccountType, balance: number, isActive = true) =>
    ({ name, accountType, balance, isActive }) as FinancialAccount;

  it('sums only active Checking/Cash/Savings accounts with positive balance', () => {
    // Mirrors the real production data: debt accounts mislabeled as Checking (negative),
    // and an investment account — only the real checking cash should count.
    const accounts = [
      acc('Conta Corrente', AccountType.Checking, 3700),
      acc('Itaú', AccountType.Checking, -18949.18), // debt → excluded (negative)
      acc('Santander PF', AccountType.Checking, -35179.27), // debt → excluded (negative)
      acc('XP Investimentos', AccountType.DigitalWallet, 77420), // not liquid type → excluded
      acc('Migração', 99 as AccountType, 0, false), // inactive → excluded
    ];
    expect(liquidCashBalance(accounts)).toBe(3700);
  });

  it('includes Cash and Savings, excludes Investment', () => {
    const accounts = [
      acc('Carteira', AccountType.Cash, 200),
      acc('Poupança', AccountType.Savings, 800),
      acc('Corretora', AccountType.Investment, 50000),
    ];
    expect(liquidCashBalance(accounts)).toBe(1000);
  });
});

describe('buildRemanejamentos', () => {
  it('suggests deferring only UNPAID bills to the next salary when a bucket is short', () => {
    const mv = makeMonthView({
      incomes: [income('i1', 'Salário 1', 5, 100), income('i2', 'Salário 2', 20, 1000)],
      expenses: [
        expense('e1', 'Aluguel', 6, 300, false), // unpaid, movable
        expense('e2', 'Luz paga', 7, 50, true), // paid, must not be suggested
      ],
    });
    const buckets = buildSalaryBuckets(mv, 0);
    const sugg = buildRemanejamentos(buckets);
    expect(sugg.length).toBeGreaterThan(0);
    expect(sugg[0].message).toContain('Aluguel');
    expect(sugg[0].message).not.toContain('Luz paga');
    // defers to the next salary bucket (effective pay day, may shift off weekends)
    expect(sugg[0].message).toContain(`para o salário de dia ${buckets[1].day}`);
  });

  it('returns no suggestions when every bucket is covered', () => {
    const mv = makeMonthView({
      incomes: [income('i1', 'Salário', 5, 1000)],
      expenses: [expense('e1', 'Aluguel', 6, 300, false)],
    });
    const buckets = buildSalaryBuckets(mv, 0);
    expect(buildRemanejamentos(buckets)).toHaveLength(0);
  });

  // Regression for the reported bug: with R$3.700 liquid and a large PAID expense
  // (NOVO APARTAMENTO 34.200) in the same bucket, the unpaid Faxina Regiane (1.260)
  // was being flagged "não coberto" because paid items were double-subtracted.
  it('cenário real maio: Faxina Regiane (não paga) não é remanejada quando o caixa cobre', () => {
    const salaryReceived = (id: string, name: string, day: number, amount: number) =>
      ({ id, name, dayOfMonth: day, amount, isPaid: true }) as PersonalControlMonthView['incomes'][number];
    const mv = makeMonthView({
      incomes: [
        salaryReceived('s1', 'SALARIO KPIT', 3, 10000),
        salaryReceived('s2', 'SALARIO PANTHEON', 15, 19600),
      ],
      expenses: [
        expense('e1', 'Faxina Regiane', 1, 1260, false), // o item do bug — não pago
        expense('e2', 'Faxina Hosana', 1, 3360, true),
        expense('e3', 'NOVO APARTAMENTO', 5, 34200, true), // pago — não pode derrubar o caixa
      ],
    });
    const buckets = buildSalaryBuckets(mv, 3700);
    const sugg = buildRemanejamentos(buckets);
    // a Faxina Regiane NÃO deve aparecer em nenhuma sugestão de remanejamento
    expect(sugg.every((s) => !s.message.includes('Faxina Regiane'))).toBe(true);
  });
});
