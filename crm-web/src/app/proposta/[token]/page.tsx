'use client';

import {
  acceptPublicProposal,
  getPublicProposal,
  type PublicProposal,
} from '@/services/proposals';
import { AlertCircle, Check, CheckCircle2, Copy, FileText, Loader2 } from 'lucide-react';
import { useParams } from 'next/navigation';
import { useEffect, useState } from 'react';

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

/**
 * Página PÚBLICA da proposta — o cliente abre este link, lê, aceita e paga via PIX.
 * Sem autenticação (o token opaco na URL é a credencial).
 */
export default function PublicProposalPage() {
  const params = useParams<{ token: string }>();
  const token = params?.token ?? '';

  const [proposal, setProposal] = useState<PublicProposal | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAccepting, setIsAccepting] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!token) return;
    getPublicProposal(token)
      .then(setProposal)
      .catch(() => setError('Proposta não encontrada ou link inválido.'))
      .finally(() => setIsLoading(false));
  }, [token]);

  const accept = async () => {
    setIsAccepting(true);
    setError(null);
    try {
      setProposal(await acceptPublicProposal(token));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível aceitar a proposta.');
    } finally {
      setIsAccepting(false);
    }
  };

  const copyPix = async () => {
    if (!proposal?.pixCopiaECola) return;
    try {
      await navigator.clipboard.writeText(proposal.pixCopiaECola);
      setCopied(true);
      setTimeout(() => setCopied(false), 2500);
    } catch {
      // clipboard bloqueado — o usuário pode selecionar manualmente
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0c0c0f] flex items-center justify-center">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    );
  }

  if (error && !proposal) {
    return (
      <div className="min-h-screen bg-[#0c0c0f] flex flex-col items-center justify-center gap-3 px-6 text-center">
        <AlertCircle className="h-10 w-10 text-red-400" />
        <p className="text-white/70">{error}</p>
      </div>
    );
  }

  if (!proposal) return null;

  const isAccepted = proposal.status === 2;
  const isPaid = proposal.status === 3;
  const isCancelled = proposal.status === 4;
  const canInteract = !isPaid && !isCancelled && !proposal.isExpired;

  return (
    <div className="min-h-screen bg-[#0c0c0f] text-white">
      <div className="max-w-2xl mx-auto px-6 py-12">
        {/* Cabeçalho */}
        <div className="flex items-center gap-3 mb-8">
          <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center">
            <FileText className="h-5 w-5 text-white" />
          </div>
          <div>
            <p className="text-[11px] uppercase tracking-wider text-white/40">Proposta comercial para</p>
            <p className="text-sm font-semibold text-white/90">{proposal.customerName}</p>
          </div>
        </div>

        {/* Status banners */}
        {isPaid && (
          <div className="mb-6 flex items-center gap-2 rounded-xl bg-emerald-500/10 border border-emerald-500/25 px-4 py-3 text-emerald-300 text-sm">
            <CheckCircle2 className="h-4 w-4" /> Proposta paga — obrigado! Entraremos em contato para iniciar o projeto.
          </div>
        )}
        {isAccepted && !isPaid && (
          <div className="mb-6 flex items-center gap-2 rounded-xl bg-violet-500/10 border border-violet-500/25 px-4 py-3 text-violet-300 text-sm">
            <CheckCircle2 className="h-4 w-4" /> Proposta aceita em {proposal.acceptedAt ? new Date(proposal.acceptedAt).toLocaleDateString('pt-BR') : ''} — finalize com o pagamento abaixo.
          </div>
        )}
        {proposal.isExpired && !isPaid && (
          <div className="mb-6 flex items-center gap-2 rounded-xl bg-amber-500/10 border border-amber-500/25 px-4 py-3 text-amber-300 text-sm">
            <AlertCircle className="h-4 w-4" /> Esta proposta expirou. Entre em contato para uma nova versão.
          </div>
        )}
        {isCancelled && (
          <div className="mb-6 flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-red-300 text-sm">
            <AlertCircle className="h-4 w-4" /> Esta proposta foi cancelada.
          </div>
        )}
        {error && (
          <div className="mb-6 flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-red-300 text-sm">
            <AlertCircle className="h-4 w-4" /> {error}
          </div>
        )}

        {/* Conteúdo */}
        <div className="rounded-2xl bg-white/[0.03] border border-white/10 p-8 space-y-6">
          <h1 className="text-2xl font-bold text-white">{proposal.title}</h1>

          <div className="text-sm text-white/70 leading-relaxed whitespace-pre-wrap">
            {proposal.description}
          </div>

          <div className="border-t border-white/10 pt-6 flex items-end justify-between flex-wrap gap-4">
            <div>
              <p className="text-[11px] uppercase tracking-wider text-white/40">Investimento total</p>
              <p className="text-3xl font-bold text-emerald-300">{BRL.format(proposal.amount)}</p>
              {proposal.validUntil && !proposal.isExpired && (
                <p className="text-[11px] text-white/35 mt-1">
                  Válida até {new Date(proposal.validUntil).toLocaleDateString('pt-BR')}
                </p>
              )}
            </div>

            {canInteract && !isAccepted && (
              <button
                type="button"
                onClick={accept}
                disabled={isAccepting}
                className="flex items-center gap-2 px-6 py-3 rounded-xl bg-violet-600 hover:bg-violet-500 text-white font-semibold transition-all disabled:opacity-60"
              >
                {isAccepting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
                Aceitar proposta
              </button>
            )}
          </div>
        </div>

        {/* PIX */}
        {canInteract && proposal.pixCopiaECola && (
          <div className="mt-6 rounded-2xl bg-white/[0.03] border border-white/10 p-6">
            <h2 className="text-sm font-semibold text-white/80 mb-1">💳 Pagamento via PIX</h2>
            <p className="text-[12px] text-white/40 mb-4">
              Abra o app do seu banco, escolha <strong>PIX → Copia e Cola</strong>, e cole o código abaixo.
            </p>
            <div className="rounded-xl bg-black/40 border border-white/10 p-3 font-mono text-[11px] text-white/60 break-all select-all">
              {proposal.pixCopiaECola}
            </div>
            <button
              type="button"
              onClick={copyPix}
              className={`mt-3 flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium transition-all
                ${copied ? 'bg-emerald-600/20 border border-emerald-500/30 text-emerald-300' : 'bg-white/8 hover:bg-white/12 border border-white/10 text-white/70'}`}
            >
              {copied ? <><Check className="h-4 w-4" /> Copiado!</> : <><Copy className="h-4 w-4" /> Copiar código PIX</>}
            </button>
            <p className="text-[11px] text-white/30 mt-3">
              Após o pagamento, confirmaremos por email/WhatsApp e daremos início ao projeto.
            </p>
          </div>
        )}

        <p className="text-center text-[11px] text-white/25 mt-10">
          Alexandre Queiroz · Marketing Digital &amp; Desenvolvimento — alexandrequeiroz.com.br
        </p>
      </div>
    </div>
  );
}
