'use client';

import {
  motion, AnimatePresence, LayoutGroup,
  useMotionValue, useSpring, useTransform,
  type Variants,
} from 'framer-motion';
import dynamic from 'next/dynamic';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Activity, AlertTriangle, ArrowDownRight, ArrowRight, ArrowUpRight,
  Baby, CheckCircle, CreditCard, DollarSign, Home, Landmark, LayoutGrid,
  Mail, Plane, PiggyBank, RefreshCw, Repeat, ShieldAlert, Sparkles,
  Target, TrendingUp, Users, Wallet, Zap,
} from 'lucide-react';

import { HeroParticles } from '@/components/dashboard/HeroParticles';
import { SpringMetric } from '@/components/dashboard/SpringMetric';
import { financeService, PersonalFinanceMonthResponse, PersonalFinanceMonthSummary } from '@/services/finance';
import { getLeads, CustomerStatus as LeadStatus } from '@/services/leads';
import { agendaService } from '@/services/agenda';
import { plannerService } from '@/services/plannerService';
import { FinancialGoal, GoalCategory, MonthlySimulation } from '@/types/planner';
import { apiFetch } from '@/services/api';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

/* ═══════════════════════════════════════════════════════════════════
   DESIGN TOKENS — single source of truth (dark teal-green theme)
   Mirrors CSS vars below so JS (charts/inline) and CSS never diverge.
   ═══════════════════════════════════════════════════════════════════ */
const C = {
  primary: '#00D4AA',   // brand teal — revenue, CTAs, identity
  success: '#22C55E',   // profit / positive delta
  loss:    '#EF4444',   // loss / negative delta
  warn:    '#F59E0B',   // expenses / caution
  info:    '#818CF8',   // leads / neutral-positive
  accent:  '#F472B6',   // email / highlight
  // funnel scale (cold → hot → won)
  funnel:  ['#6366F1', '#8B5CF6', '#A78BFA', '#EC4899', '#00D4AA'],
  expense: ['#F59E0B', '#EF4444', '#8B5CF6', '#6366F1', '#EC4899'],
  text:  '#f4f4f5',
  text2: '#a1a1aa',
  text3: '#71717a',
  muted: '#52525b',
  grid:  'rgba(255,255,255,0.045)',
} as const;

/* ── utils ──────────────────────────────────────────────────────── */
function prevMonthOf(y: number, m: number, offset: number): [number, number] {
  let mo = m - offset, yr = y;
  while (mo <= 0) { mo += 12; yr--; }
  return [yr, mo];
}
const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });
const S = (v: number) => Math.abs(v) >= 1_000_000 ? `R$ ${(v / 1_000_000).toFixed(1)}M` : Math.abs(v) >= 1_000 ? `R$ ${(v / 1_000).toFixed(1)}k` : R(v);
const MONTHS = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

/* ── animation system (DESIGN.md easing curves) ─────────────────── */
const EASE_OUT = [0.16, 1, 0.3, 1] as [number, number, number, number];

const pageVariants: Variants = {
  hidden: { opacity: 0 },
  show: { opacity: 1, transition: { staggerChildren: 0.06, delayChildren: 0.04 } },
};
const sectionVariants: Variants = {
  hidden: { opacity: 0, y: 24 },
  show: { opacity: 1, y: 0, transition: { duration: 0.5, ease: EASE_OUT } },
};
const popVariants: Variants = {
  hidden: { opacity: 0, y: 26, scale: 0.9 },
  show: { opacity: 1, y: 0, scale: 1, transition: { type: 'spring', stiffness: 320, damping: 22 } },
};
const stagger7: Variants = { hidden: {}, show: { transition: { staggerChildren: 0.07, delayChildren: 0.06 } } };
const stagger5: Variants = { hidden: {}, show: { transition: { staggerChildren: 0.06, delayChildren: 0.05 } } };
const tabPanel: Variants = {
  hidden: { opacity: 0, y: 14 },
  show: { opacity: 1, y: 0, transition: { duration: 0.4, ease: EASE_OUT, staggerChildren: 0.06, delayChildren: 0.02 } },
  exit: { opacity: 0, y: -10, transition: { duration: 0.22, ease: EASE_OUT } },
};

const cardHover = {
  y: -5, scale: 1.015,
  boxShadow: '0 22px 60px rgba(0,0,0,0.45)',
  transition: { type: 'spring', stiffness: 360, damping: 26 },
} as const;
const cardTap = { scale: 0.985, transition: { type: 'spring', stiffness: 500, damping: 30 } } as const;

/* ── types ──────────────────────────────────────────────────────── */
interface EmailStats { totalCampaigns: number; totalEmailsSent: number; openRate: number; clickRate: number; }
interface Funnel { lead: number; contacted: number; qualified: number; negotiating: number; customer: number; }
interface MonthPoint { label: string; income: number; expense: number; }
interface CatExpense { name: string; total: number; pct: number; }
interface DashData {
  funnel: Funnel; curr: PersonalFinanceMonthResponse | null;
  prev: PersonalFinanceMonthResponse | null; trend: MonthPoint[];
  email: EmailStats | null; expenses: CatExpense[];
  agenda: { time: string; title: string }[];
  goals: FinancialGoal[]; simulation: MonthlySimulation | null;
}
type Tab = 'overview' | 'pipeline' | 'finance';

/* ── goal category visual meta ──────────────────────────────────── */
const GOAL_META: Record<GoalCategory, { icon: React.ReactNode; color: string; label: string }> = {
  [GoalCategory.Emergency]:  { icon: <ShieldAlert size={17} />, color: C.loss,    label: 'Emergência' },
  [GoalCategory.Baby]:       { icon: <Baby size={17} />,        color: C.accent,  label: 'Bebê' },
  [GoalCategory.House]:      { icon: <Home size={17} />,        color: C.info,    label: 'Casa' },
  [GoalCategory.Travel]:     { icon: <Plane size={17} />,       color: C.primary, label: 'Viagem' },
  [GoalCategory.Investment]: { icon: <TrendingUp size={17} />,  color: C.success, label: 'Investimento' },
  [GoalCategory.Debt]:       { icon: <Landmark size={17} />,    color: C.warn,    label: 'Dívida' },
  [GoalCategory.Other]:      { icon: <Target size={17} />,      color: C.text2,   label: 'Outro' },
};

/* ── chart configs (all consume real data passed in) ────────────── */
const animCfg = { enabled: true, easing: 'easeinout', speed: 1100, animateGradually: { enabled: true, delay: 120 }, dynamicAnimation: { enabled: true, speed: 500 } };

