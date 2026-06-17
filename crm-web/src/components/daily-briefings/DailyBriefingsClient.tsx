'use client';

import { useState, useRef, useCallback, memo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Newspaper, Clock, RefreshCw, ArrowRight, ArrowLeft, Trash2,
  Sparkles, Code2, ListChecks, TrendingUp, BarChart2, Activity, Bot,
  Calendar, Send,
} from 'lucide-react';
import { dailyBriefingsService } from '@/services/dailyBriefingsService';
import { BriefingContent } from './BriefingContent';
import type { BriefingCard } from '@/types/dailyBriefings';

/* ═════════════════════════════════════ META ══════════════════ */

const SOURCE: Record<string, {
  label: string; short: string; icon: React.ElementType;
  accent: string; rgb: string;
}> = {
  'ia-anthropic':    { label: 'Claude AI',       short: 'Claude',   icon: Sparkles,   accent: '#38bdf8', rgb: '56,189,248'  },
  'ia-openai':       { label: 'Codex / OpenAI',  short: 'Codex',    icon: Code2,      accent: '#34d399', rgb: '52,211,153'  },
  'crm-tarefas':     { label: 'Tarefas',         short: 'Tarefas',  icon: ListChecks, accent: '#fbbf24', rgb: '251,191,36'  },
  'investiq-manha':  { label: 'Mercado — Manhã', short: 'Mercado',  icon: TrendingUp, accent: '#22c55e', rgb: '34,197,94'   },
  'investiq-tarde':  { label: 'Mercado — Tarde', short: 'Tarde',    icon: BarChart2,  accent: '#818cf8', rgb: '129,140,248' },
  'trends-semanal':  { label: 'Trend Pulse',     short: 'Trends',   icon: Activity,   accent: '#fb923c', rgb: '251,146,60'  },
  'briefing-sessao': { label: 'Sessão Claude',   short: 'Sessão',   icon: Bot,        accent: '#2dd4bf', rgb: '45,212,191'  },
  // content engine
  'content-engine-vng': { label: 'VagaNaGringa', short: 'VNG',  icon: Send, accent: '#f472b6', rgb: '244,114,182' },
  'content-engine-aq':  { label: 'Alex Queiroz', short: 'AQ',   icon: Send, accent: '#67e8f9', rgb: '103,232,249' },
  // legacy aliases
  'claude-ai':       { label: 'Claude AI',       short: 'Claude',   icon: Sparkles,   accent: '#38bdf8', rgb: '56,189,248'  },
  'codex-chatgpt':   { label: 'Codex / OpenAI',  short: 'Codex',    icon: Code2,      accent: '#34d399', rgb: '52,211,153'  },
  'tarefas-ias':     { label: 'Tarefas',         short: 'Tarefas',  icon: ListChecks, accent: '#fbbf24', rgb: '251,191,36'  },
};

function srcMeta(source: string) {
  return SOURCE[source] ?? { label: source, short: source, icon: Newspaper, accent: '#00D4AA', rgb: '0,212,170' };
}

function fmtTime(iso: string): string {
  try { return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }); }
  catch { return ''; }
}

const SPRING = { type: 'spring' as const, stiffness: 310, damping: 26 };
const EASE: [number, number, number, number] = [0.16, 1, 0.3, 1];

/* ════════════════════════════════════ STYLES ═════════════════ */

