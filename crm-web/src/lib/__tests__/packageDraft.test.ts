import { describe, expect, it } from 'vitest';
import { packageToDraft, parseBRL } from '../packageDraft';

const pacote = {
  id: '1', name: 'Site Essencial', shortDescription: 'Site de 1 página',
  scope: 'Página única\n\n  Botão WhatsApp  ', price: 1800, deliveryDays: 15, sortOrder: 0, isActive: true,
};

describe('packageToDraft', () => {
  it('monta título, descrição com itens e valor', () => {
    const d = packageToDraft(pacote);
    expect(d.title).toBe('Site Essencial');
    expect(d.description).toBe('Site de 1 página\n\n• Página única\n• Botão WhatsApp\n\nPrazo de entrega: 15 dias úteis após aprovação.');
    expect(d.amount).toBe('1800');
  });

  it('sem prazo não menciona entrega', () => {
    expect(packageToDraft({ ...pacote, deliveryDays: 0 }).description).not.toContain('Prazo');
  });
});

describe('parseBRL', () => {
  it('aceita formatos brasileiros e simples', () => {
    expect(parseBRL('2.500,00')).toBe(2500);
    expect(parseBRL('2500')).toBe(2500);
    expect(parseBRL('1.800,5')).toBe(1800.5);
    expect(parseBRL(' 900 ')).toBe(900);
  });

  it('rejeita texto', () => {
    expect(Number.isNaN(parseBRL('abc'))).toBe(true);
    expect(Number.isNaN(parseBRL(''))).toBe(true);
    expect(Number.isNaN(parseBRL('-10'))).toBe(true);
  });
});