function sparkConfig(color: string, data: number[]) {
  return {
    type: 'area' as const, height: 56,
    series: [{ data: data.length ? data : [0] }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, animations: animCfg },
      stroke: { curve: 'smooth' as const, width: 2 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.34, opacityTo: 0 } },
      colors: [color], tooltip: { enabled: false },
    },
  };
}
function heroChartConfig(trend: MonthPoint[]) {
  return {
    type: 'area' as const, height: 132,
    series: [{ name: 'Receita', data: trend.map(t => Math.round(t.income)) }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, background: 'transparent', animations: { ...animCfg, speed: 1400 } },
      stroke: { curve: 'smooth' as const, width: 3 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.34, opacityTo: 0 } },
      colors: [C.primary],
      tooltip: { enabled: true, theme: 'dark' as const, y: { formatter: (v: number) => R(v) } },
      xaxis: { categories: trend.map(t => t.label) },
    },
  };
}
function trendConfig(trend: MonthPoint[]) {
  return {
    type: 'line' as const, height: 250,
    series: [
      { name: 'Receita', type: 'area', data: trend.map(t => Math.round(t.income)) },
      { name: 'Despesas', type: 'line', data: trend.map(t => Math.round(t.expense)) },
    ],
    options: {
      chart: { toolbar: { show: false }, background: 'transparent', animations: animCfg },
      colors: [C.primary, C.warn],
      stroke: { curve: 'smooth' as const, width: [3, 2], dashArray: [0, 4] },
      fill: { type: ['gradient', 'none'], gradient: { opacityFrom: 0.22, opacityTo: 0 } },
      xaxis: { categories: trend.map(t => t.label), labels: { style: { colors: C.muted, fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { labels: { style: { colors: C.muted, fontSize: '11px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` } },
      grid: { borderColor: C.grid, strokeDashArray: 4 },
      legend: { labels: { colors: C.text3 }, position: 'top' as const, horizontalAlign: 'right' as const },
      tooltip: { theme: 'dark' as const, y: { formatter: (v: number) => R(v) } },
      markers: { size: 0, hover: { size: 6 }, colors: [C.primary, C.warn], strokeColors: 'transparent' },
    },
  };
}
function funnelConfig(f: Funnel) {
  const total = f.lead + f.contacted + f.qualified + f.negotiating + f.customer;
  const stages = [
    { name: 'Leads', v: total },
    { name: 'Contato', v: f.contacted + f.qualified + f.negotiating + f.customer },
    { name: 'Qualif.', v: f.qualified + f.negotiating + f.customer },
    { name: 'Neg.', v: f.negotiating + f.customer },
    { name: 'Clientes', v: f.customer },
  ];
  return {
    type: 'bar' as const, height: 230,
    series: [{ name: 'Total', data: stages.map(s => s.v) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: animCfg },
      plotOptions: { bar: { horizontal: true, barHeight: '62%', borderRadius: 8, distributed: true, borderRadiusApplication: 'end' as const } },
      colors: [...C.funnel],
      xaxis: { labels: { style: { colors: C.muted, fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { categories: stages.map(s => s.name), labels: { style: { colors: C.text2, fontSize: '12px' } } },
      grid: { borderColor: C.grid, xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      dataLabels: { enabled: true, style: { colors: ['#fff'], fontSize: '11px', fontWeight: 700 }, formatter: (v: number) => v > 0 ? String(v) : '' },
      legend: { show: false },
      tooltip: { theme: 'dark' as const },
    },
  };
}
function expenseConfig(cats: CatExpense[]) {
  return {
    type: 'bar' as const, height: 210,
    series: [{ name: 'Valor', data: cats.map(c => Math.round(c.total)) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: animCfg },
      plotOptions: { bar: { horizontal: true, barHeight: '56%', borderRadius: 7, distributed: true, borderRadiusApplication: 'end' as const } },
      colors: [...C.expense],
      xaxis: { labels: { style: { colors: C.muted, fontSize: '10px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { categories: cats.map(c => c.name.length > 14 ? c.name.slice(0, 14) + '…' : c.name), labels: { style: { colors: C.text2, fontSize: '11px' } } },
      grid: { borderColor: C.grid, xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      dataLabels: { enabled: false },
      legend: { show: false },
      tooltip: { theme: 'dark' as const, y: { formatter: (v: number) => R(v) } },
    },
  };
}
function expenseDonutConfig(cats: CatExpense[], total: number) {
  return {
    type: 'donut' as const, height: 248,
    series: cats.map(c => Math.round(c.total)),
    options: {
      chart: { background: 'transparent', animations: animCfg },
      labels: cats.map(c => c.name),
      colors: [...C.expense],
      stroke: { width: 0 },
      legend: { position: 'bottom' as const, labels: { colors: C.text2 }, fontSize: '11px', markers: { width: 8, height: 8 } as never },
      dataLabels: { enabled: false },
      plotOptions: { pie: { donut: { size: '70%', labels: { show: true, total: { show: true, label: 'Total', color: C.text3, fontSize: '12px', formatter: () => S(total) }, value: { color: C.text, fontSize: '20px', fontFamily: 'var(--font-mono)', fontWeight: 700, formatter: (v: string) => S(Number(v)) } } } } },
      tooltip: { theme: 'dark' as const, y: { formatter: (v: number) => R(v) } },
    },
  };
}

function forecastConfig(sim: MonthlySimulation) {
  const cats = sim.dailyBalances.map(d => new Date(d.date).getDate());
  const data = sim.dailyBalances.map(d => Math.round(d.closingBalance));
  const risk = sim.hasNegativeBalanceRisk;
  const lineColor = risk ? C.warn : C.primary;
  const low = Math.min(sim.lowestProjectedBalance, ...data, 0);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const annotations: any = {
    yaxis: [
      { y: 0, borderColor: C.loss, strokeDashArray: 4, label: { text: 'Zero', position: 'left' as const, style: { color: '#fff', background: C.loss, fontSize: '10px' } } },
    ],
  };
  if (low < 0) annotations.yaxis.push({ y: 0, y2: low, fillColor: C.loss, opacity: 0.08, borderColor: 'transparent' });
  return {
    type: 'area' as const, height: 280,
    series: [{ name: 'Saldo projetado', data }],
    options: {
      chart: { toolbar: { show: false }, background: 'transparent', animations: { ...animCfg, speed: 1300 } },
      colors: [lineColor],
      stroke: { curve: 'smooth' as const, width: 3 },
      fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.3, opacityTo: 0, stops: [0, 100] } },
      dataLabels: { enabled: false },
      xaxis: { categories: cats, tickAmount: 10, labels: { style: { colors: C.muted, fontSize: '10px' } }, axisBorder: { show: false }, axisTicks: { show: false }, title: { text: 'Dia do mês', style: { color: C.muted, fontSize: '10px', fontWeight: 500 } } },
      yaxis: { labels: { style: { colors: C.muted, fontSize: '11px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` } },
      grid: { borderColor: C.grid, strokeDashArray: 4 },
      annotations,
      tooltip: { theme: 'dark' as const, x: { formatter: (v: number) => `Dia ${v}` }, y: { formatter: (v: number) => R(v) } },
      markers: { size: 0, hover: { size: 6 } },
    },
  };
}

/* ── AnimatedBar ────────────────────────────────────────────────── */
function AnimatedBar({ pct, color, h = 4 }: { pct: number; color: string; h?: number }) {
  const width = useMotionValue('0%');
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current; if (!el) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      setTimeout(() => width.set(`${Math.min(Math.max(pct, 0), 100)}%`), 80);
    }, { threshold: 0.2 });
    io.observe(el); return () => io.disconnect();
  }, [pct, width]);
  return (
    <div ref={ref} style={{ height: h, background: 'rgba(255,255,255,0.08)', borderRadius: 999, overflow: 'hidden' }}>
      <motion.div style={{ height: '100%', width, background: color, borderRadius: 999, boxShadow: `0 0 12px ${color}66` }}
        transition={{ duration: 1.1, ease: EASE_OUT }} />
    </div>
  );
}

/* ── AnimatedRing — radial gauge for goal progress ──────────────── */
function AnimatedRing({ pct, color, size = 116, stroke = 9, label, value, center }: { pct: number; color: string; size?: number; stroke?: number; label?: string; value?: string; center?: React.ReactNode }) {
  const r = (size - stroke - 3) / 2;
  const circ = 2 * Math.PI * r;
  const dash = useMotionValue(circ);
  const ref = useRef<SVGSVGElement>(null);
  useEffect(() => {
    const el = ref.current; if (!el) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      setTimeout(() => dash.set(circ * (1 - Math.min(Math.max(pct, 0), 100) / 100)), 120);
    }, { threshold: 0.3 });
    io.observe(el); return () => io.disconnect();
  }, [pct, circ, dash]);
  return (
    <div style={{ position: 'relative', width: size, height: size, flexShrink: 0 }}>
      <svg ref={ref} width={size} height={size} style={{ transform: 'rotate(-90deg)' }}>
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" stroke="rgba(255,255,255,0.07)" strokeWidth={stroke} />
        <motion.circle
          cx={size / 2} cy={size / 2} r={r} fill="none" stroke={color} strokeWidth={stroke}
          strokeLinecap="round" strokeDasharray={circ} style={{ strokeDashoffset: dash, filter: `drop-shadow(0 0 6px ${color}88)` }}
          transition={{ duration: 1.3, ease: EASE_OUT }}
        />
      </svg>
      <div style={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
        {center ?? <>
          <span className="num" style={{ fontSize: 22, fontWeight: 800, color, lineHeight: 1 }}>{value}</span>
          <span style={{ fontSize: 9, color: C.text3, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{label}</span>
        </>}
      </div>
    </div>
  );
}

/* ── Skeleton ───────────────────────────────────────────────────── */
function Skel({ h = 20, r = 8, w = '100%' }: { h?: number; r?: number; w?: string | number }) {
  return (
    <motion.div
      style={{ height: h, borderRadius: r, width: w, background: 'linear-gradient(90deg, rgba(255,255,255,0.05), rgba(255,255,255,0.1), rgba(255,255,255,0.05))', backgroundSize: '200% 100%' }}
      animate={{ backgroundPosition: ['200% 0', '-200% 0'] }}
      transition={{ duration: 1.6, repeat: Infinity, ease: 'linear' }}
    />
  );
}

/* ── CSS — tokens + components ──────────────────────────────────── */
const CSS = `
  @property --glow-angle { syntax: '<angle>'; inherits: false; initial-value: 0deg; }
  @keyframes glow-rotate { to { --glow-angle: 360deg; } }
  @keyframes live-pulse { 0%,100% { transform: scale(1); opacity: 1; } 50% { transform: scale(1.7); opacity: 0.5; } }
  @keyframes shimmer { to { background-position: -200% 0; } }
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after { animation-duration: 0.01ms !important; transition-duration: 0.01ms !important; }
    .hero-card { animation: none !important; }
  }

  .dsh {
    /* ── color tokens ── */
    --c-primary: ${C.primary}; --c-success: ${C.success}; --c-loss: ${C.loss};
    --c-warn: ${C.warn}; --c-info: ${C.info}; --c-accent: ${C.accent};
    --bg-0: #0B1410; --surface-1: rgba(255,255,255,0.04); --surface-2: rgba(255,255,255,0.025);
    --border-1: rgba(255,255,255,0.09); --border-2: rgba(255,255,255,0.06);
    --fg-1: ${C.text}; --fg-2: ${C.text2}; --fg-3: ${C.text3}; --fg-muted: ${C.muted};
    --font-ui: var(--font-jakarta), var(--font-inter), -apple-system, sans-serif;
    --font-num: var(--font-mono), 'SF Mono', ui-monospace, monospace;
    font-family: var(--font-ui);
  }

  .dsh :focus-visible { outline: 2px solid var(--c-primary); outline-offset: 2px; border-radius: 8px; }
  .num { font-family: var(--font-num); font-variant-numeric: tabular-nums; font-feature-settings: 'tnum' 1; }

  /* Hero card with animated conic glow border */
  .hero-card {
    position: relative;
    background: radial-gradient(ellipse at 82% 40%, rgba(0,212,170,0.08) 0%, transparent 62%),
                radial-gradient(ellipse at 12% 90%, rgba(129,140,248,0.06) 0%, transparent 58%),
                rgba(255,255,255,0.035);
    border-radius: 24px; padding: 30px 34px; overflow: hidden;
    --glow-angle: 0deg; animation: glow-rotate 7s linear infinite;
  }
  .hero-card::before {
    content: ''; position: absolute; inset: -1.5px; border-radius: 25.5px;
    background: conic-gradient(from var(--glow-angle), transparent 62%, ${C.primary} 74%, ${C.success} 82%, ${C.info} 90%, transparent 100%);
    z-index: 0; opacity: 0.75;
  }
  .hero-card::after { content: ''; position: absolute; inset: 1px; border-radius: 23px; background: #0A130F; z-index: 0; }
  .hero-card > * { position: relative; z-index: 1; }

  .card { background: var(--surface-1); border: 1px solid var(--border-1); border-radius: 18px; padding: 22px; }
  .card-h { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 16px; }
  .card-title { font-size: 14px; font-weight: 700; color: var(--fg-1); }

  .metric-huge { font-size: 52px; font-weight: 800; color: var(--fg-1); letter-spacing: -0.04em; line-height: 1; }
  .metric-lg   { font-size: 30px; font-weight: 700; color: var(--fg-1); letter-spacing: -0.03em; }
  .metric-md   { font-size: 22px; font-weight: 700; color: var(--fg-1); letter-spacing: -0.02em; }
  .label { font-size: 11px; font-weight: 600; color: var(--fg-3); text-transform: uppercase; letter-spacing: 0.08em; }
  .sub   { font-size: 13px; color: var(--fg-3); }
  .teal  { color: var(--c-primary); }

  .badge { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
  .badge-up   { background: rgba(34,197,94,0.15);  color: ${C.success}; }
  .badge-dn   { background: rgba(239,68,68,0.15);   color: ${C.loss}; }
  .badge-teal { background: rgba(0,212,170,0.15);   color: ${C.primary}; }
  .badge-warn { background: rgba(245,158,11,0.15);  color: ${C.warn}; }
  .badge-neu  { background: rgba(113,113,122,0.18); color: var(--fg-2); }

  .live-dot { width: 7px; height: 7px; border-radius: 50%; background: var(--c-primary); display: inline-block; box-shadow: 0 0 0 0 rgba(0,212,170,0.6); animation: live-pulse 2s infinite; }

  .cta-link { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: var(--c-primary); text-decoration: none; }
  .cta-link:hover { text-decoration: underline; }

  .stage-card { background: var(--surface-2); border: 1px solid var(--border-1); border-radius: 14px; padding: 16px; }
  .btn-ghost  { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border-radius: 10px; border: 1px solid var(--border-2); font-size: 12px; font-weight: 600; cursor: pointer; background: rgba(255,255,255,0.05); color: var(--fg-2); }

  /* Segmented tab control */
  .seg { position: relative; display: inline-flex; gap: 2px; padding: 4px; border-radius: 14px; background: rgba(255,255,255,0.05); border: 1px solid var(--border-2); }
  .seg-btn { position: relative; display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border-radius: 10px; border: none; background: transparent; cursor: pointer; font-size: 13px; font-weight: 600; color: var(--fg-3); z-index: 1; transition: color 0.2s; }
  .seg-btn[aria-selected="true"] { color: #07130F; }
  .seg-pill { position: absolute; inset: 4px auto 4px 4px; border-radius: 10px; background: var(--c-primary); box-shadow: 0 6px 18px rgba(0,212,170,0.4); z-index: 0; }

  .g4 { display: grid; grid-template-columns: repeat(4,1fr); gap: 14px; }
  .g3 { display: grid; grid-template-columns: repeat(3,1fr); gap: 14px; }
  .g2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
  .g73 { display: grid; grid-template-columns: 1fr 320px; gap: 14px; }
  .g63 { display: grid; grid-template-columns: 1fr 300px; gap: 14px; }
  .g5cmd { display: grid; grid-template-columns: repeat(5,1fr); gap: 10px; }
  .divider { border: none; border-top: 1px solid var(--border-2); margin: 0; }

  @media(max-width:1200px) {
    .g4 { grid-template-columns: repeat(2,1fr) !important; }
    .g73, .g63 { grid-template-columns: 1fr !important; }
    .g5cmd { grid-template-columns: repeat(3,1fr) !important; }
  }
  @media(max-width:700px) {
    .g4,.g3,.g2,.g5cmd { grid-template-columns: 1fr !important; }
    .metric-huge { font-size: 38px !important; }
  }
`;

/* ── KPI Card (flexible: real spark series OR custom mini) ──────── */
function KpiCard({ label, value, prefix = '', suffix = '', delta, up, spark, mini, color, icon, loading, idx }: {
  label: string; value: number; prefix?: string; suffix?: string;
  delta?: string; up?: boolean; spark?: number[]; mini?: React.ReactNode;
  color: string; icon?: React.ReactNode; loading: boolean; idx: number;
}) {
  return (
    <motion.div className="card" variants={popVariants} whileHover={cardHover} whileTap={cardTap} style={{ cursor: 'default' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
          {icon && <span style={{ color, display: 'inline-flex' }}>{icon}</span>}
          <span className="label">{label}</span>
        </div>
        {delta && (
          <motion.span className={`badge ${up ? 'badge-up' : 'badge-dn'}`}
            initial={{ scale: 0, opacity: 0 }} animate={{ scale: 1, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 400, damping: 20, delay: 0.12 + idx * 0.07 }}>
            {up ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}{delta}
          </motion.span>
        )}
      </div>
      {loading
        ? <Skel h={34} r={6} />
        : <SpringMetric value={value} prefix={prefix} suffix={suffix} className="metric-lg num" stiffness={65} damping={16} />}
      <div style={{ marginTop: 12, minHeight: 56 }}>
        {loading ? <Skel h={56} r={6} />
          : mini ? mini
          : spark ? <ApexChart {...sparkConfig(color, spark)} />
          : null}
      </div>
    </motion.div>
  );
}

/* ── Command Center — "5 perguntas" (real data) ─────────────────── */
function CommandCenter({ data, loading }: { data: DashData | null; loading: boolean }) {
  if (loading || !data) {
    return <motion.div className="g5cmd" variants={sectionVariants}>{[0, 1, 2, 3, 4].map(i => <Skel key={i} h={92} r={14} />)}</motion.div>;
  }
  const { funnel, curr, prev: prevD, email } = data;
  const cs = curr?.summary, ps = prevD?.summary;
  const revMoM = cs && ps && ps.totalIncome > 0 ? ((cs.totalIncome - ps.totalIncome) / ps.totalIncome) * 100 : 0;
  const totalPipeline = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;
  const worstDrop = [
    { lbl: 'Lead→Contato', v: funnel.lead > 0 ? (1 - funnel.contacted / funnel.lead) * 100 : 0 },
    { lbl: 'Contato→Qualif', v: funnel.contacted > 0 ? (1 - funnel.qualified / funnel.contacted) * 100 : 0 },
    { lbl: 'Qualif.→Neg.', v: funnel.qualified > 0 ? (1 - funnel.negotiating / funnel.qualified) * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b);
  const cashFlow = cs?.remainingBalance ?? 0;
  const topExpense = data.expenses[0];
  const convRate = (totalPipeline + funnel.customer) > 0 ? (funnel.customer / (totalPipeline + funnel.customer)) * 100 : 0;

  const answers = [
    { q: 'O que funciona?', icon: <CheckCircle size={15} />, c: revMoM >= 0 ? C.primary : C.warn, text: revMoM > 0 ? `Receita +${revMoM.toFixed(1)}% vs mês ant.` : 'Receita estável', href: '/finance' },
    { q: 'O que está quebrado?', icon: <AlertTriangle size={15} />, c: worstDrop.v > 50 ? C.loss : C.warn, text: totalPipeline > 0 ? `${worstDrop.v.toFixed(0)}% drop em ${worstDrop.lbl}` : 'Pipeline vazio', href: '/leads' },
    { q: 'Onde perde dinheiro?', icon: <DollarSign size={15} />, c: cashFlow < 0 ? C.loss : C.warn, text: cashFlow < 0 ? `Fluxo negativo: ${S(cashFlow)}` : topExpense ? `${topExpense.name}: ${topExpense.pct.toFixed(0)}% das despesas` : 'Verifique despesas', href: '/finance' },
    { q: 'O que fazer agora?', icon: <Zap size={15} />, c: C.info, text: totalPipeline > 0 ? `${totalPipeline} leads aguardam ação` : 'Adicione leads', href: '/leads' },
    { q: 'Qual ação gera receita?', icon: <Target size={15} />, c: C.primary, text: email && email.openRate > 0 ? `Email: ${email.openRate.toFixed(1)}% abertura` : convRate > 0 ? `Conversão: ${convRate.toFixed(1)}%` : 'Analise canais', href: '/leads' },
  ];

  return (
    <motion.div className="g5cmd" variants={stagger5}>
      {answers.map((a, i) => (
        <Link key={i} href={a.href} style={{ textDecoration: 'none' }}>
          <motion.div variants={popVariants}
            whileHover={{ y: -4, scale: 1.025, boxShadow: `0 14px 38px ${a.c}33`, transition: { type: 'spring', stiffness: 380, damping: 24 } }}
            whileTap={{ scale: 0.965 }}
            style={{ background: `${a.c}12`, border: `1px solid ${a.c}30`, borderRadius: 14, padding: '14px 16px', cursor: 'pointer', height: '100%' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 7 }}>
              <motion.span style={{ color: a.c, display: 'inline-flex' }} whileHover={{ rotate: 12, scale: 1.15 }} transition={{ type: 'spring', stiffness: 400, damping: 14 }}>{a.icon}</motion.span>
              <span style={{ fontSize: 10, fontWeight: 700, color: a.c, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{a.q}</span>
            </div>
            <div style={{ fontSize: 12, color: '#d4d4d8', lineHeight: 1.45, fontWeight: 500 }}>{a.text}</div>
            <div style={{ marginTop: 7, display: 'flex', alignItems: 'center', gap: 3, fontSize: 10, color: a.c, fontWeight: 600 }}>Ver detalhes <ArrowRight size={9} /></div>
          </motion.div>
        </Link>
      ))}
    </motion.div>
  );
}

/* ── Hero Card (real projection-based goal) ─────────────────────── */
function HeroCard({ data, loading, mounted }: { data: DashData | null; loading: boolean; mounted: boolean }) {
  const s = data?.curr?.summary;
  const income = s?.totalIncome ?? 0;
  const prev = data?.prev?.summary?.totalIncome ?? 0;
  const mom = prev > 0 ? ((income - prev) / prev) * 100 : 0;
  const cashFlow = s?.remainingBalance ?? 0;
  const totalExpenses = s?.totalExpenses ?? 0;
  // Real reference: projected income for the month; fallback to previous month
  const projected = s?.projectedIncome ?? 0;
  const goalRef = projected > 0 ? projected : prev;
  const goalPct = goalRef > 0 ? Math.min((income / goalRef) * 100, 100) : 0;
  const goalLabel = projected > 0 ? 'da projeção' : prev > 0 ? 'vs mês ant.' : 'meta';
  const goalColor = goalPct >= 80 ? C.primary : goalPct >= 50 ? C.warn : C.loss;
  const heroCfg = mounted && data?.trend?.length ? heroChartConfig(data.trend) : null;

  return (
    <motion.div variants={sectionVariants} className="hero-card">
      <HeroParticles />
      <div style={{ display: 'grid', gridTemplateColumns: '1.1fr 1fr', gap: 32, alignItems: 'center' }}>
        {/* Left */}
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
            <span className="label" style={{ color: C.muted }}>Receita do Mês</span>
            <span className="live-dot" /><span style={{ fontSize: 10, color: C.muted, fontWeight: 600 }}>AO VIVO</span>
          </div>
          <AnimatePresence mode="wait">
            {loading
              ? <Skel key="sk" h={58} r={8} w={260} />
              : <motion.div key="metric" initial={{ opacity: 0, scale: 0.85, y: 12 }} animate={{ opacity: 1, scale: 1, y: 0 }} transition={{ type: 'spring', stiffness: 280, damping: 20, delay: 0.1 }}>
                  <SpringMetric value={income} prefix="R$ " className="metric-huge teal num" stiffness={55} damping={13} />
                </motion.div>}
          </AnimatePresence>
          <motion.div style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 14, flexWrap: 'wrap' }}
            initial={{ opacity: 0, y: 8 }} animate={{ opacity: loading ? 0 : 1, y: 0 }} transition={{ delay: 0.35, duration: 0.4 }}>
            <motion.span className={`badge ${mom >= 0 ? 'badge-up' : 'badge-dn'}`} initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ type: 'spring', stiffness: 400, damping: 18, delay: 0.4 }}>
              {mom >= 0 ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}{Math.abs(mom).toFixed(1)}% vs mês ant.
            </motion.span>
            {cashFlow >= 0
              ? <span className="badge badge-teal"><DollarSign size={10} /> Fluxo: {S(cashFlow)}</span>
              : <span className="badge badge-dn"><ArrowDownRight size={10} /> Fluxo: {S(cashFlow)}</span>}
          </motion.div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 22, marginTop: 24 }}>
            {!loading && goalRef > 0 && (
              <AnimatedRing pct={goalPct} color={goalColor} value={`${goalPct.toFixed(0)}%`} label={goalLabel} />
            )}
            <div style={{ flex: 1 }}>
              {loading ? [0, 1].map(i => <div key={i} style={{ marginBottom: 12 }}><Skel h={40} r={8} /></div>) : [
                { lbl: 'Despesas do mês', val: totalExpenses, c: C.warn, icon: <Wallet size={13} /> },
                { lbl: 'Saldo líquido', val: cashFlow, c: cashFlow >= 0 ? C.primary : C.loss, icon: <PiggyBank size={13} /> },
              ].map((m, i) => (
                <motion.div key={m.lbl} initial={{ opacity: 0, x: 12 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.42 + i * 0.08, duration: 0.4, ease: EASE_OUT }} style={{ marginBottom: i === 0 ? 14 : 0 }}>
                  <div className="label" style={{ marginBottom: 4, display: 'flex', alignItems: 'center', gap: 5 }}><span style={{ color: m.c, display: 'inline-flex' }}>{m.icon}</span>{m.lbl}</div>
                  <SpringMetric value={Math.abs(m.val)} prefix="R$ " suffix={m.val < 0 ? ' ↓' : ''} className="metric-md num" style={{ color: m.c } as React.CSSProperties} stiffness={70} damping={16} />
                </motion.div>
              ))}
            </div>
          </div>
        </div>
        {/* Right */}
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
            <span className="label" style={{ display: 'flex', alignItems: 'center', gap: 6 }}><TrendingUp size={13} style={{ color: C.primary }} />Tendência — 6 meses</span>
            <Link href="/finance" className="cta-link">Detalhes <ArrowRight size={11} /></Link>
          </div>
          {loading || !heroCfg ? <Skel h={132} r={8} /> : <ApexChart {...heroCfg} />}
          <hr className="divider" style={{ margin: '16px 0' }} />
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            {loading ? [0, 1].map(i => <Skel key={i} h={42} r={8} />) : [
              { lbl: 'Recebido', val: income, c: C.primary },
              { lbl: 'Projetado', val: projected, c: C.info },
            ].map((m, i) => (
              <motion.div key={m.lbl} initial={{ opacity: 0, x: 10 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.45 + i * 0.06, duration: 0.35, ease: EASE_OUT }}>
                <div className="label" style={{ marginBottom: 5 }}>{m.lbl}</div>
                <SpringMetric value={Math.abs(m.val)} prefix="R$ " className="metric-md num" style={{ color: m.c } as React.CSSProperties} stiffness={70} damping={16} />
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </motion.div>
  );
}

/* ── Section wrapper (entrance on scroll) ───────────────────────── */
function Section({ children, style }: { children: React.ReactNode; style?: React.CSSProperties }) {
  return <motion.div variants={sectionVariants} style={style}>{children}</motion.div>;
}

/* ── Trend card ─────────────────────────────────────────────────── */
function TrendCard({ data, mounted, loading, revMoM }: { data: DashData | null; mounted: boolean; loading: boolean; revMoM: number }) {
  const cfg = mounted && data?.trend?.length ? trendConfig(data.trend) : null;
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h">
        <div>
          <div className="card-title">Receita vs Despesas</div>
          <div className="sub" style={{ marginTop: 2 }}>Últimos 6 meses</div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <span className={`badge ${revMoM >= 0 ? 'badge-up' : 'badge-dn'}`}>{revMoM >= 0 ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}{Math.abs(revMoM).toFixed(1)}%</span>
          <Link href="/finance" className="cta-link" style={{ fontSize: 11 }}>Ver tudo <ArrowRight size={10} /></Link>
        </div>
      </div>
      {!mounted || loading || !cfg ? <Skel h={250} r={10} /> : <ApexChart {...cfg} />}
    </motion.div>
  );
}

/* ── Email card ─────────────────────────────────────────────────── */
function EmailCard({ data, loading }: { data: DashData | null; loading: boolean }) {
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap} style={{ display: 'flex', flexDirection: 'column' }}>
      <div className="card-h" style={{ alignItems: 'center' }}>
        <div>
          <div className="card-title">Email Marketing</div>
          <div className="sub" style={{ marginTop: 2 }}>Últimos 30 dias</div>
        </div>
        <motion.span whileHover={{ rotate: 12, scale: 1.15 }} transition={{ type: 'spring', stiffness: 400, damping: 14 }}><Mail size={16} style={{ color: C.accent }} /></motion.span>
      </div>
      {loading
        ? <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>{[0, 1, 2, 3].map(i => <Skel key={i} h={22} r={6} />)}</div>
        : data?.email
          ? <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              {[
                { lbl: 'Enviados', val: data.email.totalEmailsSent.toLocaleString('pt-BR'), pct: 100, c: C.info },
                { lbl: 'Abertos', val: `${data.email.openRate.toFixed(1)}%`, pct: data.email.openRate, c: C.success },
                { lbl: 'Cliques', val: `${data.email.clickRate.toFixed(1)}%`, pct: data.email.clickRate * 3, c: C.warn },
              ].map((s, i) => (
                <motion.div key={s.lbl} initial={{ opacity: 0, x: 16 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: i * 0.1 + 0.15, duration: 0.4, ease: EASE_OUT }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                    <span className="sub" style={{ fontSize: 12 }}>{s.lbl}</span>
                    <span className="num" style={{ fontSize: 13, fontWeight: 700, color: C.text }}>{s.val}</span>
                  </div>
                  <AnimatedBar pct={s.pct > 100 ? 100 : s.pct} color={s.c} h={3} />
                </motion.div>
              ))}
              <hr className="divider" />
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span className="sub" style={{ fontSize: 11 }}>{data.email.totalCampaigns} campanhas</span>
                <Link href="/analytics" className="cta-link">Ver analytics <ArrowRight size={10} /></Link>
              </div>
            </div>
          : <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: C.muted, padding: '24px 0' }}>
              <Mail size={28} /><div className="sub">Sem dados de email</div><Link href="/email-marketing" className="cta-link">Criar campanha →</Link>
            </div>}
    </motion.div>
  );
}

/* ── Funnel chart card ──────────────────────────────────────────── */
function FunnelCard({ data, mounted, loading, totalLeads }: { data: DashData | null; mounted: boolean; loading: boolean; totalLeads: number }) {
  const cfg = mounted && data?.funnel ? funnelConfig(data.funnel) : null;
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h">
        <div>
          <div className="card-title">Funil de Vendas</div>
          <div className="sub" style={{ marginTop: 2 }}>{totalLeads + (data?.funnel.customer ?? 0)} contatos · {data?.funnel.customer ?? 0} clientes</div>
        </div>
        <Link href="/leads" className="cta-link" style={{ fontSize: 11 }}>Ver leads <ArrowRight size={10} /></Link>
      </div>
      {!mounted || loading || !cfg ? <Skel h={230} r={10} /> : <ApexChart {...cfg} />}
      {data && !loading && (
        <motion.div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 8, marginTop: 14, paddingTop: 14, borderTop: '1px solid var(--border-2)' }} variants={stagger5} initial="hidden" whileInView="show" viewport={{ once: true }}>
          {[
            { lbl: 'Lead', n: data.funnel.lead, c: C.funnel[0] },
            { lbl: 'Contato', n: data.funnel.contacted, c: C.funnel[1] },
            { lbl: 'Qualif.', n: data.funnel.qualified, c: C.funnel[2] },
            { lbl: 'Neg.', n: data.funnel.negotiating, c: C.funnel[3] },
            { lbl: 'Cliente', n: data.funnel.customer, c: C.funnel[4] },
          ].map(s => (
            <motion.div key={s.lbl} variants={popVariants} style={{ textAlign: 'center' }}>
              <SpringMetric value={s.n} className="num" style={{ fontSize: 18, fontWeight: 700, color: s.c } as React.CSSProperties} stiffness={80} damping={16} />
              <div className="label" style={{ fontSize: 9 }}>{s.lbl}</div>
            </motion.div>
          ))}
        </motion.div>
      )}
    </motion.div>
  );
}

/* ── Expenses card (bar or donut) ───────────────────────────────── */
function ExpenseCard({ data, mounted, loading, expenses, cashFlow, donut = false }: { data: DashData | null; mounted: boolean; loading: boolean; expenses: number; cashFlow: number; donut?: boolean }) {
  const hasData = data?.expenses?.length;
  const cfg = mounted && hasData ? (donut ? expenseDonutConfig(data!.expenses, expenses) : expenseConfig(data!.expenses)) : null;
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h">
        <div>
          <div className="card-title">Despesas por Categoria</div>
          <div className="sub" style={{ marginTop: 2 }}>Mês atual — {S(expenses)} total</div>
        </div>
        {cashFlow < 0 && <span className="badge badge-dn"><AlertTriangle size={10} /> Fluxo negativo</span>}
      </div>
      {!mounted || loading ? <Skel h={donut ? 248 : 210} r={10} />
        : cfg && hasData ? <ApexChart {...cfg} />
          : <div style={{ height: 210, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: C.muted }}>
              <motion.div animate={{ y: [0, -6, 0] }} transition={{ duration: 2.4, repeat: Infinity, ease: 'easeInOut' }}><DollarSign size={28} /></motion.div>
              <div className="sub">Sem despesas cadastradas</div><Link href="/finance" className="cta-link">Registrar →</Link>
            </div>}
      {data && !loading && (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 14, paddingTop: 14, borderTop: '1px solid var(--border-2)' }}>
          <div>
            <div className="label" style={{ marginBottom: 4 }}>Saldo Líquido</div>
            <SpringMetric value={Math.abs(cashFlow)} prefix="R$ " suffix={cashFlow < 0 ? ' ↓' : ''} className="metric-md num" style={{ color: cashFlow >= 0 ? C.primary : C.loss } as React.CSSProperties} />
          </div>
          <Link href="/finance" className="cta-link">Ver finanças <ArrowRight size={11} /></Link>
        </div>
      )}
    </motion.div>
  );
}

/* ── Finance breakdown card (NEW — real summary fields) ─────────── */
function FinanceBreakdownCard({ summary, loading }: { summary: PersonalFinanceMonthSummary | null | undefined; loading: boolean }) {
  const s = summary;
  const rows = s ? [
    { lbl: 'Recebido vs Projetado', a: s.totalIncome, b: s.projectedIncome, c: C.primary, icon: <TrendingUp size={13} /> },
    { lbl: 'Pago vs A pagar', a: s.paidExpenses, b: s.unpaidExpenses, c: C.warn, icon: <Wallet size={13} />, note: `${s.paidCount} pagas · ${s.unpaidCount} pendentes` },
    { lbl: 'No cartão vs À vista', a: s.expensesWithCard, b: s.expensesWithoutCard, c: C.accent, icon: <CreditCard size={13} /> },
  ] : [];
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h">
        <div>
          <div className="card-title">Composição Financeira</div>
          <div className="sub" style={{ marginTop: 2 }}>Detalhe do mês atual</div>
        </div>
        <Link href="/finance" className="cta-link" style={{ fontSize: 11 }}>Abrir <ArrowRight size={10} /></Link>
      </div>
      {loading || !s
        ? <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>{[0, 1, 2].map(i => <Skel key={i} h={44} r={8} />)}</div>
        : <div style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
            {rows.map((r, i) => {
              const tot = Math.abs(r.a) + Math.abs(r.b);
              const pct = tot > 0 ? (Math.abs(r.a) / tot) * 100 : 0;
              return (
                <motion.div key={r.lbl} initial={{ opacity: 0, y: 10 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} transition={{ delay: i * 0.08, duration: 0.4, ease: EASE_OUT }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 7 }}>
                    <span className="label" style={{ display: 'flex', alignItems: 'center', gap: 6 }}><span style={{ color: r.c, display: 'inline-flex' }}>{r.icon}</span>{r.lbl}</span>
                    <span className="num" style={{ fontSize: 12, fontWeight: 700, color: C.text2 }}>{S(r.a)} <span style={{ color: C.muted }}>/ {S(r.b)}</span></span>
                  </div>
                  <AnimatedBar pct={pct} color={r.c} h={5} />
                  {r.note && <div className="sub" style={{ fontSize: 10, marginTop: 5 }}>{r.note}</div>}
                </motion.div>
              );
            })}
            <hr className="divider" />
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div className="label" style={{ marginBottom: 4, display: 'flex', alignItems: 'center', gap: 5 }}><Repeat size={12} style={{ color: C.info }} />Assinaturas/mês</div>
                <SpringMetric value={s.subscriptionTotal} prefix="R$ " className="metric-md num" style={{ color: C.info } as React.CSSProperties} />
              </div>
              <div style={{ textAlign: 'right' }}>
                <div className="label" style={{ marginBottom: 4 }}>Saldo projetado</div>
                <SpringMetric value={Math.abs(s.projectedRemainingBalance)} prefix="R$ " suffix={s.projectedRemainingBalance < 0 ? ' ↓' : ''} className="metric-md num" style={{ color: s.projectedRemainingBalance >= 0 ? C.primary : C.loss } as React.CSSProperties} />
              </div>
            </div>
          </div>}
    </motion.div>
  );
}

/* ── Pipeline stages card ───────────────────────────────────────── */
function PipelineStagesCard({ data, loading }: { data: DashData | null; loading: boolean }) {
  return (
    <motion.div className="card" whileHover={{ boxShadow: '0 16px 48px rgba(0,0,0,0.4)', transition: { duration: 0.2 } }}>
      <div className="card-h" style={{ alignItems: 'center' }}>
        <div>
          <div className="card-title">Pipeline — Conversão por Etapa</div>
          <div className="sub" style={{ marginTop: 2 }}>Taxa entre estágios</div>
        </div>
        <Link href="/leads" className="cta-link" style={{ fontSize: 11 }}>Ver todos <ArrowRight size={10} /></Link>
      </div>
      {loading
        ? <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 10 }}>{[0, 1, 2, 3, 4].map(i => <Skel key={i} h={104} r={12} />)}</div>
        : data && (
          <motion.div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 10 }} variants={stagger5} initial="hidden" whileInView="show" viewport={{ once: true }}>
            {[
              { lbl: 'Leads', n: data.funnel.lead, c: C.funnel[0], next: data.funnel.contacted },
              { lbl: 'Em Contato', n: data.funnel.contacted, c: C.funnel[1], next: data.funnel.qualified },
              { lbl: 'Qualificados', n: data.funnel.qualified, c: C.funnel[2], next: data.funnel.negotiating },
              { lbl: 'Negociando', n: data.funnel.negotiating, c: C.funnel[3], next: data.funnel.customer },
              { lbl: 'Clientes', n: data.funnel.customer, c: C.funnel[4], next: null },
            ].map((s) => {
              const conv = s.n > 0 && s.next !== null ? (s.next / s.n) * 100 : null;
              return (
                <motion.div key={s.lbl} className="stage-card" variants={popVariants} whileHover={{ y: -3, scale: 1.03, borderColor: `${s.c}55`, transition: { type: 'spring', stiffness: 380, damping: 22 } }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 10 }}>
                    <motion.div style={{ width: 8, height: 8, borderRadius: '50%', background: s.c, boxShadow: `0 0 8px ${s.c}` }} animate={{ scale: [1, 1.25, 1] }} transition={{ duration: 2.6, repeat: Infinity, ease: 'easeInOut' }} />
                    <span className="label" style={{ fontSize: 9 }}>{s.lbl}</span>
                  </div>
                  <SpringMetric value={s.n} className="num" style={{ fontSize: 28, fontWeight: 800, color: s.c, lineHeight: 1 } as React.CSSProperties} stiffness={80} damping={16} />
                  {conv !== null
                    ? <div style={{ marginTop: 10 }}>
                        <AnimatedBar pct={conv} color={s.c} h={3} />
                        <div className="num" style={{ fontSize: 10, fontWeight: 700, color: conv < 30 ? C.loss : s.c, marginTop: 4 }}>{conv.toFixed(0)}% →</div>
                      </div>
                    : <div style={{ marginTop: 10, display: 'flex', alignItems: 'center', gap: 3 }}>
                        <CheckCircle size={10} color={C.primary} /><span style={{ fontSize: 10, color: C.primary, fontWeight: 700 }}>Final</span>
                      </div>}
                </motion.div>
              );
            })}
          </motion.div>
        )}
    </motion.div>
  );
}

