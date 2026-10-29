import { describe, expect, it } from 'vitest';
import { lossBreakdown } from '../proposalLoss';

describe('lossBreakdown', () => {
  it('ignora propostas que não estão canceladas', () => {
    expect(lossBreakdown([{ status: 1, amount: 100, lossReason: 1 }, { status: 3, amount: 50 }])).toEqual([]);
  });

  it('agrupa por motivo, soma valores e ordena do mais frequente', () => {
    const r = lossBreakdown([
      { status: 4, amount: 1000, lossReason: 2 },
      { status: 4, amount: 500, lossReason: 1 },
      { status: 4, amount: 700, lossReason: 1 },
    ]);
    expect(r.map(i => [i.label, i.count, i.amount])).toEqual([
      ['Preço', 2, 1200],
      ['Sem resposta', 1, 1000],
    ]);
  });

  it('separa canceladas sem motivo informado do motivo Outro (0)', () => {
    const r = lossBreakdown([
      { status: 4, amount: 10 },
      { status: 4, amount: 10, lossReason: null },
      { status: 4, amount: 10, lossReason: 0 },
    ]);
    expect(r.find(i => i.reason === null)?.count).toBe(2);
    expect(r.find(i => i.reason === 0)?.label).toBe('Outro');
  });
});
