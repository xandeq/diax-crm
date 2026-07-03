'use client';

import { useAuth } from '@/contexts/AuthContext';
import {
  cancelProposal,
  listProposals,
  markProposalPaid,
  publicProposalUrl,
  PROPOSAL_STATUS_LABEL,
  type Proposal,
} from '@/services/proposals';
import {
  AlertCircle,
  Banknote,
  Check,
  Copy,
  Eye,
  FileText,
  Loader2,
  RefreshCw,
  XCircle,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

const STATUS_BADGE: Record<number, string> = {
  0: 'bg-white/10 text-white/60 border-white/15',                 // Rascunho
  1: 'bg-sky-500/15 text-sky-300 border-sky-500/25',              // Enviada
  2: 'bg-violet-500/15 text-violet-300 border-violet-500/25',     // Aceita
  3: 'bg-emerald-500/15 text-emerald-300 border-emerald-500/25',  // Paga
  4: 'bg-red-500/10 text-red-300/70 border-red-500/20',           // Cancelada
};

const FILTERS: { key: string; label: string; statuses: number[] }[] = [
  { key: 'all', label: 'Todas', statuses: [0, 1, 2, 3, 4] },
  { key: 'open', label: 'Em aberto', statuses: [0, 1, 2] },
  { key: 'paid', label: 'Pagas', statuses: [3] },
  { key: 'cancelled', label: 'Canceladas', statuses: [4] },
];

export default function ProposalsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [proposals, setProposals] = useState<Proposal[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('all');
  const [busyId, setBusyId] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [confirmAction, setConfirmAction] = useState<{ id: string; kind: 'pay' | 'cancel'; title: string } | null>(null);

  const load = useCallback(() => {
    setIsLoading(true);
    setError(null);
    listProposals()
      .then(setProposals)
      .catch(err => setError(err instanceof Error ? err.message : 'Erro ao carregar propostas.'))
      .finally(() => setIsLoading(false));
  }, []);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    load();
  }, [authLoading, isAuthenticated, router, load]);

  const copyLink = async (p: Proposal) => {
    try {
      await navigator.clipboard.writeText(publicProposalUrl(p.publicToken));
      setCopiedId(p.id);
      setTimeout(() => setCopiedId(null), 2000);
    } catch {
      setError('Não foi possível copiar o link.');
    }
  };

  const runAction = async () => {
    if (!confirmAction) return;
    setBusyId(confirmAction.id);
    setError(null);
    try {
      const updated = confirmAction.kind === 'pay'
        ? await markProposalPaid(confirmAction.id)
        : await cancelProposal(confirmAction.id);
      setProposals(prev => prev.map(p => (p.id === updated.id ? updated : p)));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ação falhou.');
    } finally {
      setBusyId(null);
      setConfirmAction(null);
    }
  };

  const visible = proposals.filter(p =>
    (FILTERS.find(f => f.key === filter)?.statuses ?? []).includes(p.status));
  const openTotal = proposals.filter(p => p.status === 1 || p.status === 2).reduce((s, p) => s + p.amount, 0);
  const paidTotal = proposals.filter(p => p.status === 3).reduce((s, p) => s + p.amount, 0);

  if (authLoading || (isLoading && proposals.length === 0 && !error)) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    );
  }

  return (
    <div className="min-h-screen">
      {/* Header */}
      <div className="border-b border-white/5 bg-white/[0.02]">
        <div className="max-w-screen-xl mx-auto px-6 py-5 flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-xl font-bold text-white flex items-center gap-2">
              <FileText className="h-5 w-5 text-violet-400" /> Propostas
            </h1>
            <p className="text-[12px] text-white/40 mt-0.5">
              Na mesa: <span className="text-sky-300 font-medium">{BRL.format(openTotal)}</span>
              {' · '}Pagas: <span className="text-emerald-300 font-medium">{BRL.format(paidTotal)}</span>
            </p>
          </div>
          <div className="flex items-center gap-2">
            {FILTERS.map(f => (
              <button
                key={f.key}
                type="button"
                onClick={() => setFilter(f.key)}
                className={`px-3 h-8 rounded-lg text-xs font-medium border transition-all
                  ${filter === f.key
                    ? 'bg-violet-600/25 border-violet-500/40 text-violet-200'
                    : 'bg-white/5 border-white/10 text-white/50 hover:text-white'}`}
              >
                {f.label}
              </button>
            ))}
            <button
              type="button"
              onClick={load}
              className="h-8 w-8 rounded-lg bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center text-white/50 hover:text-white transition-all"
              title="Atualizar"
            >
              <RefreshCw className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>

      {error && (
        <div className="max-w-screen-xl mx-auto px-6 pt-4">
          <div className="flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-sm text-red-300">
            <AlertCircle className="h-4 w-4 shrink-0" /> {error}
          </div>
        </div>
      )}

      {/* Modal de confirmação */}
      {confirmAction && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4"
          onClick={() => !busyId && setConfirmAction(null)}>
          <div className="w-full max-w-sm rounded-2xl bg-[#141419] border border-white/10 p-6 space-y-4"
            onClick={e => e.stopPropagation()}>
            <h2 className="text-sm font-semibold text-white">
              {confirmAction.kind === 'pay' ? '💰 Confirmar pagamento?' : '❌ Cancelar proposta?'}
            </h2>
            <p className="text-[12px] text-white/50">
              {confirmAction.kind === 'pay'
                ? `"${confirmAction.title}" será marcada como PAGA e o lead vira cliente no pipeline.`
                : `"${confirmAction.title}" será cancelada — o link público deixa de aceitar pagamento.`}
            </p>
            <div className="flex gap-2">
              <button type="button" onClick={() => setConfirmAction(null)} disabled={!!busyId}
                className="flex-1 px-4 py-2.5 rounded-xl bg-white/5 border border-white/10 text-white/60 text-sm hover:text-white transition-all">
                Voltar
              </button>
              <button type="button" onClick={runAction} disabled={!!busyId}
                className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-semibold transition-all disabled:opacity-60
                  ${confirmAction.kind === 'pay' ? 'bg-emerald-600 hover:bg-emerald-500 text-white' : 'bg-red-600/80 hover:bg-red-500/80 text-white'}`}>
                {busyId ? <Loader2 className="h-4 w-4 animate-spin" /> : confirmAction.kind === 'pay' ? <Banknote className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
                Confirmar
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Lista */}
      <div className="max-w-screen-xl mx-auto px-6 py-6">
        {visible.length === 0 ? (
          <div className="text-center py-16 text-white/35 text-sm">
            {proposals.length === 0
              ? <>Nenhuma proposta ainda — crie a primeira pelo botão <em>Proposta</em> nos cards do <a href="/pipeline" className="text-violet-300 underline">Pipeline</a>.</>
              : 'Nenhuma proposta neste filtro.'}
          </div>
        ) : (
          <div className="space-y-2">
            {visible.map(p => (
              <div key={p.id} className="rounded-xl bg-white/[0.03] border border-white/10 px-5 py-4 flex items-center gap-4 flex-wrap">
                <div className="flex-1 min-w-[220px]">
                  <p className="text-sm font-medium text-white/90">{p.title}</p>
                  <p className="text-[11px] text-white/40 mt-0.5">
                    {p.customerName} · criada {new Date(p.createdAt).toLocaleDateString('pt-BR')}
                    {p.validUntil && ` · válida até ${new Date(p.validUntil).toLocaleDateString('pt-BR')}`}
                  </p>
                </div>

                <span className={`px-2.5 py-1 rounded-lg text-[11px] font-medium border ${STATUS_BADGE[p.status]}`}>
                  {PROPOSAL_STATUS_LABEL[p.status]}
                </span>

                <span className="flex items-center gap-1 text-[11px] text-white/40" title="Visualizações do link público">
                  <Eye className="h-3 w-3" /> {p.viewCount}
                </span>

                <span className="text-sm font-semibold text-white/90 min-w-[90px] text-right">
                  {BRL.format(p.amount)}
                </span>

                <div className="flex items-center gap-1.5">
                  <button
                    type="button"
                    onClick={() => copyLink(p)}
                    className={`h-8 px-2.5 rounded-lg text-[11px] font-medium border flex items-center gap-1 transition-all
                      ${copiedId === p.id
                        ? 'bg-emerald-600/20 border-emerald-500/30 text-emerald-300'
                        : 'bg-white/5 border-white/10 text-white/50 hover:text-white'}`}
                    title="Copiar link público"
                  >
                    {copiedId === p.id ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                    Link
                  </button>
                  {(p.status === 1 || p.status === 2) && (
                    <button
                      type="button"
                      onClick={() => setConfirmAction({ id: p.id, kind: 'pay', title: p.title })}
                      disabled={busyId === p.id}
                      className="h-8 px-2.5 rounded-lg text-[11px] font-medium bg-emerald-600/15 border border-emerald-500/25 text-emerald-300 hover:bg-emerald-600/25 flex items-center gap-1 transition-all disabled:opacity-50"
                      title="Marcar como paga — o lead vira cliente"
                    >
                      <Banknote className="h-3 w-3" /> Paga
                    </button>
                  )}
                  {p.status !== 3 && p.status !== 4 && (
                    <button
                      type="button"
                      onClick={() => setConfirmAction({ id: p.id, kind: 'cancel', title: p.title })}
                      disabled={busyId === p.id}
                      className="h-8 px-2.5 rounded-lg text-[11px] font-medium bg-white/5 border border-white/10 text-white/40 hover:text-red-300 hover:border-red-500/25 flex items-center gap-1 transition-all disabled:opacity-50"
                      title="Cancelar proposta"
                    >
                      <XCircle className="h-3 w-3" /> Cancelar
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