/* ── Agenda card ────────────────────────────────────────────────── */
function AgendaCard({ data, loading }: { data: DashData | null; loading: boolean }) {
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap} style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <div className="card-h" style={{ alignItems: 'center' }}>
        <div>
          <div className="card-title">Agenda de Hoje</div>
          <div className="sub" style={{ marginTop: 2 }}>{new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'short' })}</div>
        </div>
        <Link href="/agenda" aria-label="Abrir agenda" style={{ display: 'inline-flex', alignItems: 'center', padding: '6px 9px', borderRadius: 8, background: 'rgba(255,255,255,0.06)', color: C.text3, textDecoration: 'none' }}><Activity size={13} /></Link>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8, flex: 1 }}>
        <AnimatePresence>
          {loading ? [0, 1, 2].map(i => <Skel key={i} h={60} r={10} />)
            : data?.agenda?.length ? data.agenda.slice(0, 5).map((ev, i) => (
              <motion.div key={i} initial={{ opacity: 0, x: 16, scale: 0.95 }} animate={{ opacity: 1, x: 0, scale: 1 }} transition={{ type: 'spring', stiffness: 300, damping: 24, delay: i * 0.08 }}
                whileHover={{ x: 3, transition: { type: 'spring', stiffness: 400, damping: 20 } }}
                style={{ display: 'flex', gap: 10, padding: '10px 12px', borderRadius: 12, background: 'rgba(0,212,170,0.05)', border: '1px solid rgba(0,212,170,0.12)' }}>
                <div style={{ width: 3, borderRadius: 999, background: C.primary, flexShrink: 0 }} />
                <div>
                  <div className="num" style={{ fontSize: 10, color: C.primary, marginBottom: 2 }}>{ev.time}</div>
                  <div style={{ fontSize: 13, fontWeight: 600, color: C.text }}>{ev.title}</div>
                </div>
              </motion.div>
            )) : (
              <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: C.muted, padding: '20px 0' }}>
                <motion.div animate={{ rotate: [0, 8, -8, 0], y: [0, -4, 0] }} transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }} style={{ fontSize: 28 }}>📅</motion.div>
                <div className="sub">Nenhum compromisso hoje</div><Link href="/agenda" className="cta-link">Agendar →</Link>
              </motion.div>
            )}
        </AnimatePresence>
      </div>
    </motion.div>
  );
}

