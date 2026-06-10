'use client';

import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Newspaper, Clock, RefreshCw, ArrowRight, Trash2 } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { ScrollArea } from '@/components/ui/scroll-area';
import { dailyBriefingsService } from '@/services/dailyBriefingsService';
import { sourceMeta } from '@/types/dailyBriefings';
import { BriefingContent } from './BriefingContent';

function fmtTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  } catch {
    return '';
  }
}

export default function DailyBriefingsClient() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const queryClient = useQueryClient();

  async function handleDelete(id: string, e: React.MouseEvent) {
    e.stopPropagation();
    if (!window.confirm('Remover este briefing?')) return;
    setDeletingId(id);
    try {
      await dailyBriefingsService.remove(id);
      await queryClient.invalidateQueries({ queryKey: ['daily-briefings', 'today'] });
    } finally {
      setDeletingId(null);
    }
  }

  const { data: cards, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['daily-briefings', 'today'],
    queryFn: dailyBriefingsService.getToday,
    staleTime: 60_000,
  });

  const { data: detail, isLoading: detailLoading } = useQuery({
    queryKey: ['daily-briefings', 'detail', selectedId],
    queryFn: () => dailyBriefingsService.getById(selectedId as string),
    enabled: !!selectedId,
  });

  const today = new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

  return (
    <div>
      {/* Header */}
      <div className="flex items-start justify-between gap-4 mb-6">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-slate-900">
            <Newspaper className="h-6 w-6 text-emerald-600" />
            Daily Briefings
          </h1>
          <p className="mt-1 text-sm text-slate-500 capitalize">{today}</p>
        </div>
        <button
          onClick={() => refetch()}
          className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} />
          Atualizar
        </button>
      </div>

      {/* Loading */}
      {isLoading && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {[0, 1, 2].map(i => (
            <div key={i} className="h-36 animate-pulse rounded-xl border border-slate-200 bg-slate-100" />
          ))}
        </div>
      )}

      {/* Error */}
      {isError && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-center text-sm text-red-700">
          Não foi possível carregar os briefings. <button onClick={() => refetch()} className="underline">Tentar de novo</button>
        </div>
      )}

      {/* Empty */}
      {!isLoading && !isError && (cards?.length ?? 0) === 0 && (
        <div className="flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-slate-300 bg-slate-50 py-16 text-center">
          <Newspaper className="h-10 w-10 text-slate-300" />
          <p className="text-sm font-medium text-slate-600">Nenhum briefing hoje ainda</p>
          <p className="max-w-sm text-xs text-slate-400">
            Os briefings do dia aparecem aqui automaticamente quando os agentes rodam (manhã). Só o dia corrente é exibido.
          </p>
        </div>
      )}

      {/* Cards */}
      {!isLoading && (cards?.length ?? 0) > 0 && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {cards!.map(card => {
            const meta = sourceMeta(card.source);
            return (
              <div
                key={card.id}
                role="button"
                tabIndex={0}
                onClick={() => setSelectedId(card.id)}
                onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setSelectedId(card.id); }}
                className="group relative flex h-full cursor-pointer flex-col rounded-xl border border-slate-200 bg-white p-5 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-emerald-300 hover:shadow-md"
              >
                <button
                  type="button"
                  aria-label="Remover briefing"
                  onClick={(e) => handleDelete(card.id, e)}
                  disabled={deletingId === card.id}
                  className="absolute right-2.5 top-2.5 rounded-md p-1.5 text-slate-300 opacity-0 transition hover:bg-red-50 hover:text-red-500 group-hover:opacity-100 disabled:opacity-50"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
                <span className={`mb-3 inline-flex w-fit items-center rounded-full px-2.5 py-1 text-[11px] font-semibold ${meta.badge}`}>
                  {meta.label}
                </span>
                <h3 className="line-clamp-3 flex-1 pr-6 text-sm font-semibold leading-snug text-slate-800">
                  {card.title}
                </h3>
                <div className="mt-4 flex items-center justify-between text-xs text-slate-400">
                  <span className="inline-flex items-center gap-1">
                    <Clock className="h-3 w-3" /> {fmtTime(card.createdAt)}
                  </span>
                  <span className="inline-flex items-center gap-1 font-medium text-emerald-600 opacity-0 transition group-hover:opacity-100">
                    Ler <ArrowRight className="h-3 w-3" />
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal de leitura */}
      <Dialog open={!!selectedId} onOpenChange={(open) => { if (!open) setSelectedId(null); }}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 pr-6 text-left">
              {detail && (
                <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold ${sourceMeta(detail.source).badge}`}>
                  {sourceMeta(detail.source).label}
                </span>
              )}
              <span className="text-sm">{detail?.title ?? 'Carregando…'}</span>
            </DialogTitle>
          </DialogHeader>
          <ScrollArea className="max-h-[70vh] rounded-md">
            {detailLoading || !detail ? (
              <div className="space-y-3 py-4">
                {[0, 1, 2, 3].map(i => <div key={i} className="h-5 animate-pulse rounded bg-slate-100" />)}
              </div>
            ) : (
              <BriefingContent content={detail.content} format={detail.format} />
            )}
          </ScrollArea>
        </DialogContent>
      </Dialog>
    </div>
  );
}
