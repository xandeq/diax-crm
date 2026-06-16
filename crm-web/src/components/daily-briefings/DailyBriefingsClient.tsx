'use client';

import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Newspaper, Clock, RefreshCw, ArrowRight, ArrowLeft, Trash2,
  Sparkles, Code2, ListChecks, Calendar, TrendingUp, BarChart2, Activity, Bot,
} from 'lucide-react';
import { dailyBriefingsService } from '@/services/dailyBriefingsService';
import { BriefingContent } from './BriefingContent';

/* ── visual por fonte (icone + cor) ───────────────────────────── */
const SOURCE_VISUAL: Record<string, { label: string; icon: typeof Sparkles; color: string }> = {
  // ── IA Briefings (routines remotas) ──
  'ia-anthropic':    { label: 'IA — Anthropic & MCP',         icon: Sparkles,    color: '#60a5fa' },
  'ia-openai':       { label: 'IA — OpenAI & Codex',          icon: Code2,       color: '#34d399' },
  // ── CRM Operador ──
  'crm-tarefas':     { label: 'CRM — Tarefas & Finanças',     icon: ListChecks,  color: '#fbbf24' },
  // ── InvestIQ ──
  'investiq-manha':  { label: 'InvestIQ — Análise de Mercado',icon: TrendingUp,  color: '#a78bfa' },
  'investiq-tarde':  { label: 'InvestIQ — Resumo do Dia',     icon: BarChart2,   color: '#818cf8' },
  // ── Trends ──
  'trends-semanal':  { label: 'Trends — Momentum Semanal',    icon: Activity,    color: '#fb923c' },
  // ── Sessão ──
  'briefing-sessao': { label: 'Briefing — Sessão do Dia',     icon: Bot,         color: '#2dd4bf' },
  // ── Legado (backwards compat) ──
  'claude-ai':       { label: 'IA — Anthropic & MCP',         icon: Sparkles,    color: '#60a5fa' },
  'codex-chatgpt':   { label: 'IA — OpenAI & Codex',          icon: Code2,       color: '#34d399' },
  'tarefas-ias':     { label: 'CRM — Tarefas & Finanças',     icon: ListChecks,  color: '#fbbf24' },
};
function visual(source: string) {
  return SOURCE_VISUAL[source] ?? { label: source, icon: Newspaper, color: '#00D4AA' };
}

function fmtTime(iso: string): string {
  try { return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }); }
  catch { return ''; }
}

const EASE = [0.16, 1, 0.3, 1] as [number, number, number, number];