/* ── Goal ring (uses generalized AnimatedRing with icon center) ── */
function GoalRing({ goal }: { goal: FinancialGoal }) {
  const meta = GOAL_META[goal.category] ?? GOAL_META[GoalCategory.Other];
  const pct = Math.min(Math.max(goal.progress ?? (goal.targetAmount > 0 ? (goal.currentAmount / goal.targetAmount) * 100 : 0), 0), 100);
  const done = goal.isCompleted || pct >= 100;
  const color = done ? C.success : meta.color;
  return (
    <motion.div
      variants={popVariants}
      whileHover={{ y: -4, scale: 1.03, transition: { type: 'spring', stiffness: 380, damping: 22 } }}
      style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', gap: 8, padding: '6px 4px' }}
    >
      <AnimatedRing pct={pct} color={color} size={94} stroke={8} center={
        <motion.span style={{ color, display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}
          animate={done ? { scale: [1, 1.18, 1] } : undefined} transition={done ? { duration: 1.8, repeat: Infinity, ease: 'easeInOut' } : undefined}>
          {done ? <Sparkles size={20} /> : meta.icon}
          <span className="num" style={{ fontSize: 13, fontWeight: 800, color }}>{pct.toFixed(0)}%</span>
        </motion.span>
      } />
      <div>
        <div style={{ fontSize: 12, fontWeight: 700, color: C.text, lineHeight: 1.2, maxWidth: 130, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{goal.name}</div>
        <div className="num" style={{ fontSize: 10, color: C.text3, marginTop: 3 }}>{S(goal.currentAmount)} <span style={{ color: C.muted }}>/ {S(goal.targetAmount)}</span></div>
      </div>
    </motion.div>
  );
}

/* ── Goals card — radial progress rings (real /planner/goals) ───── */
function GoalsCard({ goals, loading }: { goals: FinancialGoal[]; loading: boolean }) {
  const active = goals.filter(g => g.isActive !== false).sort((a, b) => (b.progress ?? 0) - (a.progress ?? 0)).slice(0, 4);
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h" style={{ alignItems: 'center' }}>
        <div>
          <div className="card-title">Metas Financeiras</div>
          <div className="sub" style={{ marginTop: 2 }}>{goals.length} {goals.length === 1 ? 'meta ativa' : 'metas ativas'}</div>
        </div>
        <Link href="/finance/planner/goals" className="cta-link" style={{ fontSize: 11 }}>Gerenciar <ArrowRight size={10} /></Link>
      </div>
      {loading
        ? <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2,1fr)', gap: 16 }}>{[0, 1, 2, 3].map(i => <Skel key={i} h={150} r={12} />)}</div>
        : active.length
          ? <motion.div style={{ display: 'grid', gridTemplateColumns: active.length === 1 ? '1fr' : 'repeat(2,1fr)', gap: 14 }} variants={stagger5} initial="hidden" whileInView="show" viewport={{ once: true }}>
              {active.map((g) => <GoalRing key={g.id} goal={g} />)}
            </motion.div>
          : <div style={{ height: 180, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: C.muted }}>
              <motion.div animate={{ y: [0, -6, 0] }} transition={{ duration: 2.6, repeat: Infinity, ease: 'easeInOut' }}><Target size={28} /></motion.div>
              <div className="sub">Nenhuma meta ativa</div><Link href="/finance/planner/goals" className="cta-link">Criar meta →</Link>
            </div>}
    </motion.div>
  );
}

/* ── Cash-flow forecast card (real /planner/simulations) ────────── */
function CashFlowForecastCard({ sim, mounted, loading }: { sim: MonthlySimulation | null; mounted: boolean; loading: boolean }) {
  const hasData = sim && sim.dailyBalances?.length;
  const cfg = mounted && hasData ? forecastConfig(sim!) : null;
  const rec = sim?.recommendations?.slice().sort((a, b) => a.priority - b.priority)[0];
  return (
    <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
      <div className="card-h">
        <div>
          <div className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
            <Activity size={15} style={{ color: sim?.hasNegativeBalanceRisk ? C.warn : C.primary }} />Projeção de Fluxo de Caixa
          </div>
          <div className="sub" style={{ marginTop: 2 }}>Saldo diário projetado — mês atual</div>
        </div>
        {sim && (sim.hasNegativeBalanceRisk
          ? <span className="badge badge-warn"><AlertTriangle size={10} /> Risco de saldo negativo</span>
          : <span className="badge badge-teal"><CheckCircle size={10} /> Saldo saudável</span>)}
      </div>
      {!mounted || loading ? <Skel h={280} r={10} />
        : cfg ? <ApexChart {...cfg} />
          : <div style={{ height: 280, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: C.muted }}>
              <motion.div animate={{ y: [0, -6, 0] }} transition={{ duration: 2.4, repeat: Infinity, ease: 'easeInOut' }}><Activity size={28} /></motion.div>
              <div className="sub">Sem simulação para este mês</div><Link href="/finance/planner" className="cta-link">Configurar planner →</Link>
            </div>}
      {sim && !loading && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 10, marginTop: 16, paddingTop: 16, borderTop: '1px solid var(--border-2)' }}>
          {[
            { lbl: 'Saldo final projetado', val: sim.projectedEndingBalance, c: sim.projectedEndingBalance >= 0 ? C.primary : C.loss },
            { lbl: 'Menor saldo do mês', val: sim.lowestProjectedBalance, c: sim.lowestProjectedBalance >= 0 ? C.success : C.loss },
          ].map(m => (
            <div key={m.lbl}>
              <div className="label" style={{ marginBottom: 4 }}>{m.lbl}</div>
              <SpringMetric value={Math.abs(m.val)} prefix="R$ " suffix={m.val < 0 ? ' ↓' : ''} className="num" style={{ fontSize: 18, fontWeight: 700, color: m.c } as React.CSSProperties} stiffness={70} damping={16} />
            </div>
          ))}
          <div>
            <div className="label" style={{ marginBottom: 4 }}>Alerta de risco</div>
            <div className="num" style={{ fontSize: 14, fontWeight: 700, color: sim.firstNegativeBalanceDate ? C.loss : C.success, lineHeight: 1.3 }}>
              {sim.firstNegativeBalanceDate ? new Date(sim.firstNegativeBalanceDate).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) : 'Sem risco'}
            </div>
          </div>
        </div>
      )}
      {rec && !loading && (
        <motion.div initial={{ opacity: 0, y: 8 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} transition={{ delay: 0.2, duration: 0.4, ease: EASE_OUT }}
          style={{ display: 'flex', gap: 10, marginTop: 12, padding: '10px 12px', borderRadius: 12, background: `${C.warn}10`, border: `1px solid ${C.warn}28` }}>
          <Zap size={15} style={{ color: C.warn, flexShrink: 0, marginTop: 1 }} />
          <div>
            <div style={{ fontSize: 12, fontWeight: 700, color: C.text }}>{rec.title}</div>
            <div className="sub" style={{ fontSize: 11, marginTop: 2 }}>{rec.message}</div>
          </div>
        </motion.div>
      )}
    </motion.div>
  );
}

