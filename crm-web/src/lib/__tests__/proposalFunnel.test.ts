import { describe, expect, it } from 'vitest';
import { proposalFunnel } from '../proposalFunnel';

type P = Parameters<typeof proposalFunnel>[0][number];
const base: P = { status: 1, sentAt: '2026-10-20T10:00:00Z', viewCount: 0, acceptedAt: null, amount: 1000 };

describe('proposalFunnel', () => {
  it('conta cada etapa só com propostas que chegaram ao cliente', () => {
    const f = proposalFunnel([
      { ...base },                                                    // enviada, não vista
      { ...base, viewCount: 3 },                                      // vista
      { ...base, status: 2, viewCount: 1, acceptedAt: '2026-10-21T10:00:00Z' }, // aceita
      { ...base, status: 3, viewCount: 2, acceptedAt: '2026-10-21T10:00:00Z', amount: 2500 }, // paga
      { ...base, status: 0, sentAt: null },                           // rascunho: fora do funil
    ]);
    expect(f.sent).toBe(4);
    expect(f.viewed).toBe(3);
    expect(f.accepted).toBe(2);
    expect(f.paid).toBe(1);
    expect(f.paidAmount).toBe(2500);
  });

  it('percentuais sobre as enviadas (inteiros)', () => {
    const f = proposalFunnel([
      { ...base, viewCount: 1 }, { ...base, viewCount: 1 }, { ...base }, { ...base, status: 3, viewCount: 1, acceptedAt: 'x' },
    ]);
    expect(f.pct.viewed).toBe(75);
    expect(f.pct.accepted).toBe(25);
    expect(f.pct.paid).toBe(25);
  });

  it('rascunho aberto pelo link conta como enviado (o backend marca Sent ao abrir)', () => {
    const f = proposalFunnel([{ ...base, status: 0, sentAt: null, viewCount: 1 }]);
    expect(f.sent).toBe(1);
    expect(f.viewed).toBe(1);
  });

  it('cancelada que já tinha sido enviada continua contando como enviada', () => {
    const f = proposalFunnel([{ ...base, status: 4, viewCount: 2 }]);
    expect(f.sent).toBe(1);
    expect(f.viewed).toBe(1);
    expect(f.paid).toBe(0);
  });

  it('lista vazia não divide por zero', () => {
    const f = proposalFunnel([]);
    expect(f.sent).toBe(0);
    expect(f.pct).toEqual({ viewed: 0, accepted: 0, paid: 0 });
  });
});