const CSS = `
  .db {
    font-family: var(--font-jakarta), var(--font-inter), system-ui, -apple-system, sans-serif;
    color: #d4d4d8;
  }
  .db-mono { font-family: var(--font-mono), 'JetBrains Mono', ui-monospace, monospace; font-variant-numeric: tabular-nums; }

  /* heading */
  .db-heading {
    font-size: clamp(20px, 2.4vw, 27px);
    font-weight: 800;
    letter-spacing: -0.026em;
    color: #fafafa;
    line-height: 1;
  }
  .db-sub {
    font-size: 12.5px; color: #52525b;
    display: flex; align-items: center; gap: 6px;
    margin-top: 6px; text-transform: capitalize;
  }

  /* source pills */
  .db-pills { display: flex; gap: 7px; overflow-x: auto; padding-bottom: 2px; scrollbar-width: none; }
  .db-pills::-webkit-scrollbar { display: none; }
  .db-pill {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 5px 13px; border-radius: 999px;
    border: 1px solid rgba(255,255,255,0.08);
    background: rgba(255,255,255,0.04);
    font-size: 12px; font-weight: 600; color: #71717a;
    cursor: pointer; white-space: nowrap; flex-shrink: 0;
    transition: color .15s, border-color .15s, background .15s;
  }
  .db-pill:hover { color: #d4d4d8; border-color: rgba(255,255,255,0.18); background: rgba(255,255,255,0.07); }
  .db-pill-on {
    color: var(--pc) !important;
    border-color: rgba(var(--pr), 0.35) !important;
    background: rgba(var(--pr), 0.09) !important;
  }
  .db-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; flex-shrink: 0; }

  /* grid */
  .db-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 13px;
  }
  @media (max-width: 600px) { .db-grid { grid-template-columns: 1fr; } }

  /* card */
  .db-card {
    position: relative;
    display: flex; flex-direction: column;
    padding: 18px 18px 16px;
    border-radius: 15px;
    background: rgba(255,255,255,0.03);
    border: 1px solid rgba(255,255,255,0.075);
    cursor: pointer; overflow: hidden;
    --sx: 50%; --sy: 50%;
    transition: border-color .22s, box-shadow .22s;
    user-select: none;
  }
  .db-card:hover {
    border-color: rgba(var(--ar), 0.28);
    box-shadow: 0 14px 36px rgba(0,0,0,.38);
  }
  /* left accent bar */
  .db-card::before {
    content: '';
    position: absolute; left: 0; top: 14px; bottom: 14px;
    width: 2.5px; border-radius: 0 3px 3px 0;
    background: var(--ac); opacity: 0.75;
  }
  /* spotlight radial */
  .db-card::after {
    content: '';
    position: absolute; inset: 0; border-radius: inherit;
    background: radial-gradient(320px circle at var(--sx) var(--sy),
      rgba(var(--ar), 0.1) 0%, transparent 65%);
    opacity: 0; transition: opacity .32s ease; pointer-events: none;
  }
  .db-card:hover::after { opacity: 1; }

  .db-chip {
    width: 34px; height: 34px; border-radius: 9px;
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0; background: rgba(var(--ar), 0.13);
  }
  .db-src-label { font-size: 10.5px; font-weight: 700; letter-spacing: 0.025em; color: var(--ac); }
  .db-card-title {
    font-size: 13.5px; font-weight: 600; color: #e4e4e7; line-height: 1.52;
    display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical;
    overflow: hidden; flex: 1; margin-top: 12px; padding-right: 6px;
  }
  .db-foot { font-size: 11px; color: #52525b; display: flex; align-items: center; gap: 4px; }
  .db-read { font-size: 11.5px; font-weight: 700; color: var(--ac); display: flex; align-items: center; gap: 3px; opacity: 0; transition: opacity .18s; }
  .db-card:hover .db-read { opacity: 1; }

  /* delete btn */
  .db-del {
    position: absolute; top: 10px; right: 10px;
    display: flex; padding: 6px; border-radius: 7px;
    border: none; background: transparent; color: #3f3f46;
    cursor: pointer; opacity: 0; transition: opacity .15s, color .15s, background .15s;
  }
  .db-card:hover .db-del { opacity: 1; }
  .db-del:hover { color: #f87171; background: rgba(248,113,113,0.13); }

  /* buttons */
  .db-btn {
    display: inline-flex; align-items: center; gap: 7px;
    padding: 8px 14px; border-radius: 10px;
    border: 1px solid rgba(255,255,255,0.1);
    background: rgba(255,255,255,0.05);
    color: #a1a1aa; font-size: 13px; font-weight: 600; cursor: pointer;
    transition: color .15s, border-color .15s, background .15s;
    flex-shrink: 0;
  }
  .db-btn:hover { color: #f4f4f5; border-color: rgba(255,255,255,0.2); background: rgba(255,255,255,0.08); }
  .db-btn:active { transform: scale(0.98); }

  /* skeleton shimmer */
  .db-skel {
    border-radius: 15px;
    background: linear-gradient(90deg,
      rgba(255,255,255,0.04) 0%, rgba(255,255,255,0.07) 50%, rgba(255,255,255,0.04) 100%);
    background-size: 200% 100%;
    animation: db-sh 1.85s ease infinite;
  }
  @keyframes db-sh { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }

  /* empty */
  .db-empty {
    display: flex; flex-direction: column; align-items: center;
    justify-content: center; gap: 13px; padding: 72px 20px;
    border-radius: 18px; border: 1px dashed rgba(255,255,255,0.1);
    background: rgba(255,255,255,0.015); text-align: center;
    grid-column: 1 / -1;
  }

  /* detail */
  .db-read-view { max-width: 780px; margin: 0 auto; }
  .db-detail-chip {
    width: 44px; height: 44px; border-radius: 12px;
    display: flex; align-items: center; justify-content: center;
    flex-shrink: 0; background: rgba(var(--ar), 0.14);
  }
  .db-detail-title {
    font-size: 21px; font-weight: 800; color: #fafafa;
    letter-spacing: -0.022em; line-height: 1.3; margin-top: 3px;
  }

  /* markdown prose */
  .briefing-md { color: #cbced4; font-size: 15px; line-height: 1.78; }
  .briefing-md > :first-child { margin-top: 0; }
  .briefing-md h1, .briefing-md h2, .briefing-md h3, .briefing-md h4 {
    color: #f4f4f5; font-weight: 700; line-height: 1.3;
    margin: 1.6em 0 0.6em; letter-spacing: -0.01em;
  }
  .briefing-md h1 { font-size: 22px; }
  .briefing-md h2 { font-size: 17.5px; padding-bottom: 7px; border-bottom: 1px solid rgba(255,255,255,0.08); }
  .briefing-md h3 { font-size: 15.5px; color: #e4e4e7; }
  .briefing-md p { margin: 0.85em 0; }
  .briefing-md strong { color: #f4f4f5; font-weight: 700; }
  .briefing-md a { color: #00D4AA; text-decoration: none; border-bottom: 1px solid rgba(0,212,170,0.35); word-break: break-word; }
  .briefing-md a:hover { border-bottom-color: #00D4AA; }
  .briefing-md ul, .briefing-md ol { padding-left: 1.4em; margin: 0.7em 0; }
  .briefing-md li { margin: 0.38em 0; }
  .briefing-md code { font-family: var(--font-mono), monospace; font-size: 13px; background: rgba(255,255,255,0.08); padding: 2px 6px; border-radius: 5px; color: #5eead4; }
  .briefing-md pre { background: rgba(0,0,0,0.38); border: 1px solid rgba(255,255,255,0.09); border-radius: 11px; padding: 16px; overflow-x: auto; margin: 1em 0; }
  .briefing-md pre code { background: none; padding: 0; color: #cbd5e1; font-size: 12.5px; line-height: 1.65; }
  .briefing-md blockquote { border-left: 3px solid #00D4AA; padding-left: 14px; margin: 1em 0; color: #a1a1aa; }
  .briefing-md hr { border: none; border-top: 1px solid rgba(255,255,255,0.08); margin: 1.8em 0; }
  .briefing-md table { width: 100%; border-collapse: collapse; margin: 1em 0; font-size: 13px; }
  .briefing-md th, .briefing-md td { border: 1px solid rgba(255,255,255,0.1); padding: 8px 10px; text-align: left; }
  .briefing-md th { background: rgba(255,255,255,0.04); color: #e4e4e7; font-weight: 700; }

  /* html email */
  .briefing-html { display: flex; justify-content: center; }
  .briefing-html > div { max-width: 660px; width: 100%; border-radius: 13px; overflow: hidden; }
  .briefing-html img { max-width: 100%; height: auto; }
`;

