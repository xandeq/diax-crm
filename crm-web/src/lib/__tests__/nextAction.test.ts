import { describe, expect, it } from 'vitest';
import { nextActionView } from '../nextAction';

const now = new Date(2026, 9, 29, 15, 0, 0);

describe('nextActionView', () => {
  it('sem tarefa aberta avisa que falta próxima ação', () => {
    expect(nextActionView(null, null, now)).toEqual({ tone: 'none', label: 'Sem próxima ação' });
  });

  it('tarefa sem data fica neutra', () => {
    expect(nextActionView('Ligar', null, now)).toEqual({ tone: 'later', label: 'Ligar' });
  });

  it('classifica atrasada, hoje, em breve e futura pelo dia (não pela hora)', () => {
    expect(nextActionView('Ligar', new Date(2026, 9, 27, 9).toISOString(), now).tone).toBe('overdue');
    expect(nextActionView('Ligar', new Date(2026, 9, 27, 9).toISOString(), now).label).toBe('Ligar · atrasada 2d');
    expect(nextActionView('Ligar', new Date(2026, 9, 29, 8).toISOString(), now).tone).toBe('today');
    expect(nextActionView('Ligar', new Date(2026, 9, 31, 8).toISOString(), now).tone).toBe('soon');
    expect(nextActionView('Ligar', new Date(2026, 10, 5, 8).toISOString(), now)).toEqual({ tone: 'later', label: 'Ligar · em 7d' });
  });
});
