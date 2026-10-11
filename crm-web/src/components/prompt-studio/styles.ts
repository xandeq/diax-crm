/**
 * Estilos do Prompt Studio. Segue o padrão do AppShell (CSS em string) e só
 * referencia os tokens existentes em globals.css (--diax-*, --chart-*).
 */
export const STUDIO_CSS = `
.ps {
  --ps-bg: var(--diax-bg);
  --ps-pane: var(--diax-bg-sidebar);
  --ps-surface: var(--diax-bg-card);
  --ps-surface-2: var(--diax-bg-card-2);
  --ps-border: var(--diax-border);
  --ps-border-2: var(--diax-border-2);
  --ps-t1: var(--diax-text-1); --ps-t2: var(--diax-text-2); --ps-t3: var(--diax-text-3); --ps-t4: var(--diax-text-4);
  --ps-accent: var(--diax-green); --ps-accent-hover: var(--diax-green-hover); --ps-accent-dim: var(--diax-green-dim);
  --ps-var: #F2B54A; --ps-var-bg: rgba(242,181,74,.14);
  --ps-miss: #F87171; --ps-miss-bg: rgba(248,113,113,.14);
  --ps-fav: #F5B83D;
  --ps-add: rgba(16,185,129,.22); --ps-del: rgba(248,113,113,.22);
  --ps-ease: var(--diax-transition-fast);
  --ps-mono: var(--font-mono), "JetBrains Mono", ui-monospace, monospace;

  height: calc(100dvh - 56px - 64px);
  min-height: 560px;
  display: grid;
  grid-template-columns: 210px minmax(260px, 320px) minmax(0, 1fr) 290px;
  border: 1px solid var(--ps-border);
  border-radius: 14px;
  overflow: hidden;
  background: var(--ps-bg);
  color: var(--ps-t2);
  font-size: 13.5px;
}
.ps button { font: inherit; color: inherit; }
.ps :focus-visible { outline: 2px solid var(--ps-accent); outline-offset: 2px; }
.ps-pane { min-height: 0; overflow: auto; }
.ps-pane::-webkit-scrollbar { width: 6px; }
.ps-pane::-webkit-scrollbar-thumb { background: rgba(255,255,255,.08); border-radius: 999px; }

/* Sidebar */
.ps-side { background: var(--ps-pane); border-right: 1px solid var(--ps-border); padding: 12px 8px; display: flex; flex-direction: column; gap: 16px; }
.ps-h { margin: 0 8px 6px; font-size: 10.5px; font-weight: 600; letter-spacing: .07em; text-transform: uppercase; color: var(--ps-t3); display: flex; align-items: center; justify-content: space-between; }
.ps-h button { background: none; border: 0; color: var(--ps-t3); padding: 2px; border-radius: 5px; cursor: pointer; display: grid; place-items: center; }
.ps-h button:hover { color: var(--ps-t1); background: var(--ps-surface); }
.ps-nav { display: flex; flex-direction: column; gap: 1px; }
.ps-nav-item { display: flex; align-items: center; gap: 9px; width: 100%; padding: 6px 8px; border: 0; border-radius: 7px; background: transparent; color: var(--ps-t2); font-size: 13px; text-align: left; cursor: pointer; transition: background var(--ps-ease); }
.ps-nav-item:hover { background: var(--ps-surface); }
.ps-nav-item[aria-current="true"] { background: var(--ps-accent-dim); color: var(--ps-t1); }
.ps-nav-item .n { margin-left: auto; font-size: 11.5px; color: var(--ps-t3); font-variant-numeric: tabular-nums; }
.ps-dot { width: 9px; height: 9px; border-radius: 3px; flex: none; }
.ps-tagcloud { display: flex; flex-wrap: wrap; gap: 5px; padding: 0 6px; }
.ps-inline-input { margin: 4px 6px 0; display: flex; gap: 6px; }
.ps-inline-input input { flex: 1; min-width: 0; background: var(--ps-bg); border: 1px solid var(--ps-border-2); border-radius: 7px; padding: 5px 8px; font-size: 12.5px; color: var(--ps-t1); outline: none; }
.ps-inline-input input:focus { border-color: var(--ps-accent); }

/* Tags */
.ps-tag { display: inline-flex; align-items: center; gap: 4px; padding: 1px 7px; border-radius: 999px; font-size: 11.5px; font-weight: 500; line-height: 18px; border: 0; white-space: nowrap; }
button.ps-tag { cursor: pointer; }
button.ps-tag[aria-pressed="true"] { outline: 1.5px solid currentColor; }
.ps-tag .x { background: none; border: 0; padding: 0; cursor: pointer; opacity: .7; display: grid; place-items: center; }
.ps-tag .x:hover { opacity: 1; }
.t0{color:#34D399;background:rgba(52,211,153,.13)} .t1{color:#818CF8;background:rgba(129,140,248,.14)}
.t2{color:#FBBF24;background:rgba(251,191,36,.13)} .t3{color:#F87171;background:rgba(248,113,113,.13)}
.t4{color:#C084FC;background:rgba(192,132,252,.13)} .t5{color:#38BDF8;background:rgba(56,189,248,.13)}
.t6{color:#F472B6;background:rgba(244,114,182,.13)} .t7{color:#A3E635;background:rgba(163,230,53,.12)}

/* Lista */
.ps-list { border-right: 1px solid var(--ps-border); display: flex; flex-direction: column; }
.ps-list-head { position: sticky; top: 0; z-index: 1; background: var(--ps-bg); padding: 12px 12px 8px; border-bottom: 1px solid var(--ps-border); display: flex; flex-direction: column; gap: 8px; }
.ps-list-title { display: flex; align-items: center; justify-content: space-between; gap: 6px; }
.ps-list-title h2 { margin: 0; flex: 1 1 auto; min-width: 56px; font-size: 14.5px; font-weight: 600; color: var(--ps-t1); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ps-select { max-width: 104px; background: var(--ps-surface); border: 1px solid var(--ps-border); border-radius: 7px; padding: 4px 6px; font-size: 12px; color: var(--ps-t2); outline: none; }
.ps-select option { background: #0B1510; }
.ps-filter { display: flex; align-items: center; gap: 8px; padding: 6px 9px; border: 1px solid var(--ps-border); border-radius: 8px; background: var(--ps-surface); color: var(--ps-t3); }
.ps-filter:focus-within { border-color: var(--ps-accent); }
.ps-filter input { flex: 1; min-width: 0; border: 0; background: transparent; outline: none; font-size: 13px; color: var(--ps-t1); }
.ps-filter input::placeholder { color: var(--ps-t4); }
.ps-kbd { font: 500 10.5px var(--ps-mono); padding: 0 5px; border: 1px solid var(--ps-border-2); border-radius: 5px; color: var(--ps-t3); }
.ps-rows { list-style: none; margin: 0; padding: 6px; }
.ps-row { display: grid; grid-template-columns: 18px 1fr auto; gap: 2px 8px; align-items: center; padding: 8px 9px; border-radius: 8px; cursor: pointer; border: 1px solid transparent; transition: background var(--ps-ease); }
.ps-row:hover { background: var(--ps-surface); }
.ps-row[aria-selected="true"] { background: var(--ps-accent-dim); border-color: rgba(16,185,129,.28); }
.ps-star { border: 0; background: none; padding: 0; color: var(--ps-t4); display: grid; place-items: center; cursor: pointer; }
.ps-star.on { color: var(--ps-fav); }
.ps-row .ttl { font-weight: 600; color: var(--ps-t1); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ps-row .meta { font-size: 11px; color: var(--ps-t3); white-space: nowrap; font-variant-numeric: tabular-nums; }
.ps-row .sub { grid-column: 2 / 4; display: flex; align-items: center; gap: 5px; min-width: 0; overflow: hidden; }
.ps-row .prev { font-size: 12px; color: var(--ps-t3); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-width: 0; }
.ps-mini { font: 500 10px var(--ps-mono); color: var(--ps-t3); border: 1px solid var(--ps-border-2); border-radius: 5px; padding: 0 4px; white-space: nowrap; }
.ps-empty { padding: 36px 20px; text-align: center; color: var(--ps-t3); font-size: 13px; display: flex; flex-direction: column; align-items: center; gap: 10px; }
.ps-skel { height: 44px; margin: 4px 6px; border-radius: 8px; background: var(--ps-surface); animation: ps-pulse 1.4s ease-in-out infinite; }
@keyframes ps-pulse { 50% { opacity: .5 } }

/* Botões compactos */
.ps-btn { display: inline-flex; align-items: center; gap: 6px; height: 32px; padding: 0 11px; border-radius: 8px; border: 1px solid var(--ps-border); background: var(--ps-surface); color: var(--ps-t1); font-weight: 500; font-size: 12.5px; cursor: pointer; transition: background var(--ps-ease), border-color var(--ps-ease); white-space: nowrap; }
.ps-btn:hover:not(:disabled) { background: var(--ps-surface-2); border-color: var(--ps-border-2); }
.ps-btn:disabled { opacity: .5; cursor: default; }
.ps-btn.primary { background: var(--ps-accent); border-color: transparent; color: #04140D; font-weight: 600; }
.ps-btn.primary:hover:not(:disabled) { background: var(--ps-accent-hover); }
.ps-btn.ghost { background: transparent; border-color: transparent; color: var(--ps-t3); }
.ps-btn.ghost:hover:not(:disabled) { color: var(--ps-t1); background: var(--ps-surface); }
.ps-btn.danger:hover:not(:disabled) { color: #FCA5A5; border-color: rgba(248,113,113,.4); }
.ps-btn.icon { width: 32px; padding: 0; justify-content: center; }

/* Editor */
.ps-editor { display: flex; flex-direction: column; min-width: 0; }
.ps-ed-head { padding: 14px 18px 0; display: flex; flex-direction: column; gap: 10px; }
.ps-ed-top { display: flex; align-items: flex-start; gap: 8px; }
.ps-back { display: none; }
.ps-title-in { width: 100%; border: 0; background: transparent; font-size: 19px; font-weight: 600; color: var(--ps-t1); padding: 2px 0; outline: none; letter-spacing: -.01em; }
.ps-title-in:focus-visible { outline: none; box-shadow: 0 1px 0 var(--ps-accent); }
.ps-slug { display: flex; gap: 6px; align-items: center; font: 11.5px var(--ps-mono); color: var(--ps-t3); flex-wrap: wrap; }
.ps-chips { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; }
.ps-chip { display: inline-flex; align-items: center; gap: 5px; height: 26px; padding: 0 8px; border-radius: 7px; border: 1px solid var(--ps-border); font-size: 12px; color: var(--ps-t2); background: transparent; }
button.ps-chip { cursor: pointer; }
button.ps-chip:hover { border-color: var(--ps-border-2); color: var(--ps-t1); }
.ps-chip select { background: transparent; border: 0; color: inherit; font-size: 12px; outline: none; cursor: pointer; max-width: 160px; }
.ps-chip select option { background: #0B1510; }
.ps-chip.on { color: var(--ps-accent); border-color: rgba(16,185,129,.4); background: var(--ps-accent-dim); }
.ps-tag-input { border: 0; background: transparent; outline: none; color: var(--ps-t1); font-size: 12px; width: 110px; }
.ps-tabs { display: flex; gap: 2px; border-bottom: 1px solid var(--ps-border); overflow-x: auto; }
.ps-tab { border: 0; background: none; padding: 8px 10px; font-size: 13px; color: var(--ps-t3); border-bottom: 2px solid transparent; margin-bottom: -1px; white-space: nowrap; cursor: pointer; }
.ps-tab[aria-selected="true"] { color: var(--ps-t1); border-bottom-color: var(--ps-accent); font-weight: 600; }
.ps-panel { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.ps-ed-wrap { position: relative; flex: 1; min-height: 260px; margin: 12px 18px 0; border: 1px solid var(--ps-border); border-radius: 10px; background: var(--ps-pane); overflow: hidden; }
.ps-ed-wrap:focus-within { border-color: rgba(16,185,129,.45); }
.ps-ed-wrap pre, .ps-ed-wrap textarea { position: absolute; inset: 0; margin: 0; padding: 14px 16px; font: 13.5px/22px var(--ps-mono); white-space: pre-wrap; overflow-wrap: break-word; tab-size: 2; font-variant-ligatures: none; border: 0; overflow: auto; letter-spacing: 0; }
.ps-ed-wrap pre { color: var(--ps-t2); pointer-events: none; }
.ps-ed-wrap textarea { background: transparent; color: transparent; caret-color: var(--ps-t1); resize: none; outline: none; }
.ps-ed-wrap textarea::selection { background: rgba(16,185,129,.28); color: transparent; }
.ps-ed-wrap textarea::placeholder { color: var(--ps-t4); }
.ps-hv { color: var(--ps-var); background: var(--ps-var-bg); border-radius: 4px; box-shadow: inset 0 -1px 0 var(--ps-var); }
.ps-hv-esc { color: var(--ps-t3); }
.ps-status { display: flex; flex-wrap: wrap; gap: 6px 14px; align-items: center; padding: 9px 18px 12px; font-size: 11.5px; color: var(--ps-t3); font-variant-numeric: tabular-nums; }
.ps-status .draft { color: var(--ps-var); }
.ps-status .sp { flex: 1; }
.ps-preview { margin: 12px 18px 18px; padding: 16px 18px; border: 1px solid var(--ps-border); border-radius: 10px; background: var(--ps-pane); overflow: auto; color: var(--ps-t2); flex: 1; }
.ps-preview.raw { font: 13.5px/22px var(--ps-mono); white-space: pre-wrap; }

/* Histórico */
.ps-hist { display: grid; grid-template-columns: 230px 1fr; gap: 14px; padding: 12px 18px 18px; min-height: 0; flex: 1; }
.ps-vlist { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 2px; overflow: auto; }
.ps-vbtn { width: 100%; text-align: left; border: 1px solid transparent; background: none; border-radius: 8px; padding: 8px 9px; display: grid; grid-template-columns: auto 1fr; gap: 1px 8px; cursor: pointer; }
.ps-vbtn:hover { background: var(--ps-surface); }
.ps-vbtn[aria-pressed="true"] { border-color: var(--ps-border-2); background: var(--ps-surface); }
.ps-vbtn:disabled { cursor: default; }
.ps-vn { font: 600 12px var(--ps-mono); color: var(--ps-t1); grid-row: span 2; }
.ps-vnote { font-size: 12.5px; color: var(--ps-t2); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ps-vmeta { font-size: 11px; color: var(--ps-t3); }
.ps-src { font: 500 10px var(--ps-mono); padding: 0 4px; border-radius: 4px; background: var(--ps-surface-2); color: var(--ps-t3); }
.ps-diffbox { border: 1px solid var(--ps-border); border-radius: 10px; background: var(--ps-pane); display: flex; flex-direction: column; min-height: 0; overflow: hidden; }
.ps-diffbar { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 12px; padding: 8px 12px; border-bottom: 1px solid var(--ps-border); font-size: 12px; color: var(--ps-t3); }
.ps-diffbar .sp { flex: 1; }
.ps-plus { color: var(--ps-accent); font-weight: 600; } .ps-minus { color: var(--ps-miss); font-weight: 600; }
.ps-diff { padding: 14px 16px; font: 13px/22px var(--ps-mono); white-space: pre-wrap; overflow: auto; color: var(--ps-t2); }
.ps-diff ins { text-decoration: none; background: var(--ps-add); color: var(--ps-t1); border-radius: 3px; }
.ps-diff del { background: var(--ps-del); color: var(--ps-t3); border-radius: 3px; }

/* Variáveis */
.ps-vars { background: var(--ps-pane); border-left: 1px solid var(--ps-border); display: flex; flex-direction: column; }
.ps-vars-head { padding: 14px 14px 10px; display: flex; align-items: center; justify-content: space-between; gap: 8px; border-bottom: 1px solid var(--ps-border); }
.ps-vars-head h3 { margin: 0; font-size: 13px; font-weight: 600; color: var(--ps-t1); }
.ps-missing { font-size: 11.5px; color: var(--ps-miss); font-weight: 500; }
.ps-missing.ok { color: var(--ps-accent); }
.ps-fields { padding: 12px 14px; display: flex; flex-direction: column; gap: 12px; }
.ps-field label { display: flex; justify-content: space-between; align-items: center; gap: 6px; font-size: 12px; font-weight: 500; color: var(--ps-t2); margin-bottom: 5px; }
.ps-field label code { font: 11px var(--ps-mono); color: var(--ps-var); }
.ps-field input, .ps-field textarea, .ps-field select { width: 100%; border: 1px solid var(--ps-border); background: var(--ps-bg); border-radius: 8px; padding: 7px 9px; font-size: 13px; color: var(--ps-t1); outline: none; transition: border-color var(--ps-ease); }
.ps-field textarea { min-height: 70px; resize: vertical; font: 12.5px/18px var(--ps-mono); }
.ps-field select option { background: #0B1510; }
.ps-field input:focus, .ps-field textarea:focus, .ps-field select:focus { border-color: var(--ps-accent); }
.ps-field.req-empty input, .ps-field.req-empty textarea { border-color: rgba(248,113,113,.6); }
.ps-field .gear { background: none; border: 0; color: var(--ps-t4); cursor: pointer; padding: 0; display: grid; place-items: center; }
.ps-field .gear:hover, .ps-field .gear[aria-expanded="true"] { color: var(--ps-t1); }
.ps-varmeta { margin-top: 6px; padding: 8px; border: 1px dashed var(--ps-border-2); border-radius: 8px; display: grid; grid-template-columns: 1fr 1fr; gap: 6px; }
.ps-varmeta .full { grid-column: 1 / -1; }
.ps-varmeta input, .ps-varmeta select { padding: 5px 7px; font-size: 12px; }
.ps-varmeta .chk { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--ps-t2); }
.ps-varmeta .chk input { width: auto; }
.ps-novars { font-size: 12.5px; color: var(--ps-t3); }
.ps-rend-h { padding: 4px 14px 6px; font-size: 10.5px; font-weight: 600; letter-spacing: .07em; text-transform: uppercase; color: var(--ps-t3); }
.ps-rendered { margin: 0 14px 14px; padding: 12px; border: 1px solid var(--ps-border); border-radius: 8px; background: var(--ps-bg); font: 12px/19px var(--ps-mono); white-space: pre-wrap; max-height: 320px; overflow: auto; color: var(--ps-t2); }
.ps-rendered .ok { background: var(--ps-accent-dim); color: var(--ps-t1); border-radius: 3px; }
.ps-rendered .miss { background: var(--ps-miss-bg); color: var(--ps-miss); border-radius: 3px; }

.ps-center { flex: 1; display: grid; place-items: center; color: var(--ps-t3); text-align: center; padding: 24px; }

/* Responsivo — container queries: o Studio vive dentro do AppShell (sidebar de ~220px),
   então o que importa é a largura disponível para ELE, não a da janela. */
.ps-container { container-type: inline-size; container-name: studio; }
.ps { position: relative; }
.ps-editor { container-type: inline-size; container-name: editor; }

/* < 1400px: sidebar do Studio vira gaveta (botão ☰ na lista); lista + editor + variáveis */
@container studio (max-width: 1399px) {
  .ps { grid-template-columns: minmax(260px, 300px) minmax(0, 1fr) minmax(250px, 280px); }
  .ps-side { display: none; }
  .ps.side-open .ps-side { display: flex; position: absolute; z-index: 20; width: 240px; top: 0; bottom: 0; left: 0; box-shadow: 0 20px 40px rgba(0,0,0,.5); }
}
@container studio (min-width: 1400px) { .ps-menu { display: none !important; } }

/* < 1000px: variáveis descem para baixo do editor */
@container studio (max-width: 999px) {
  .ps { grid-template-columns: minmax(230px, 280px) minmax(0, 1fr); grid-template-rows: minmax(0, 1fr) auto; }
  .ps-list { grid-row: 1 / span 2; }
  .ps-editor { grid-column: 2; grid-row: 1; }
  .ps-vars { grid-column: 2; grid-row: 2; border-left: 0; border-top: 1px solid var(--ps-border); max-height: 42vh; }
}

/* < 700px (celular): um painel por vez */
@container studio (max-width: 699px) {
  .ps { grid-template-columns: 1fr; grid-template-rows: auto; border-radius: 10px; }
  .ps-list { grid-row: auto; border-right: 0; }
  .ps-editor, .ps-vars { display: none; grid-column: 1; grid-row: auto; max-height: none; }
  .ps.show-detail { display: flex; flex-direction: column; overflow: auto; }
  .ps.show-detail .ps-list { display: none; }
  .ps.show-detail .ps-editor, .ps.show-detail .ps-vars { display: flex; }
  .ps.show-detail .ps-pane { overflow: visible; }
  .ps-back { display: inline-flex; }
  .ps-ed-wrap { min-height: 240px; flex: none; height: 300px; }
}
@media (max-width: 767px) { .ps { height: calc(100dvh - 56px - 48px); } }

/* Dentro do editor estreito: histórico empilha (lista de versões em cima do diff) */
@container editor (max-width: 640px) {
  .ps-hist { grid-template-columns: 1fr; grid-template-rows: auto minmax(0, 1fr); }
  .ps-vlist { max-height: 160px; }
  .ps-ed-top { flex-wrap: wrap; }
}
@media (prefers-reduced-motion: reduce) { .ps * { transition-duration: 1ms !important; animation: none !important; } }
`;
