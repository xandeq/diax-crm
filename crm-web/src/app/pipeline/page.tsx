'use client';

import { useAuth } from '@/contexts/AuthContext';
import {
  getPipelineBoard,
  movePipelineStage,
  updatePipelineDeal,
  type PipelineBoard,
  type PipelineCard,
  type PipelineStage,
} from '@/services/pipeline';
import {
  AlertCircle,
  Banknote,
  Loader2,
  Phone,
  RefreshCw,
  Target,
  TrendingUp,
  Trophy,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';

// ─── Helpers ───────────────────────────────────────────────────────────────────

const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 });

function formatBRL(value?: number | null): string {
  return value != null ? BRL.format(value) : '—';
}

function daysSince(iso?: string | null): number | null {
  if (!iso) return null;
  return Math.floor((Date.now() - new Date(iso).getTime()) / 86_400_000);
}

const COLUMN_STYLE: Record<string, { border: string; header: string; dot: string }> = {
  Lead:        { border: 'border-slate-500/25', header: 'bg-slate-500/10',  dot: 'bg-slate-400' },
  Contacted:   { border: 'border-sky-500/25',   header: 'bg-sky-500/10',    dot: 'bg-sky-400' },
  Qualified:   { border: 'border-violet-500/25',header: 'bg-violet-500/10', dot: 'bg-violet-400' },
  Negotiating: { border: 'border-amber-500/25', header: 'bg-amber-500/10',  dot: 'bg-amber-400' },
  Customer:    { border: 'border-emerald-500/25', header: 'bg-emerald-500/10', dot: 'bg-emerald-400' },
};

const SEGMENT_BADGE: Record<string, string> = {
  Hot: 'bg-red-500/15 text-red-300 border-red-500/25',
  Warm: 'bg-amber-500/15 text-amber-300 border-amber-500/25',
  Cold: 'bg-sky-500/15 text-sky-300 border-sky-500/25',
};

// ─── Page ──────────────────────────────────────────────────────────────────────

