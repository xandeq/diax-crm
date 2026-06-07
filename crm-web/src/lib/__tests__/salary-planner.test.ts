import { describe, it, expect } from 'vitest';
import { buildSalaryBuckets, buildRemanejamentos } from '../salary-planner';
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

  it('a PAID expense subtracts from cash but is never flagged pending, even when it exceeds available', () => {
    // startingBalance 0, income 100, a paid expense of 300 → available goes negative,
    // but because it is paid it must NOT be pending.
    const mv = makeMonthView({
      incomes: [income('i1', 'Salário', 5, 100)],
      expenses: [expense('e1', 'Aluguel pago', 6, 300, true)],
    });
    const [b] = buildSalaryBuckets(mv, 0);
    const paid = b.expensesAtVista.find((e) => e.id === 'e1');
    expect(paid?._pending).toBe(false);
    expect(b.pendingTotal).toBe(0);
    // still subtracts: 0 + 100 - 300 = -200
    expect(b.runningBalance).toBe(-200);
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
});