/* ══════════════════════════════════ SPOTLIGHT CARD ══════════ */

const SpotlightCard = memo(function SpotlightCard({
  card,
  onSelect,
  onDelete,
  isDeleting,
}: {
  card: BriefingCard;
  onSelect: (id: string) => void;
  onDelete: (id: string, e: React.MouseEvent) => void;
  isDeleting: boolean;
}) {
  const elRef = useRef<HTMLDivElement>(null);
  const m = srcMeta(card.source);
  const Icon = m.icon;

  const trackMouse = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    const el = elRef.current;
    if (!el) return;
    const r = el.getBoundingClientRect();
    el.style.setProperty('--sx', `${((e.clientX - r.left) / r.width) * 100}%`);
    el.style.setProperty('--sy', `${((e.clientY - r.top) / r.height) * 100}%`);
  }, []);

  return (
    <motion.div
      ref={elRef}
      role="button"
      tabIndex={0}
      aria-label={`Ler: ${card.title}`}
      className="db-card"
      style={{ '--ac': m.accent, '--ar': m.rgb } as React.CSSProperties}
      onMouseMove={trackMouse}
      onClick={() => onSelect(card.id)}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') onSelect(card.id); }}
      variants={{
        hidden: { opacity: 0, y: 16, scale: 0.97 },
        show:   { opacity: 1, y: 0,  scale: 1,    transition: SPRING },
      }}
      whileHover={{ y: -3 }}
      whileTap={{ scale: 0.985 }}
    >
      {/* delete */}
      <button
        className="db-del"
        aria-label="Remover"
        disabled={isDeleting}
        onClick={(e) => onDelete(card.id, e)}
      >
        <Trash2 style={{ width: 13, height: 13 }} />
      </button>

      {/* source row */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
        <motion.div
          className="db-chip"
          whileHover={{ scale: 1.1, rotate: 7 }}
          transition={{ type: 'spring', stiffness: 420, damping: 14 }}
        >
          <Icon style={{ width: 15, height: 15, color: m.accent }} />
        </motion.div>
        <span className="db-src-label">{m.label}</span>
      </div>

      {/* title */}
      <div className="db-card-title">{card.title}</div>

      {/* footer */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 14 }}>
        <span className="db-foot db-mono">
          <Clock style={{ width: 11, height: 11 }} />
          {fmtTime(card.createdAt)}
        </span>
        <span className="db-read">
          Ler <ArrowRight style={{ width: 12, height: 12 }} />
        </span>
      </div>
    </motion.div>
  );
});

