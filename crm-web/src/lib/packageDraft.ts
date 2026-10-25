import type { ServicePackage } from '@/services/servicePackages';

/** Rascunho de proposta a partir do pacote. Mesmo texto de ServicePackage.ToProposalDescription() no backend. */
export function packageToDraft(p: ServicePackage) {
  const itens = p.scope.split('\n').map(s => s.trim()).filter(Boolean).map(s => `• ${s}`);
  const prazo = p.deliveryDays > 0 ? `\n\nPrazo de entrega: ${p.deliveryDays} dias úteis após aprovação.` : '';
  return {
    title: p.name,
    description: `${p.shortDescription.trim()}\n\n${itens.join('\n')}${prazo}`.trim(),
    amount: String(p.price),
  };
}

/** Aceita "2.500,00", "2500" e "2500.5". Retorna NaN se não for número. */
export function parseBRL(v: string): number {
  const t = v.trim();
  if (!/^[\d.,]+$/.test(t)) return NaN;
  const normalizado = t.includes(',') ? t.replace(/\./g, '').replace(',', '.') : t;
  return Number(normalizado);
}
