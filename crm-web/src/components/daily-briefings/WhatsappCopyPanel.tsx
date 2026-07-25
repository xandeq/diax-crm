'use client';

import { useMemo, useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { MessageCircle, Copy, Check, ChevronDown } from 'lucide-react';
import {
  splitBriefingSections,
  sectionToWhatsApp,
  splitSectionItems,
  briefingToWhatsApp,
} from '@/lib/briefingWhatsapp';

interface Props {
  /** HTML bruto do briefing (email-safe). */
  content: string;
  accent: string;
  rgb: string;
}

async function copyToClipboard(text: string): Promise<boolean> {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(text);
      return true;
    }
  } catch {
    /* cai no fallback */
  }
  try {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand('copy');
    document.body.removeChild(ta);
    return ok;
  } catch {
    return false;
  }
}

/** Primeira linha do item, sem marcadores, para rótulo compacto. */
function itemLabel(text: string): string {
  const first = text.split('\n', 1)[0].replace(/[*_~]/g, '').trim();
  return first.length > 78 ? first.slice(0, 77) + '…' : first;
}

const CSS = `
  .wap {
    border-radius: 14px;
    border: 1px solid rgba(var(--wr), 0.22);
    background:
      linear-gradient(180deg, rgba(var(--wr), 0.05), rgba(var(--wr), 0.015)),
      rgba(255,255,255,0.012);
    padding: 15px 15px 8px;
    margin-bottom: 26px;
  }
  .wap-head {
    display: flex; align-items: center; gap: 10px;
    margin-bottom: 4px;
  }
  .wap-ic {
    width: 30px; height: 30px; border-radius: 9px; flex-shrink: 0;
    display: flex; align-items: center; justify-content: center;
    background: rgba(var(--wr), 0.15);
  }
  .wap-h { flex: 1; min-width: 0; }
  .wap-title { font-size: 13px; font-weight: 700; color: #e9e9ee; letter-spacing: -0.01em; }
  .wap-desc { font-size: 11px; color: #6b6b74; margin-top: 1px; }

  .wap-all {
    display: inline-flex; align-items: center; gap: 6px; flex-shrink: 0;
    padding: 7px 12px; border-radius: 9px; cursor: pointer;
    font-size: 12px; font-weight: 700; letter-spacing: -0.005em;
    color: var(--wa); border: 1px solid rgba(var(--wr), 0.3);
    background: rgba(var(--wr), 0.1);
    transition: background .15s, border-color .15s, transform .1s;
  }
  .wap-all:hover { background: rgba(var(--wr), 0.16); border-color: rgba(var(--wr), 0.45); }
  .wap-all:active { transform: scale(0.97); }

  .wap-list { margin-top: 12px; }
  .wap-row {
    display: flex; align-items: center; gap: 10px;
    padding: 9px 4px 9px 2px;
    border-top: 1px solid rgba(255,255,255,0.05);
  }
  .wap-row:first-child { border-top: none; }
  .wap-num {
    font-family: var(--font-mono), ui-monospace, monospace;
    font-size: 11px; color: #4b4b52; width: 18px; flex-shrink: 0;
    font-variant-numeric: tabular-nums; text-align: right;
  }
  .wap-chev {
    display: flex; flex-shrink: 0; padding: 3px; border-radius: 6px;
    border: none; background: transparent; color: #6b6b74; cursor: pointer;
    transition: color .14s, background .14s, transform .2s;
  }
  .wap-chev:hover { color: #c4c4cc; background: rgba(255,255,255,0.05); }
  .wap-chev-open { transform: rotate(180deg); color: var(--wa); }
  .wap-chev-spacer { width: 22px; flex-shrink: 0; }
  .wap-name { flex: 1; min-width: 0; font-size: 12.5px; font-weight: 600; color: #cdcdd4;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .wap-count { font-family: var(--font-mono), ui-monospace, monospace;
    font-size: 10.5px; color: #4b4b52; flex-shrink: 0; font-variant-numeric: tabular-nums; }

  .wap-copy {
    display: inline-flex; align-items: center; gap: 5px; flex-shrink: 0;
    padding: 5px 10px; border-radius: 8px; cursor: pointer;
    font-size: 11.5px; font-weight: 600;
    color: #8f8f98; border: 1px solid rgba(255,255,255,0.09);
    background: rgba(255,255,255,0.03);
    transition: color .14s, border-color .14s, background .14s, transform .1s;
  }
  .wap-copy:hover { color: #e4e4e7; border-color: rgba(255,255,255,0.18); background: rgba(255,255,255,0.06); }
  .wap-copy:active { transform: scale(0.96); }
  .wap-copy-done { color: var(--wa) !important; border-color: rgba(var(--wr), 0.4) !important; background: rgba(var(--wr), 0.12) !important; }

  /* itens dentro de uma seção */
  .wap-items { overflow: hidden; }
  .wap-item {
    display: flex; align-items: center; gap: 10px;
    padding: 7px 4px 7px 30px;
  }
  .wap-item-dot { width: 4px; height: 4px; border-radius: 50%; flex-shrink: 0;
    background: rgba(var(--wr), 0.6); }
  .wap-item-txt { flex: 1; min-width: 0; font-size: 12px; color: #a7a7b0;
    overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
`;