/* ── Tab segmented control with sliding pill ────────────────────── */
function TabSlider({ tab, setTab }: { tab: Tab; setTab: (t: Tab) => void }) {
  const tabs: { id: Tab; label: string; icon: React.ReactNode }[] = [
    { id: 'overview', label: 'Visão Geral', icon: <LayoutGrid size={14} /> },
    { id: 'pipeline', label: 'Pipeline', icon: <Users size={14} /> },
    { id: 'finance', label: 'Financeiro', icon: <Wallet size={14} /> },
  ];
  return (
    <div className="seg" role="tablist" aria-label="Visões do dashboard">
      <LayoutGroup id="dash-tabs">
        {tabs.map(t => (
          <button key={t.id} role="tab" aria-selected={tab === t.id} aria-controls={`panel-${t.id}`} className="seg-btn" onClick={() => setTab(t.id)}>
            {tab === t.id && <motion.span layoutId="segPill" className="seg-pill" style={{ inset: 0 }} transition={{ type: 'spring', stiffness: 380, damping: 30 }} />}
            <span style={{ position: 'relative', zIndex: 1, display: 'inline-flex', alignItems: 'center', gap: 6 }}>{t.icon}{t.label}</span>
          </button>
        ))}
      </LayoutGroup>
    </div>
  );
}

