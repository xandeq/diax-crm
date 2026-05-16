import { describe, expect, it } from 'vitest';
import { getEffectivePayDay } from '../date-utils';

describe('getEffectivePayDay', () => {
  // Maio 2026: dia 1 = sexta ✓, dia 2 = sábado → adj sexta anterior (1)
  // dia 3 = domingo → adj sexta anterior (1), dia 9 = sábado → adj (8=sexta)

  it('returns same day when it falls on a weekday', () => {
    // 2026-05-01 = sexta
    const result = getEffectivePayDay(1, 2026, 5);
    expect(result.effectiveDay).toBe(1);
    expect(result.adjusted).toBe(false);
    expect(result.label).toBe('Dia 1');
  });

  it('advances saturday to friday', () => {
    // 2026-05-02 = sábado → move para 01 (sexta)
    const result = getEffectivePayDay(2, 2026, 5);
    expect(result.effectiveDay).toBe(1);
    expect(result.adjusted).toBe(true);
    expect(result.label).toContain('sex');
    expect(result.label).toContain('adj. do 2');
  });

  it('advances sunday to friday', () => {
    // 2026-05-03 = domingo → move para 01 (sexta)
    const result = getEffectivePayDay(3, 2026, 5);
    expect(result.effectiveDay).toBe(1);
    expect(result.adjusted).toBe(true);
    expect(result.label).toContain('adj. do 3');
  });

  it('does not cross month boundary — keeps original day', () => {
    // 2026-06-01 = segunda → sem ajuste
    // 2026-08-01 = sábado → adj sexta seria 2026-07-31 (mês anterior) → mantém dia 1
    const result = getEffectivePayDay(1, 2026, 8);
    // 2026-08-01 = sábado, shift=1 → 2026-07-31 = mês diferente → retorna original
    expect(result.effectiveDay).toBe(1);
    expect(result.adjusted).toBe(false);
  });

  it('clamps day to last day of month', () => {
    // Fevereiro 2026 tem 28 dias; dia 31 → clamp para 28
    // 2026-02-28 = sábado → adj para 27 (sexta)
    const result = getEffectivePayDay(31, 2026, 2);
    expect(result.effectiveDay).toBeLessThanOrEqual(28);
  });

  it('returns correct label for weekday (no adjustment)', () => {
    // 2026-05-15 = sexta
    const result = getEffectivePayDay(15, 2026, 5);
    expect(result.label).toBe('Dia 15');
    expect(result.adjusted).toBe(false);
  });

  it('returns correct label for adjusted saturday', () => {
    // 2026-05-09 = sábado → adj para 8 (sexta)
    const result = getEffectivePayDay(9, 2026, 5);
    expect(result.effectiveDay).toBe(8);
    expect(result.adjusted).toBe(true);
    expect(result.label).toBe('Dia 8 (sex, adj. do 9)');
  });

  it('handles day 1 as sunday correctly (no cross-month shift)', () => {
    // 2026-03-01 = domingo, shift=2 → 2026-02-27 (mês anterior) → mantém 1
    const result = getEffectivePayDay(1, 2026, 3);
    expect(result.effectiveDay).toBe(1);
    expect(result.adjusted).toBe(false);
  });

  it('handles last day of 31-day month correctly', () => {
    // 2026-05-31 = domingo → adj para 29 (sexta)
    const result = getEffectivePayDay(31, 2026, 5);
    expect(result.effectiveDay).toBe(29);
    expect(result.adjusted).toBe(true);
  });

  it('does not adjust a monday', () => {
    // 2026-05-04 = segunda
    const result = getEffectivePayDay(4, 2026, 5);
    expect(result.adjusted).toBe(false);
    expect(result.effectiveDay).toBe(4);
  });
});