const CSS = `
  .db { font-family: var(--font-jakarta), var(--font-inter), -apple-system, sans-serif; color: #e4e4e7; }
  .db-num { font-family: var(--font-mono), ui-monospace, monospace; font-variant-numeric: tabular-nums; }

  .db-title { font-size: 24px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.02em; display: flex; align-items: center; gap: 11px; }
  .db-sub { font-size: 13px; color: #71717a; margin-top: 3px; }

  .db-btn { display: inline-flex; align-items: center; gap: 7px; padding: 9px 15px; border-radius: 11px; border: 1px solid rgba(255,255,255,0.1); background: rgba(255,255,255,0.05); color: #a1a1aa; font-size: 13px; font-weight: 600; cursor: pointer; transition: color .15s, border-color .15s, background .15s; }
  .db-btn:hover { color: #f4f4f5; border-color: rgba(255,255,255,0.22); background: rgba(255,255,255,0.08); }

  .db-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(290px, 1fr)); gap: 16px; }

  .db-card { position: relative; display: flex; flex-direction: column; min-height: 172px; padding: 20px 20px 16px; border-radius: 18px; background: rgba(255,255,255,0.035); border: 1px solid rgba(255,255,255,0.08); cursor: pointer; overflow: hidden; }
  .db-card::before { content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 3px; background: var(--accent); opacity: 0.85; }
  .db-card::after { content: ''; position: absolute; inset: 0; background: radial-gradient(120% 80% at 0% 0%, var(--accent), transparent 55%); opacity: 0.06; pointer-events: none; }

  .db-chip { width: 40px; height: 40px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
  .db-badge { font-size: 11px; font-weight: 700; letter-spacing: 0.01em; }
  .db-card-title { font-size: 14.5px; font-weight: 600; color: #f4f4f5; line-height: 1.45; display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; overflow: hidden; }
  .db-foot { font-size: 11px; color: #52525b; }
  .db-read { display: inline-flex; align-items: center; gap: 3px; font-weight: 600; }

  .db-trash { position: absolute; top: 12px; right: 12px; display: flex; padding: 7px; border-radius: 9px; border: none; background: rgba(255,255,255,0.04); color: #52525b; cursor: pointer; opacity: 0; transition: opacity .15s, color .15s, background .15s; }
  .db-card:hover .db-trash { opacity: 1; }
  .db-trash:hover { color: #f87171; background: rgba(248,113,113,0.12); }

  .db-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; padding: 72px 20px; border-radius: 18px; border: 1px dashed rgba(255,255,255,0.12); background: rgba(255,255,255,0.02); text-align: center; }

  .db-skel { height: 172px; border-radius: 18px; background: linear-gradient(90deg, rgba(255,255,255,0.04), rgba(255,255,255,0.08), rgba(255,255,255,0.04)); background-size: 200% 100%; animation: db-sh 1.6s linear infinite; }
  @keyframes db-sh { to { background-position: -200% 0; } }

  /* ── leitura (detalhe) ── */
  .db-reading { max-width: 800px; margin: 0 auto; }
  .db-detail-head { display: flex; align-items: flex-start; gap: 14px; padding: 22px 0 20px; border-bottom: 1px solid rgba(255,255,255,0.07); margin-bottom: 26px; }
  .db-detail-title { font-size: 20px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.01em; line-height: 1.3; }

  /* markdown legivel no escuro */
  .briefing-md { color: #cbced4; font-size: 15px; line-height: 1.78; }
  .briefing-md > :first-child { margin-top: 0; }
  .briefing-md h1, .briefing-md h2, .briefing-md h3, .briefing-md h4 { color: #f4f4f5; font-weight: 700; line-height: 1.3; margin: 1.6em 0 0.6em; letter-spacing: -0.01em; }
  .briefing-md h1 { font-size: 24px; } .briefing-md h2 { font-size: 19px; padding-bottom: 6px; border-bottom: 1px solid rgba(255,255,255,0.07); } .briefing-md h3 { font-size: 16px; color: #e4e4e7; }
  .briefing-md p { margin: 0.8em 0; } .briefing-md strong { color: #f4f4f5; font-weight: 700; }
  .briefing-md a { color: #00D4AA; text-decoration: none; border-bottom: 1px solid rgba(0,212,170,0.35); word-break: break-word; }
  .briefing-md a:hover { border-bottom-color: #00D4AA; }
  .briefing-md ul, .briefing-md ol { padding-left: 1.4em; margin: 0.7em 0; } .briefing-md li { margin: 0.35em 0; }
  .briefing-md code { font-family: var(--font-mono), monospace; font-size: 13px; background: rgba(255,255,255,0.08); padding: 2px 6px; border-radius: 6px; color: #5eead4; }
  .briefing-md pre { background: #0b1410; border: 1px solid rgba(255,255,255,0.09); border-radius: 12px; padding: 16px; overflow-x: auto; margin: 1em 0; }
  .briefing-md pre code { background: none; padding: 0; color: #cbd5e1; font-size: 12.5px; line-height: 1.6; }
  .briefing-md blockquote { border-left: 3px solid #00D4AA; padding-left: 14px; margin: 1em 0; color: #a1a1aa; }
  .briefing-md hr { border: none; border-top: 1px solid rgba(255,255,255,0.08); margin: 1.6em 0; }
  .briefing-md table { width: 100%; border-collapse: collapse; margin: 1em 0; font-size: 13px; }
  .briefing-md th, .briefing-md td { border: 1px solid rgba(255,255,255,0.1); padding: 8px 10px; text-align: left; }
  .briefing-md th { background: rgba(255,255,255,0.04); color: #e4e4e7; }

  .briefing-html { display: flex; justify-content: center; }
  .briefing-html > div { max-width: 660px; width: 100%; border-radius: 14px; overflow: hidden; }
  .briefing-html img { max-width: 100%; height: auto; }
`;