/* ══════════════════════════════════ MAIN COMPONENT ══════════ */

export default function DailyBriefingsClient() {
  const [selectedId,   setSelectedId]   = useState<string | null>(null);
  const [deletingId,   setDeletingId]   = useState<string | null>(null);
  const [activeSource, setActiveSource] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const {
    data: cards, isLoading, isError, refetch, isFetching,
  } = useQuery({
    queryKey: ['daily-briefings', 'today'],
    queryFn: dailyBriefingsService.getToday,
    staleTime: 60_000,
  });

  const { data: detail, isLoading: detailLoading } = useQuery({
    queryKey: ['daily-briefings', 'detail', selectedId],
    queryFn: () => dailyBriefingsService.getById(selectedId as string),
    enabled: !!selectedId,
  });

  const handleDelete = useCallback(async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!window.confirm('Remover este briefing?')) return;
    setDeletingId(id);
    try {
      await dailyBriefingsService.remove(id);
      await queryClient.invalidateQueries({ queryKey: ['daily-briefings', 'today'] });
    } finally {
      setDeletingId(null);
    }
  }, [queryClient]);

  const today = new Date().toLocaleDateString('pt-BR', {
    weekday: 'long', day: 'numeric', month: 'long',
  });

  // source counts for filter pills
  const counts = cards?.reduce<Record<string, number>>((acc, c) => {
    acc[c.source] = (acc[c.source] ?? 0) + 1;
    return acc;
  }, {}) ?? {};
  const sources = Object.keys(counts);

  const visible = activeSource
    ? (cards ?? []).filter(c => c.source === activeSource)
    : (cards ?? []);

  return (
    <div className="db">
      <style>{CSS}</style>

      <AnimatePresence mode="wait">

        {/* ══════════ DETAIL ══════════ */}
        {selectedId ? (
          <motion.div
            key="detail"
            initial={{ opacity: 0, x: 26 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: 26 }}
            transition={{ duration: 0.28, ease: EASE }}
          >
            <button className="db-btn" style={{ marginBottom: 26 }} onClick={() => setSelectedId(null)}>
              <ArrowLeft style={{ width: 14, height: 14 }} /> Voltar
            </button>

            <div className="db-read-view">
              {detailLoading || !detail ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
                  {[90, 78, 84, 62, 71, 55].map((w, i) => (
                    <div key={i} className="db-skel" style={{ height: 15, width: `${w}%`, borderRadius: 7 }} />
                  ))}
                </div>
              ) : (() => {
                const m = srcMeta(detail.source);
                const Icon = m.icon;
                return (
                  <>
                    <div
                      style={{
                        display: 'flex', alignItems: 'flex-start', gap: 14,
                        paddingBottom: 22, borderBottom: '1px solid rgba(255,255,255,0.07)',
                        marginBottom: 28,
                      }}
                    >
                      <div
                        className="db-detail-chip"
                        style={{ '--ar': m.rgb } as React.CSSProperties}
                      >
                        <Icon style={{ width: 20, height: 20, color: m.accent }} />
                      </div>
                      <div style={{ flex: 1 }}>
                        <span className="db-src-label" style={{ color: m.accent }}>{m.label}</span>
                        <div className="db-detail-title">{detail.title}</div>
                        <div className="db-foot db-mono" style={{ marginTop: 7 }}>
                          <Clock style={{ width: 12, height: 12 }} />
                          {fmtTime(detail.createdAt)}
                        </div>
                      </div>
                    </div>

                    <motion.div
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ delay: 0.07, duration: 0.38, ease: EASE }}
                    >
                      <BriefingContent content={detail.content} format={detail.format} />
                    </motion.div>

                    <div style={{ marginTop: 38, paddingTop: 20, borderTop: '1px solid rgba(255,255,255,0.07)' }}>
                      <button className="db-btn" onClick={() => setSelectedId(null)}>
                        <ArrowLeft style={{ width: 14, height: 14 }} /> Voltar
                      </button>
                    </div>
                  </>
                );
              })()}
            </div>
          </motion.div>

        ) : (

          /* ══════════ LIST ══════════ */
          <motion.div
            key="list"
            initial={{ opacity: 0, x: -12 }}
            animate={{ opacity: 1, x: 0 }}
            exit={{ opacity: 0, x: -12 }}
            transition={{ duration: 0.26, ease: EASE }}
          >
            {/* Header */}
            <div style={{
              display: 'flex', alignItems: 'flex-start',
              justifyContent: 'space-between', gap: 16,
              marginBottom: 22, flexWrap: 'wrap',
            }}>
              <div>
                <h1 className="db-heading">Daily Briefings</h1>
                <div className="db-sub">
                  <Calendar style={{ width: 12, height: 12 }} />
                  {today}
                  {(cards?.length ?? 0) > 0 && (
                    <>
                      <span style={{ color: '#27272a' }}>·</span>
                      <span className="db-mono" style={{ color: '#3f3f46' }}>
                        {cards!.length} {cards!.length === 1 ? 'briefing' : 'briefings'}
                      </span>
                    </>
                  )}
                </div>
              </div>
              <button className="db-btn" onClick={() => refetch()}>
                <RefreshCw
                  className={isFetching ? 'animate-spin' : ''}
                  style={{ width: 13, height: 13 }}
                />
                Atualizar
              </button>
            </div>

            {/* Source filter pills */}
            {sources.length > 1 && (
              <div className="db-pills" style={{ marginBottom: 18 }}>
                <button
                  className={`db-pill ${!activeSource ? 'db-pill-on' : ''}`}
                  style={!activeSource
                    ? { '--pc': '#00D4AA', '--pr': '0,212,170' } as React.CSSProperties
                    : undefined}
                  onClick={() => setActiveSource(null)}
                >
                  <span className="db-dot" />
                  Todos
                  <span className="db-mono" style={{ opacity: 0.65 }}>{cards?.length}</span>
                </button>

                {sources.map(src => {
                  const m = srcMeta(src);
                  const on = activeSource === src;
                  return (
                    <button
                      key={src}
                      className={`db-pill ${on ? 'db-pill-on' : ''}`}
                      style={on
                        ? { '--pc': m.accent, '--pr': m.rgb } as React.CSSProperties
                        : undefined}
                      onClick={() => setActiveSource(on ? null : src)}
                    >
                      <span className="db-dot" style={{ background: m.accent }} />
                      {m.short}
                      <span className="db-mono" style={{ opacity: 0.65 }}>{counts[src]}</span>
                    </button>
                  );
                })}
              </div>
            )}

            {/* Loading */}
            {isLoading && (
              <div className="db-grid">
                {[156, 138, 172, 148].map((h, i) => (
                  <div key={i} className="db-skel" style={{ height: h }} />
                ))}
              </div>
            )}

            {/* Error */}
            {isError && (
              <div className="db-empty" style={{ borderColor: 'rgba(248,113,113,0.22)' }}>
                <div style={{ color: '#f87171', fontWeight: 600, fontSize: 14 }}>
                  Não foi possível carregar os briefings.
                </div>
                <button className="db-btn" onClick={() => refetch()}>Tentar novamente</button>
              </div>
            )}

            {/* Empty state */}
            {!isLoading && !isError && visible.length === 0 && (
              <div className="db-empty">
                <motion.div
                  animate={{ y: [0, -8, 0] }}
                  transition={{ duration: 3.2, repeat: Infinity, ease: 'easeInOut' }}
                >
                  <Newspaper style={{ width: 36, height: 36, color: '#3f3f46' }} />
                </motion.div>
                <div style={{ color: '#a1a1aa', fontWeight: 600, fontSize: 14 }}>
                  {activeSource
                    ? `Nenhum briefing de "${srcMeta(activeSource).label}" hoje`
                    : 'Nenhum briefing hoje ainda'}
                </div>
                <div style={{ color: '#52525b', fontSize: 12, maxWidth: 340, lineHeight: 1.65 }}>
                  Os briefings aparecem automaticamente quando os agentes rodam — manhã e fim de tarde.
                </div>
              </div>
            )}

            {/* Cards grid */}
            {!isLoading && visible.length > 0 && (
              <motion.div
                className="db-grid"
                initial="hidden"
                animate="show"
                variants={{
                  hidden: {},
                  show: { transition: { staggerChildren: 0.055, delayChildren: 0.02 } },
                }}
              >
                {visible.map(card => (
                  <SpotlightCard
                    key={card.id}
                    card={card}
                    onSelect={setSelectedId}
                    onDelete={handleDelete}
                    isDeleting={deletingId === card.id}
                  />
                ))}
              </motion.div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