/* ═══════════════════════════════════════════════════════════════
   MAIN PAGE
   ═══════════════════════════════════════════════════════════════ */
export default function DashboardPage() {
  const [mounted, setMounted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<DashData | null>(null);
  const [tab, setTab] = useState<Tab>('overview');
  const [updated, setUpdated] = useState<Date | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const now = new Date();
      const yr = now.getFullYear(), mo = now.getMonth() + 1;
      const [py, pm] = prevMonthOf(yr, mo, 1);
      const months6: [number, number][] = Array.from({ length: 6 }, (_, i) => prevMonthOf(yr, mo, 5 - i));

      const [mRes, p1Res, ...rest] = await Promise.allSettled([
        financeService.getPersonalFinanceMonth(yr, mo),
        financeService.getPersonalFinanceMonth(py, pm),
        ...months6.map(([y, m]) => financeService.getPersonalFinanceMonth(y, m)),
      ]);

      const [fL, fC, fQ, fN, fCust] = await Promise.allSettled([
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Lead }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Contacted }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Qualified }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Negotiating }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Customer }),
      ]);

      const [emailRes, agendaRes, goalsRes, simRes] = await Promise.allSettled([
        apiFetch<{ overallStats: EmailStats }>('/email-campaigns/analytics?days=30').catch(() => null),
        agendaService.getByDateRange(
          new Date(yr, mo - 1, now.getDate()).toISOString(),
          new Date(yr, mo - 1, now.getDate(), 23, 59).toISOString()
        ).catch(() => []),
        plannerService.getActiveFinancialGoals().catch(() => [] as FinancialGoal[]),
        plannerService.getOrGenerateSimulation(yr, mo).catch(() => null),
      ]);

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const get = (r: PromiseSettledResult<any>): any => r.status === 'fulfilled' ? r.value : null;

      const currMonth = get(mRes) as PersonalFinanceMonthResponse | null;
      const prevMonth1 = get(p1Res) as PersonalFinanceMonthResponse | null;

      const monthsArr = rest.slice(0, 6).map(r => get(r) as PersonalFinanceMonthResponse | null);
      const trend: MonthPoint[] = monthsArr.map((m, i) => ({
        label: MONTHS[months6[i][1] - 1],
        income: m?.summary?.totalIncome ?? 0,
        expense: m?.summary?.totalExpenses ?? 0,
      }));

      const fc = (r: PromiseSettledResult<{ totalCount?: number }>): number => (get(r) as { totalCount?: number } | null)?.totalCount ?? 0;
      const funnel: Funnel = { lead: fc(fL), contacted: fc(fC), qualified: fc(fQ), negotiating: fc(fN), customer: fc(fCust) };

      const expMap: Record<string, number> = {};
      (currMonth?.expenses ?? []).forEach(t => {
        const c = t.categoryName ?? 'Outros';
        expMap[c] = (expMap[c] ?? 0) + Math.abs(t.amount);
      });
      const expTotal = Object.values(expMap).reduce((a, b) => a + b, 0);
      const expenses: CatExpense[] = Object.entries(expMap)
        .sort(([, a], [, b]) => b - a).slice(0, 5)
        .map(([name, total]) => ({ name, total, pct: expTotal > 0 ? (total / expTotal) * 100 : 0 }));

      const emailData = get(emailRes) as { overallStats: EmailStats } | null;
      const rawAgenda = get(agendaRes) as { startTime?: string; title?: string }[] | null;
      const agenda = (rawAgenda ?? []).map(a => ({
        time: a?.startTime ? new Date(a.startTime).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : '--:--',
        title: a?.title ?? 'Compromisso',
      }));

      const goals = (get(goalsRes) as FinancialGoal[] | null) ?? [];
      const simulation = get(simRes) as MonthlySimulation | null;

      setData({ funnel, curr: currMonth, prev: prevMonth1, trend, email: emailData?.overallStats ?? null, expenses, agenda, goals, simulation });
      setUpdated(new Date());
    } catch (e) { console.error('[dash]', e); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { setMounted(true); load(); }, [load]);

  /* derived (all real) */
  const cs = data?.curr?.summary;
  const income = cs?.totalIncome ?? 0;
  const expenses = cs?.totalExpenses ?? 0;
  const cashFlow = cs?.remainingBalance ?? 0;
  const projected = cs?.projectedRemainingBalance ?? 0;
  const totalLeads = (data?.funnel.lead ?? 0) + (data?.funnel.contacted ?? 0) + (data?.funnel.qualified ?? 0) + (data?.funnel.negotiating ?? 0);
  const customers = data?.funnel.customer ?? 0;
  const openRate = data?.email?.openRate ?? 0;
  const prev = data?.prev?.summary;
  const revMoM = cs && prev && prev.totalIncome > 0 ? ((income - prev.totalIncome) / prev.totalIncome) * 100 : 0;
  const expMoM = cs && prev && prev.totalExpenses > 0 ? ((expenses - prev.totalExpenses) / prev.totalExpenses) * 100 : 0;

  const trendIncome = data?.trend.map(t => t.income) ?? [];
  const trendExp = data?.trend.map(t => t.expense) ?? [];
  // REAL series only — funnel distribution is genuine stage counts (no synthetic fill)
  const funnelSpark = data ? [data.funnel.lead, data.funnel.contacted, data.funnel.qualified, data.funnel.negotiating, data.funnel.customer] : [];

  const convRate = (totalLeads + customers) > 0 ? (customers / (totalLeads + customers)) * 100 : 0;
  const worstDrop = data ? [
    { lbl: 'L→Contato', v: data.funnel.lead > 0 ? (1 - data.funnel.contacted / data.funnel.lead) * 100 : 0 },
    { lbl: 'Contato→Qualif', v: data.funnel.contacted > 0 ? (1 - data.funnel.qualified / data.funnel.contacted) * 100 : 0 },
    { lbl: 'Qualif→Neg', v: data.funnel.qualified > 0 ? (1 - data.funnel.negotiating / data.funnel.qualified) * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b) : { lbl: '—', v: 0 };

  const overviewKpis = [
    { label: 'Receita do Mês', value: income, prefix: 'R$ ', delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, spark: trendIncome, color: C.primary, icon: <DollarSign size={14} /> },
    { label: 'Total Despesas', value: expenses, prefix: 'R$ ', delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, spark: trendExp, color: C.warn, icon: <Wallet size={14} /> },
    { label: 'Leads no Funil', value: totalLeads, spark: funnelSpark, color: C.info, up: true, icon: <Users size={14} /> },
    {
      label: 'Abertura Email', value: openRate, suffix: '%', color: C.accent, up: openRate >= 20, icon: <Mail size={14} />,
      mini: (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
            <span className="sub" style={{ fontSize: 10 }}>vs meta 20%</span>
            <span className="num" style={{ fontSize: 11, fontWeight: 700, color: openRate >= 20 ? C.success : C.warn }}>{openRate >= 20 ? 'acima' : 'abaixo'}</span>
          </div>
          <AnimatedBar pct={Math.min((openRate / 20) * 100, 100)} color={openRate >= 20 ? C.success : C.warn} h={5} />
          <div className="sub" style={{ fontSize: 10, marginTop: 8 }}>{data?.email?.clickRate?.toFixed(1) ?? '0'}% de cliques</div>
        </div>
      ),
    },
  ];

  const financeKpis = [
    { label: 'Receita', value: income, prefix: 'R$ ', delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, spark: trendIncome, color: C.primary, icon: <DollarSign size={14} /> },
    { label: 'Despesas', value: expenses, prefix: 'R$ ', delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, spark: trendExp, color: C.warn, icon: <Wallet size={14} /> },
    { label: 'Saldo Líquido', value: Math.abs(cashFlow), prefix: 'R$ ', suffix: cashFlow < 0 ? ' ↓' : '', spark: trendIncome.map((v, i) => v - (trendExp[i] ?? 0)), color: cashFlow >= 0 ? C.success : C.loss, icon: <PiggyBank size={14} /> },
    { label: 'Saldo Projetado', value: Math.abs(projected), prefix: 'R$ ', suffix: projected < 0 ? ' ↓' : '', color: projected >= 0 ? C.info : C.loss, icon: <Target size={14} />, mini: <div style={{ paddingTop: 6 }}><div className="sub" style={{ fontSize: 11 }}>Projeção do fechamento do mês considerando recorrências.</div></div> },
  ];

  const pipelineKpis = [
    { label: 'Leads no Funil', value: totalLeads, color: C.info, up: true, icon: <Users size={14} />, mini: <div style={{ paddingTop: 8 }} className="sub">{customers} já viraram clientes</div> },
    { label: 'Clientes', value: customers, color: C.primary, up: true, icon: <CheckCircle size={14} />, mini: <div style={{ paddingTop: 8 }} className="sub">Estágio final do funil</div> },
    { label: 'Taxa de Conversão', value: convRate, suffix: '%', color: convRate >= 10 ? C.success : C.warn, up: convRate >= 10, icon: <Target size={14} />, mini: <div style={{ paddingTop: 6 }}><AnimatedBar pct={Math.min(convRate * 4, 100)} color={convRate >= 10 ? C.success : C.warn} h={5} /><div className="sub" style={{ fontSize: 10, marginTop: 8 }}>contatos → clientes</div></div> },
    { label: 'Maior Gargalo', value: worstDrop.v, suffix: '%', color: worstDrop.v > 50 ? C.loss : C.warn, up: false, icon: <AlertTriangle size={14} />, mini: <div style={{ paddingTop: 8 }} className="sub">drop em {worstDrop.lbl}</div> },
  ];

  return (
    <>
      <style>{CSS}</style>
      <motion.div className="dsh" variants={pageVariants} initial="hidden" animate="show" style={{ display: 'flex', flexDirection: 'column', gap: 18, paddingBottom: 48 }}>
        {/* Topbar */}
        <motion.div variants={sectionVariants} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <h1 style={{ margin: 0, fontSize: 22, fontWeight: 800, color: C.text, letterSpacing: '-0.02em' }}>Dashboard</h1>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 4 }}>
              <span className="live-dot" />
              <span style={{ fontSize: 12, color: C.muted }}>{updated ? `Atualizado ${updated.toLocaleTimeString('pt-BR')}` : 'Carregando…'}</span>
              <span style={{ color: '#3f3f46', fontSize: 12 }}>·</span>
              <span style={{ fontSize: 12, color: C.muted }}>{new Date().toLocaleDateString('pt-BR', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })}</span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
            <TabSlider tab={tab} setTab={setTab} />
            <motion.button onClick={load} disabled={loading} className="btn-ghost" whileHover={{ scale: 1.04 }} whileTap={{ scale: 0.94 }} style={{ opacity: loading ? 0.5 : 1 }} aria-label="Atualizar dados">
              <motion.span animate={{ rotate: loading ? 360 : 0 }} transition={{ duration: 1, repeat: loading ? Infinity : 0, ease: 'linear' }}><RefreshCw size={13} /></motion.span>
              Atualizar
            </motion.button>
          </div>
        </motion.div>

        {/* Tab panels */}
        <AnimatePresence mode="wait">
          {/* ── OVERVIEW ── */}
          {tab === 'overview' && (
            <motion.div key="overview" id="panel-overview" role="tabpanel" variants={tabPanel} initial="hidden" animate="show" exit="exit" style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
              <HeroCard data={data} loading={loading} mounted={mounted} />
              <CommandCenter data={data} loading={loading} />
              <motion.div className="g4" variants={stagger7}>
                {overviewKpis.map((k, i) => <KpiCard key={k.label} {...k} loading={loading} idx={i} />)}
              </motion.div>
              <Section><div className="g73"><TrendCard data={data} mounted={mounted} loading={loading} revMoM={revMoM} /><EmailCard data={data} loading={loading} /></div></Section>
              <Section><div className="g2"><FunnelCard data={data} mounted={mounted} loading={loading} totalLeads={totalLeads} /><ExpenseCard data={data} mounted={mounted} loading={loading} expenses={expenses} cashFlow={cashFlow} /></div></Section>
              <Section><div className="g63"><PipelineStagesCard data={data} loading={loading} /><AgendaCard data={data} loading={loading} /></div></Section>
            </motion.div>
          )}

          {/* ── PIPELINE ── */}
          {tab === 'pipeline' && (
            <motion.div key="pipeline" id="panel-pipeline" role="tabpanel" variants={tabPanel} initial="hidden" animate="show" exit="exit" style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
              <motion.div className="g4" variants={stagger7}>
                {pipelineKpis.map((k, i) => <KpiCard key={k.label} {...k} loading={loading} idx={i} />)}
              </motion.div>
              <Section><div className="g63"><FunnelCard data={data} mounted={mounted} loading={loading} totalLeads={totalLeads} /><AgendaCard data={data} loading={loading} /></div></Section>
              <Section><PipelineStagesCard data={data} loading={loading} /></Section>
              <CommandCenter data={data} loading={loading} />
            </motion.div>
          )}

          {/* ── FINANCE ── */}
          {tab === 'finance' && (
            <motion.div key="finance" id="panel-finance" role="tabpanel" variants={tabPanel} initial="hidden" animate="show" exit="exit" style={{ display: 'flex', flexDirection: 'column', gap: 18 }}>
              <HeroCard data={data} loading={loading} mounted={mounted} />
              <motion.div className="g4" variants={stagger7}>
                {financeKpis.map((k, i) => <KpiCard key={k.label} {...k} loading={loading} idx={i} />)}
              </motion.div>
              <Section><CashFlowForecastCard sim={data?.simulation ?? null} mounted={mounted} loading={loading} /></Section>
              <Section><div className="g73"><TrendCard data={data} mounted={mounted} loading={loading} revMoM={revMoM} /><FinanceBreakdownCard summary={cs} loading={loading} /></div></Section>
              <Section><div className="g2"><ExpenseCard data={data} mounted={mounted} loading={loading} expenses={expenses} cashFlow={cashFlow} donut /><GoalsCard goals={data?.goals ?? []} loading={loading} /></div></Section>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </>
  );
}