interface SectionVM {
  title: string;
  text: string;
  items: string[];
}

export function WhatsappCopyPanel({ content, accent, rgb }: Props) {
  const sections = useMemo<SectionVM[]>(() => {
    return splitBriefingSections(content)
      .map((s) => ({ title: s.title, text: sectionToWhatsApp(s), items: splitSectionItems(s) }))
      .filter((s) => s.text.trim().length > 0);
  }, [content]);

  const [doneKey, setDoneKey] = useState<string | null>(null);
  const [open, setOpen] = useState<Record<string, boolean>>({});

  const flash = useCallback((key: string) => {
    setDoneKey(key);
    window.setTimeout(() => setDoneKey((k) => (k === key ? null : k)), 1600);
  }, []);

  const copyText = useCallback(
    async (key: string, text: string) => {
      if (await copyToClipboard(text)) flash(key);
    },
    [flash],
  );

  const copyAll = useCallback(async () => {
    if (await copyToClipboard(briefingToWhatsApp(content))) flash('__all__');
  }, [content, flash]);

  if (sections.length === 0) return null;

  const hasTitles = sections.some((s) => s.title.trim().length > 0);
  const allDone = doneKey === '__all__';

  return (
    <div className="wap" style={{ '--wa': accent, '--wr': rgb } as React.CSSProperties}>
      <style>{CSS}</style>

      <div className="wap-head">
        <span className="wap-ic">
          <MessageCircle style={{ width: 15, height: 15, color: accent }} />
        </span>
        <div className="wap-h">
          <div className="wap-title">Copiar para o WhatsApp</div>
          <div className="wap-desc">
            {hasTitles
              ? `${sections.length} ${sections.length === 1 ? 'bloco' : 'blocos'} · abra um bloco p/ copiar item a item`
              : 'Texto formatado · URLs clicáveis'}
          </div>
        </div>
        <button className={`wap-all ${allDone ? 'wap-copy-done' : ''}`} onClick={copyAll}>
          {allDone ? <Check style={{ width: 13, height: 13 }} /> : <Copy style={{ width: 13, height: 13 }} />}
          {allDone ? 'Copiado' : 'Copiar tudo'}
        </button>
      </div>

      {hasTitles && (
        <div className="wap-list">
          {sections.map((s, i) => {
            const key = `${i}:${s.title}`;
            const done = doneKey === key;
            const isOpen = !!open[key];
            const hasItems = s.items.length > 0;
            return (
              <motion.div
                key={key}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.02 * i, duration: 0.32, ease: [0.16, 1, 0.3, 1] }}
              >
                <div className="wap-row">
                  <span className="wap-num">{String(i + 1).padStart(2, '0')}</span>
                  {hasItems ? (
                    <button
                      className={`wap-chev ${isOpen ? 'wap-chev-open' : ''}`}
                      onClick={() => setOpen((o) => ({ ...o, [key]: !o[key] }))}
                      aria-label={isOpen ? 'Recolher itens' : 'Ver itens'}
                      aria-expanded={isOpen}
                    >
                      <ChevronDown style={{ width: 14, height: 14 }} />
                    </button>
                  ) : (
                    <span className="wap-chev-spacer" />
                  )}
                  <span className="wap-name" title={s.title || 'Bloco'}>
                    {s.title || 'Bloco'}
                  </span>
                  {hasItems && <span className="wap-count">{s.items.length} itens</span>}
                  <button
                    className={`wap-copy ${done ? 'wap-copy-done' : ''}`}
                    onClick={() => copyText(key, s.text)}
                    aria-label={`Copiar bloco ${s.title || i + 1} para o WhatsApp`}
                  >
                    {done ? <Check style={{ width: 12, height: 12 }} /> : <Copy style={{ width: 12, height: 12 }} />}
                    {done ? 'Copiado' : 'Copiar'}
                  </button>
                </div>

                <AnimatePresence initial={false}>
                  {isOpen && hasItems && (
                    <motion.div
                      className="wap-items"
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      transition={{ duration: 0.24, ease: [0.16, 1, 0.3, 1] }}
                    >
                      {s.items.map((it, j) => {
                        const ikey = `${key}#${j}`;
                        const idone = doneKey === ikey;
                        return (
                          <div className="wap-item" key={ikey}>
                            <span className="wap-item-dot" />
                            <span className="wap-item-txt" title={itemLabel(it)}>
                              {itemLabel(it)}
                            </span>
                            <button
                              className={`wap-copy ${idone ? 'wap-copy-done' : ''}`}
                              onClick={() => copyText(ikey, it)}
                              aria-label={`Copiar item ${j + 1} do bloco ${s.title}`}
                            >
                              {idone ? <Check style={{ width: 12, height: 12 }} /> : <Copy style={{ width: 12, height: 12 }} />}
                              {idone ? 'Copiado' : 'Copiar'}
                            </button>
                          </div>
                        );
                      })}
                    </motion.div>
                  )}
                </AnimatePresence>
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