export default function PipelinePage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [board, setBoard] = useState<PipelineBoard | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [dragOverStatus, setDragOverStatus] = useState<PipelineStage | null>(null);
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [editingValueId, setEditingValueId] = useState<string | null>(null);
  const [valueDraft, setValueDraft] = useState('');

  const loadBoard = useCallback(async () => {
    setError(null);
    try {
      setBoard(await getPipelineBoard());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar o pipeline.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (authLoading) return;
    if (!isAuthenticated) { router.push('/login'); return; }
    loadBoard();
  }, [isAuthenticated, authLoading, router, loadBoard]);

  // ── Drag and drop ────────────────────────────────────────────────────────────

  const handleDrop = async (targetStatus: PipelineStage) => {
    setDragOverStatus(null);
    if (!draggedId || !board) return;

    const sourceColumn = board.columns.find(c => c.cards.some(card => card.id === draggedId));
    if (!sourceColumn || sourceColumn.status === targetStatus) { setDraggedId(null); return; }

    const id = draggedId;
    setDraggedId(null);
    setUpdatingId(id);
    try {
      await movePipelineStage(id, targetStatus);
      await loadBoard(); // recarrega totais/previsão do servidor (fonte de verdade)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao mover o negócio.');
    } finally {
      setUpdatingId(null);
    }
  };

  // ── Edição inline de valor ───────────────────────────────────────────────────

  const startEditValue = (card: PipelineCard) => {
    setEditingValueId(card.id);
    setValueDraft(card.estimatedValue != null ? String(card.estimatedValue) : '');
  };

  const commitValue = async (card: PipelineCard) => {
    const raw = valueDraft.trim().replace(/\./g, '').replace(',', '.');
    const parsed = raw === '' ? null : Number(raw);
    setEditingValueId(null);
    if (parsed !== null && (Number.isNaN(parsed) || parsed < 0)) {
      setError('Valor inválido — use apenas números (ex.: 2500).');
      return;
    }
    if (parsed === (card.estimatedValue ?? null)) return;

    setUpdatingId(card.id);
    try {
      await updatePipelineDeal(card.id, parsed, card.expectedCloseDate ?? null);
      await loadBoard();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar o valor.');
    } finally {
      setUpdatingId(null);
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────────

  if (authLoading || isLoading) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
        <p className="text-sm text-white/50">Carregando pipeline de vendas...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0c0c0f] text-white">
      {/* ── Header com previsão ── */}
      <div className="border-b border-white/8 bg-[#0f0f13]/80 backdrop-blur-sm sticky top-0 z-10">
        <div className="max-w-screen-2xl mx-auto px-6 py-4 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-violet-500/20">
              <Target className="h-4 w-4 text-white" />
            </div>
            <div>
              <h1 className="text-base font-semibold">Pipeline de Vendas</h1>
              <p className="text-[11px] text-white/40">
                {board?.totalOpenDeals ?? 0} negócios abertos · arraste os cards entre estágios
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3 flex-wrap">
            <div className="rounded-xl bg-violet-600/15 border border-violet-500/25 px-4 py-2"
              title="Soma de (valor × probabilidade do estágio) dos negócios abertos: Lead 10% · Contactado 25% · Qualificado 50% · Negociando 75%">
              <p className="text-[10px] uppercase tracking-wider text-violet-300/70 flex items-center gap-1">
                <TrendingUp className="h-3 w-3" /> Previsão ponderada
              </p>
              <p className="text-lg font-bold text-violet-200">{formatBRL(board?.weightedForecast)}</p>
            </div>
            <div className="rounded-xl bg-white/5 border border-white/10 px-4 py-2">
              <p className="text-[10px] uppercase tracking-wider text-white/40 flex items-center gap-1">
                <Banknote className="h-3 w-3" /> Em aberto
              </p>
              <p className="text-lg font-bold text-white/80">{formatBRL(board?.totalOpenValue)}</p>
            </div>
            <div className="rounded-xl bg-emerald-600/15 border border-emerald-500/25 px-4 py-2">
              <p className="text-[10px] uppercase tracking-wider text-emerald-300/70 flex items-center gap-1">
                <Trophy className="h-3 w-3" /> Fechado (30d)
              </p>
              <p className="text-lg font-bold text-emerald-200">
                {formatBRL(board?.wonLast30DaysValue)}
                <span className="text-xs font-normal text-emerald-300/60 ml-1">
                  · {board?.wonLast30DaysCount ?? 0}
                </span>
              </p>
            </div>
            <button
              type="button"
              onClick={loadBoard}
              className="h-9 w-9 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 flex items-center justify-center text-white/50 hover:text-white transition-all"
              title="Atualizar"
            >
              <RefreshCw className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      {/* ── Erro ── */}
      {error && (
        <div className="max-w-screen-2xl mx-auto px-6 pt-4">
          <div className="flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-sm text-red-300">
            <AlertCircle className="h-4 w-4 shrink-0" />
            <span className="flex-1">{error}</span>
            <button type="button" onClick={() => setError(null)} className="text-red-300/60 hover:text-red-200">✕</button>
          </div>
        </div>
      )}

      {/* ── Kanban ── */}
      <div className="max-w-screen-2xl mx-auto px-6 py-6">
        <div className="flex gap-4 overflow-x-auto pb-6 min-h-[65vh]">
          {board?.columns.map(col => {
            const style = COLUMN_STYLE[col.status] ?? COLUMN_STYLE.Lead;
            const isOver = dragOverStatus === col.status;
            return (
              <div
                key={col.status}
                className={`flex flex-col shrink-0 w-72 rounded-2xl border-2 transition-colors duration-150
                  ${isOver ? 'border-violet-400 bg-violet-500/10' : `${style.border} bg-white/[0.02]`}`}
                onDragOver={e => { e.preventDefault(); setDragOverStatus(col.status); }}
                onDragLeave={() => setDragOverStatus(null)}
                onDrop={e => { e.preventDefault(); handleDrop(col.status); }}
              >
                {/* Column header */}
                <div className={`px-4 py-3 rounded-t-2xl ${style.header} border-b ${style.border}`}>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className={`h-2 w-2 rounded-full ${style.dot}`} />
                      <span className="text-sm font-semibold text-white/80">{col.label}</span>
                      <span className="text-[11px] text-white/35">({col.count})</span>
                    </div>
                    {col.status !== 'Customer' && (
                      <span className="text-[10px] text-white/30" title="Probabilidade de fechamento deste estágio">
                        {Math.round(col.probability * 100)}%
                      </span>
                    )}
                  </div>
                  <p className="text-xs font-bold text-white/60 mt-1">{formatBRL(col.totalValue)}</p>
                </div>

                {/* Cards */}
                <div className="flex-1 p-2 space-y-2 overflow-y-auto max-h-[62vh]">
                  {col.cards.length === 0 && (
                    <p className="text-[11px] text-white/20 text-center py-8">
                      {isOver ? 'Solte aqui' : 'Vazio'}
                    </p>
                  )}
                  {col.count > col.cards.length && (
                    <p className="text-[10px] text-white/30 text-center py-1 bg-white/[0.03] rounded-lg border border-white/5"
                      title="O board prioriza os negócios de maior valor e score. Defina valores nos leads para trazê-los para o topo.">
                      mostrando top {col.cards.length} de {col.count}
                    </p>
                  )}
                  {col.cards.map(card => {
                    const idle = daysSince(card.lastContactAt);
                    const isUpdating = updatingId === card.id;
                    return (
                      <div
                        key={card.id}
                        draggable={!isUpdating}
                        onDragStart={e => { setDraggedId(card.id); e.dataTransfer.effectAllowed = 'move'; }}
                        onDragEnd={() => { setDraggedId(null); setDragOverStatus(null); }}
                        className={`rounded-xl bg-white/[0.04] border border-white/8 p-3 cursor-grab active:cursor-grabbing
                          hover:border-violet-500/30 transition-all
                          ${draggedId === card.id ? 'opacity-40' : ''} ${isUpdating ? 'animate-pulse' : ''}`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <p className="text-sm font-medium text-white/85 leading-tight">{card.name}</p>
                          {card.segment && (
                            <span className={`shrink-0 px-1.5 py-0.5 rounded text-[9px] font-medium border ${SEGMENT_BADGE[card.segment] ?? 'bg-white/10 text-white/50 border-white/15'}`}>
                              {card.segment}
                            </span>
                          )}
                        </div>
                        {card.companyName && (
                          <p className="text-[11px] text-white/40 truncate mt-0.5">{card.companyName}</p>
                        )}

                        {/* Valor do negócio — clique para editar */}
                        {editingValueId === card.id ? (
                          <input
                            autoFocus
                            value={valueDraft}
                            onChange={e => setValueDraft(e.target.value)}
                            onBlur={() => commitValue(card)}
                            onKeyDown={e => {
                              if (e.key === 'Enter') commitValue(card);
                              if (e.key === 'Escape') setEditingValueId(null);
                            }}
                            placeholder="R$ 0"
                            className="mt-2 w-full rounded-lg bg-black/40 border border-violet-500/40 px-2 py-1 text-sm text-white outline-none"
                          />
                        ) : (
                          <button
                            type="button"
                            onClick={() => startEditValue(card)}
                            className={`mt-2 text-sm font-bold transition-colors ${card.estimatedValue != null ? 'text-emerald-300 hover:text-emerald-200' : 'text-white/25 hover:text-white/50'}`}
                            title="Clique para definir o valor do negócio"
                          >
                            {card.estimatedValue != null ? formatBRL(card.estimatedValue) : '+ definir valor'}
                          </button>
                        )}

                        <div className="flex items-center gap-2 mt-2 text-[10px] text-white/30">
                          {card.leadScore != null && <span title="Lead score">⭐ {card.leadScore}</span>}
                          {card.phone && <span className="flex items-center gap-0.5"><Phone className="h-2.5 w-2.5" />{card.phone}</span>}
                          {idle != null && idle > 7 && col.status !== 'Customer' && (
                            <span className="text-amber-400/70" title={`${idle} dias sem contato`}>⏰ {idle}d parado</span>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