export default function DailyBriefingsClient() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const queryClient = useQueryClient();

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

  const today = new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

  return (
    <div className="db">
      <style>{CSS}</style>

      <AnimatePresence mode="wait">
        {/* ══════════ DETALHE (leitura + voltar) ══════════ */}
        {selectedId ? (
          <motion.div key="detail" initial={{ opacity: 0, x: 24 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: 24 }} transition={{ duration: 0.32, ease: EASE }}>
            <button className="db-btn" onClick={() => setSelectedId(null)}>
              <ArrowLeft className="h-4 w-4" /> Voltar
            </button>

            <div className="db-reading">
              {detailLoading || !detail ? (
                <div style={{ marginTop: 28, display: 'flex', flexDirection: 'column', gap: 14 }}>
                  {[0, 1, 2, 3, 4, 5].map(i => <div key={i} style={{ height: 18, borderRadius: 6, width: `${90 - i * 8}%`, background: 'rgba(255,255,255,0.06)' }} />)}
                </div>
              ) : (() => {
                const v = visual(detail.source);
                const Icon = v.icon;
                return (
                  <>
                    <div className="db-detail-head">
                      <div className="db-chip" style={{ background: `${v.color}1f` }}><Icon className="h-5 w-5" style={{ color: v.color }} /></div>
                      <div style={{ flex: 1 }}>
                        <span className="db-badge" style={{ color: v.color }}>{v.label}</span>
                        <div className="db-detail-title">{detail.title}</div>
                        <div className="db-foot db-num" style={{ marginTop: 6, display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                          <Clock className="h-3 w-3" /> {fmtTime(detail.createdAt)}
                        </div>
                      </div>
                    </div>
                    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.08, duration: 0.4, ease: EASE }}>
                      <BriefingContent content={detail.content} format={detail.format} />
                    </motion.div>
                    <div style={{ marginTop: 36, paddingTop: 18, borderTop: '1px solid rgba(255,255,255,0.07)' }}>
                      <button className="db-btn" onClick={() => setSelectedId(null)}><ArrowLeft className="h-4 w-4" /> Voltar</button>
                    </div>
                  </>
                );
              })()}
            </div>
          </motion.div>
        ) : (
          /* ══════════ LISTA ══════════ */
          <motion.div key="list" initial={{ opacity: 0, x: -16 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -16 }} transition={{ duration: 0.3, ease: EASE }}>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16, marginBottom: 24, flexWrap: 'wrap' }}>
              <div>
                <h1 className="db-title"><Newspaper className="h-6 w-6" style={{ color: '#00D4AA' }} /> Daily Briefings</h1>
                <div className="db-sub" style={{ textTransform: 'capitalize', display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                  <Calendar className="h-3.5 w-3.5" /> {today}
                  {(cards?.length ?? 0) > 0 && <span style={{ color: '#3f3f46' }}>·</span>}
                  {(cards?.length ?? 0) > 0 && <span className="db-num">{cards!.length} {cards!.length === 1 ? 'briefing' : 'briefings'}</span>}
                </div>
              </div>
              <button className="db-btn" onClick={() => refetch()}>
                <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? 'animate-spin' : ''}`} /> Atualizar
              </button>
            </div>

            {isLoading && <div className="db-grid">{[0, 1, 2].map(i => <div key={i} className="db-skel" />)}</div>}

            {isError && (
              <div className="db-empty" style={{ borderColor: 'rgba(248,113,113,0.3)' }}>
                <span style={{ color: '#f87171', fontSize: 14, fontWeight: 600 }}>Não foi possível carregar os briefings.</span>
                <button className="db-btn" onClick={() => refetch()}>Tentar de novo</button>
              </div>
            )}

            {!isLoading && !isError && (cards?.length ?? 0) === 0 && (
              <div className="db-empty">
                <motion.div animate={{ y: [0, -6, 0] }} transition={{ duration: 2.6, repeat: Infinity, ease: 'easeInOut' }}>
                  <Newspaper className="h-10 w-10" style={{ color: '#3f3f46' }} />
                </motion.div>
                <span style={{ color: '#a1a1aa', fontSize: 14, fontWeight: 600 }}>Nenhum briefing hoje ainda</span>
                <span style={{ color: '#52525b', fontSize: 12, maxWidth: 380 }}>
                  Os briefings do dia aparecem aqui automaticamente quando os agentes rodam (manhã). Só o dia corrente é exibido.
                </span>
              </div>
            )}

            {!isLoading && (cards?.length ?? 0) > 0 && (
              <motion.div className="db-grid" initial="hidden" animate="show" variants={{ hidden: {}, show: { transition: { staggerChildren: 0.07 } } }}>
                {cards!.map(card => {
                  const v = visual(card.source);
                  const Icon = v.icon;
                  return (
                    <motion.div
                      key={card.id}
                      role="button" tabIndex={0}
                      onClick={() => setSelectedId(card.id)}
                      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setSelectedId(card.id); }}
                      className="db-card"
                      style={{ '--accent': v.color } as React.CSSProperties}
                      variants={{ hidden: { opacity: 0, y: 22, scale: 0.96 }, show: { opacity: 1, y: 0, scale: 1, transition: { type: 'spring', stiffness: 320, damping: 24 } } }}
                      whileHover={{ y: -3, borderColor: `${v.color}55`, boxShadow: '0 18px 42px rgba(0,0,0,0.45)' }}
                      whileTap={{ scale: 0.985 }}
                    >
                      <button className="db-trash" aria-label="Remover briefing" disabled={deletingId === card.id} onClick={(e) => handleDelete(card.id, e)}>
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 11, marginBottom: 14 }}>
                        <motion.div className="db-chip" style={{ background: `${v.color}1f` }} whileHover={{ rotate: 8, scale: 1.08 }} transition={{ type: 'spring', stiffness: 380, damping: 14 }}>
                          <Icon className="h-5 w-5" style={{ color: v.color }} />
                        </motion.div>
                        <span className="db-badge" style={{ color: v.color }}>{v.label}</span>
                      </div>
                      <div className="db-card-title" style={{ flex: 1, paddingRight: 8 }}>{card.title}</div>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 14 }}>
                        <span className="db-foot db-num" style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}>
                          <Clock className="h-3 w-3" /> {fmtTime(card.createdAt)}
                        </span>
                        <span className="db-read db-foot" style={{ color: v.color }}>Ler <ArrowRight className="h-3 w-3" /></span>
                      </div>
                    </motion.div>
                  );
                })}
              </motion.div>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
