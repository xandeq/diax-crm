// Funil das propostas: onde elas param entre o envio e o pagamento.
// Status: 0 Rascunho | 1 Enviada | 2 Aceita | 3 Paga | 4 Cancelada.

export interface FunnelProposal {
  status: number;
  sentAt?: string | null;
  viewCount: number;
  acceptedAt?: string | null;
  amount: number;
}

export interface ProposalFunnel {
  sent: number;
  viewed: number;
  accepted: number;
  paid: number;
  paidAmount: number;
  /** Percentual (inteiro) de cada etapa sobre as enviadas. */
  pct: { viewed: number; accepted: number; paid: number };
}

/** Chegou ao cliente: tem data de envio, já foi aberta pelo link ou já passou de rascunho sem ser cancelada antes de enviar. */
function reachedClient(p: FunnelProposal): boolean {
  return !!p.sentAt || p.viewCount > 0 || (p.status >= 1 && p.status <= 3);
}

export function proposalFunnel(proposals: FunnelProposal[]): ProposalFunnel {
  const sent = proposals.filter(reachedClient);
  const viewed = sent.filter(p => p.viewCount > 0).length;
  const accepted = sent.filter(p => !!p.acceptedAt || p.status === 2 || p.status === 3).length;
  const paidList = sent.filter(p => p.status === 3);
  const pct = (n: number) => (sent.length ? Math.round((n / sent.length) * 100) : 0);
  return {
    sent: sent.length,
    viewed,
    accepted,
    paid: paidList.length,
    paidAmount: paidList.reduce((s, p) => s + p.amount, 0),
    pct: { viewed: pct(viewed), accepted: pct(accepted), paid: pct(paidList.length) },
  };
}
