// Motivos de perda das propostas canceladas. Valores = enum ProposalLossReason da API (número).

export const LOSS_REASON_LABEL: Record<number, string> = {
  1: 'Preço',
  2: 'Sem resposta',
  3: 'Fechou com concorrente',
  4: 'Sem orçamento',
  5: 'Momento errado',
  6: 'Escopo não atende',
  7: 'Cliente desistiu',
  0: 'Outro',
};

/** Ordem de exibição no seletor (Outro por último). */
export const LOSS_REASON_OPTIONS: { value: number; label: string }[] = [1, 2, 3, 4, 5, 6, 7, 0]
  .map(value => ({ value, label: LOSS_REASON_LABEL[value] }));

export interface LossProposal {
  status: number;
  amount: number;
  lossReason?: number | null;
}

export interface LossBreakdownItem {
  reason: number | null; // null = cancelada sem motivo informado
  label: string;
  count: number;
  amount: number;
}

/** Agrupa as propostas canceladas por motivo, da mais frequente para a menos. */
export function lossBreakdown(proposals: LossProposal[]): LossBreakdownItem[] {
  const map = new Map<number | null, LossBreakdownItem>();
  for (const p of proposals) {
    if (p.status !== 4) continue;
    const reason = p.lossReason ?? null;
    const cur = map.get(reason) ?? {
      reason,
      label: reason === null ? 'Sem motivo informado' : (LOSS_REASON_LABEL[reason] ?? 'Outro'),
      count: 0,
      amount: 0,
    };
    cur.count += 1;
    cur.amount += p.amount;
    map.set(reason, cur);
  }
  return [...map.values()].sort((a, b) => b.count - a.count || b.amount - a.amount);
}
