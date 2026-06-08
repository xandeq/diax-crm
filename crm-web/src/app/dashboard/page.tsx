'use client';

import {
  motion, AnimatePresence,
  useMotionValue, useSpring, useTransform,
  type Variants,
} from 'framer-motion';
import dynamic from 'next/dynamic';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Activity, AlertTriangle, ArrowDownRight, ArrowRight, ArrowUpRight,
  CheckCircle, DollarSign, Mail, RefreshCw, Target, TrendingDown,
  TrendingUp, Zap,
} from 'lucide-react';

import { HeroParticles } from '@/components/dashboard/HeroParticles';
import { SpringMetric } from '@/components/dashboard/SpringMetric';
import { financeService, PersonalFinanceMonthResponse } from '@/services/finance';
import { getLeads, CustomerStatus as LeadStatus } from '@/services/leads';
import { agendaService } from '@/services/agenda';
import { apiFetch } from '@/services/api';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

/* ── utils ──────────────────────────────────────────────────────── */
function prevMonthOf(y: number, m: number, offset: number): [number, number] {
  let mo = m - offset, yr = y;
  while (mo <= 0) { mo += 12; yr--; }
  return [yr, mo];
}
const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });
const S = (v: number) => Math.abs(v) >= 1_000_000 ? `R$ ${(v / 1_000_000).toFixed(1)}M` : Math.abs(v) >= 1_000 ? `R$ ${(v / 1_000).toFixed(1)}k` : R(v);
const MONTHS = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

/* ── animation variants ─────────────────────────────────────────── */
const EASE_OUT = [0.16, 1, 0.3, 1] as [number, number, number, number];

const pageVariants: Variants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: { staggerChildren: 0.06, delayChildren: 0.05 },
  },
};

const sectionVariants: Variants = {
  hidden: { opacity: 0, y: 24 },
  show: { opacity: 1, y: 0, transition: { duration: 0.5, ease: EASE_OUT } },
};

const popVariants: Variants = {
  hidden: { opacity: 0, y: 28, scale: 0.88 },
  show: {
    opacity: 1, y: 0, scale: 1,
    transition: { type: 'spring', stiffness: 320, damping: 22 },
  },
};

const stagger7: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.07, delayChildren: 0.08 } },
};

const stagger5: Variants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.06, delayChildren: 0.06 } },
};

// Card hover/tap — no borderColor (not in TargetAndTransition for divs)
const cardHover = {
  y: -5,
  scale: 1.018,
  boxShadow: '0 20px 56px rgba(0,0,0,0.42)',
  transition: { type: 'spring', stiffness: 380, damping: 26 },
} as const;
const cardTap = {
  scale: 0.978,
  transition: { type: 'spring', stiffness: 500, damping: 30 },
} as const;

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
}

/* ── chart configs ──────────────────────────────────────────────── */
const animCfg = { enabled: true, easing: 'easeinout', speed: 1100, animateGradually: { enabled: true, delay: 120 }, dynamicAnimation: { enabled: true, speed: 500 } };

function sparkConfig(color: string, data: number[]) {
  return {
    type: 'area' as const, height: 58,
    series: [{ data: data.length ? data : [0] }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, animations: animCfg },
      stroke: { curve: 'smooth' as const, width: 2 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.32, opacityTo: 0 } },
      colors: [color], tooltip: { enabled: false },
    },
  };
}

function heroChartConfig(trend: MonthPoint[]) {
  return {
    type: 'area' as const, height: 130,
    series: [{ name: 'Receita', data: trend.map(t => Math.round(t.income)) }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, background: 'transparent', animations: { ...animCfg, speed: 1400 } },
      stroke: { curve: 'smooth' as const, width: 3 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.32, opacityTo: 0 } },
      colors: ['#00D4AA'],
      tooltip: { enabled: true, theme: 'dark' as const },
      xaxis: { categories: trend.map(t => t.label) },
    },
  };
}

function trendConfig(trend: MonthPoint[]) {
  return {
    type: 'line' as const, height: 220,
    series: [
      { name: 'Receita', type: 'area', data: trend.map(t => Math.round(t.income)) },
      { name: 'Despesas', type: 'line', data: trend.map(t => Math.round(t.expense)) },
    ],
    options: {
      chart: { toolbar: { show: false }, background: 'transparent', animations: animCfg },
      colors: ['#00D4AA', '#F59E0B'],
      stroke: { curve: 'smooth' as const, width: [2, 2] },
      fill: { type: ['gradient', 'none'], gradient: { opacityFrom: 0.2, opacityTo: 0 } },
      xaxis: { categories: trend.map(t => t.label), labels: { style: { colors: '#52525b', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { labels: { style: { colors: '#52525b', fontSize: '11px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` } },
      grid: { borderColor: 'rgba(255,255,255,0.04)', strokeDashArray: 4 },
      legend: { labels: { colors: '#71717a' }, position: 'top' as const },
      tooltip: { theme: 'dark' as const },
      markers: { size: 4, colors: ['#00D4AA', '#F59E0B'], strokeColors: 'transparent', hover: { size: 6 } },
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
    type: 'bar' as const, height: 200,
    series: [{ name: 'Total', data: stages.map(s => s.v) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: animCfg },
      plotOptions: { bar: { horizontal: true, barHeight: '58%', borderRadius: 7, distributed: true } },
      colors: ['#6366F1', '#8B5CF6', '#A78BFA', '#EC4899', '#00D4AA'],
      xaxis: { labels: { style: { colors: '#52525b', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { categories: stages.map(s => s.name), labels: { style: { colors: '#a1a1aa', fontSize: '12px' } } },
      grid: { borderColor: 'rgba(255,255,255,0.04)', xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      dataLabels: { enabled: true, style: { colors: ['#fff'], fontSize: '11px', fontWeight: 700 }, formatter: (v: number) => v > 0 ? String(v) : '' },
      legend: { show: false },
      tooltip: { theme: 'dark' as const },
    },
  };
}

function expenseConfig(cats: CatExpense[]) {
  return {
    type: 'bar' as const, height: 195,
    series: [{ name: 'Valor', data: cats.map(c => Math.round(c.total)) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: animCfg },
      plotOptions: { bar: { horizontal: true, barHeight: '52%', borderRadius: 6, distributed: true } },
      colors: ['#F59E0B', '#EF4444', '#8B5CF6', '#6366F1', '#EC4899'],
      xaxis: { labels: { style: { colors: '#52525b', fontSize: '10px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { categories: cats.map(c => c.name.length > 14 ? c.name.slice(0, 14) + '…' : c.name), labels: { style: { colors: '#a1a1aa', fontSize: '11px' } } },
      grid: { borderColor: 'rgba(255,255,255,0.04)', xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      dataLabels: { enabled: false },
      legend: { show: false },
      tooltip: { theme: 'dark' as const },
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
      setTimeout(() => width.set(`${Math.min(pct, 100)}%`), 80);
    }, { threshold: 0.2 });
    io.observe(el); return () => io.disconnect();
  }, [pct, width]);
  return (
    <div ref={ref} style={{ height: h, background: 'rgba(255,255,255,0.08)', borderRadius: 999, overflow: 'hidden' }}>
      <motion.div style={{ height: '100%', width, background: color, borderRadius: 999 }}
        transition={{ duration: 1.1, ease: [0.16, 1, 0.3, 1] }} />
    </div>
  );
}

/* ── Skeleton ───────────────────────────────────────────────────── */
function Skel({ h = 20, r = 8, w = '100%' }: { h?: number; r?: number; w?: string | number }) {
  return (
    <motion.div
      style={{ height: h, borderRadius: r, width: w, background: 'rgba(255,255,255,0.07)' }}
      animate={{ opacity: [0.4, 0.9, 0.4] }}
      transition={{ duration: 1.6, repeat: Infinity, ease: 'easeInOut' }}
    />
  );
}

/* ── CSS ─────────────────────────────────────────────────────────── */
const CSS = `
  /* CSS @property for animated gradient border */
  @property --glow-angle {
    syntax: '<angle>';
    inherits: false;
    initial-value: 0deg;
  }
  @keyframes glow-rotate { to { --glow-angle: 360deg; } }
  @keyframes metric-pop {
    0%   { transform: scale(0.88); opacity: 0; }
    65%  { transform: scale(1.04); opacity: 1; }
    100% { transform: scale(1); }
  }
  @keyframes live-pulse {
    0%,100% { transform: scale(1); opacity: 1; }
    50%      { transform: scale(1.6); opacity: 0.55; }
  }
  @keyframes float {
    0%,100% { transform: translateY(0); }
    50%      { transform: translateY(-5px); }
  }
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after {
      animation-duration: 0.01ms !important;
      transition-duration: 0.01ms !important;
    }
  }

  .dsh { font-family: -apple-system, BlinkMacSystemFont, 'Inter', sans-serif; }

  /* Hero card with animated gradient border */
  .hero-card {
    position: relative;
    background: radial-gradient(ellipse at 80% 50%, rgba(0,212,170,0.07) 0%, transparent 65%),
                rgba(255,255,255,0.035);
    border-radius: 24px;
    padding: 32px 36px;
    overflow: hidden;
    --glow-angle: 0deg;
    animation: glow-rotate 7s linear infinite;
  }
  .hero-card::before {
    content: '';
    position: absolute;
    inset: -1.5px;
    border-radius: 25.5px;
    background: conic-gradient(
      from var(--glow-angle),
      transparent 65%,
      oklch(52% 0.15 189°) 75%,
      oklch(65% 0.12 155°) 82%,
      oklch(55% 0.13 245°) 88%,
      transparent 100%
    );
    z-index: -1;
    opacity: 0.7;
  }
  .hero-card::after {
    content: '';
    position: absolute;
    inset: 0.5px;
    border-radius: 23.5px;
    background: oklch(9% 0.008 189°);
    z-index: -1;
  }

  /* Card base */
  .card {
    background: rgba(255,255,255,0.04);
    border: 1px solid rgba(255,255,255,0.09);
    border-radius: 18px;
    padding: 22px;
  }

  /* Typography */
  .metric-huge  { font-size: 54px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.04em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; line-height: 1; }
  .metric-lg    { font-size: 30px; font-weight: 700; color: #f4f4f5; letter-spacing: -0.03em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; }
  .metric-md    { font-size: 22px; font-weight: 700; color: #f4f4f5; letter-spacing: -0.02em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; }
  .label        { font-size: 11px; font-weight: 600; color: #71717a; text-transform: uppercase; letter-spacing: 0.08em; }
  .sub          { font-size: 13px; color: #71717a; }
  .teal         { color: #00D4AA; }
  .txt          { color: #e4e4e7; }
  .txt2         { color: #a1a1aa; }

  /* Badges */
  .badge        { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
  .badge-up     { background: rgba(16,185,129,0.15);  color: #10B981; }
  .badge-dn     { background: rgba(239,68,68,0.15);   color: #EF4444; }
  .badge-teal   { background: rgba(0,212,170,0.15);   color: #00D4AA; }
  .badge-warn   { background: rgba(245,158,11,0.15);  color: #F59E0B; }
  .badge-neu    { background: rgba(113,113,122,0.15); color: #a1a1aa; }

  /* Live indicator */
  .live-dot     { width: 7px; height: 7px; border-radius: 50%; background: #00D4AA; display: inline-block; animation: live-pulse 2s infinite; }

  /* Links */
  .cta-link     { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: #00D4AA; text-decoration: none; }
  .cta-link:hover { text-decoration: underline; }

  /* Stage cards */
  .stage-card   { background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.08); border-radius: 14px; padding: 16px; }

  /* Buttons */
  .btn-ghost    { display: flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 10px; border: none; font-size: 12px; font-weight: 600; cursor: pointer; background: rgba(255,255,255,0.06); color: #a1a1aa; }

  /* Grids */
  .g4           { display: grid; grid-template-columns: repeat(4,1fr); gap: 14px; }
  .g3           { display: grid; grid-template-columns: repeat(3,1fr); gap: 14px; }
  .g2           { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
  .g73          { display: grid; grid-template-columns: 1fr 310px; gap: 14px; }
  .g63          { display: grid; grid-template-columns: 1fr 280px; gap: 14px; }
  .g5cmd        { display: grid; grid-template-columns: repeat(5,1fr); gap: 10px; }

  /* Divider */
  .divider      { border: none; border-top: 1px solid rgba(255,255,255,0.06); margin: 0; }

  /* Responsive */
  @media(max-width:1200px) {
    .g4   { grid-template-columns: repeat(2,1fr) !important; }
    .g73, .g63 { grid-template-columns: 1fr !important; }
    .g5cmd { grid-template-columns: repeat(3,1fr) !important; }
  }
  @media(max-width:700px) {
    .g4,.g3,.g2,.g5cmd { grid-template-columns: 1fr !important; }
    .metric-huge { font-size: 38px !important; }
  }
`;

/* ── KPI Card ───────────────────────────────────────────────────── */
function KpiCard({ label, value, prefix = '', suffix = '', delta, up, spark, color, loading, idx }: {
  label: string; value: number; prefix?: string; suffix?: string;
  delta?: string; up?: boolean; spark: number[]; color: string; loading: boolean; idx: number;
}) {
  return (
    <motion.div
      className="card"
      variants={popVariants}
      whileHover={cardHover}
      whileTap={cardTap}
      style={{ cursor: 'default' }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
        <span className="label">{label}</span>
        {delta && (
          <motion.span
            className={`badge ${up ? 'badge-up' : 'badge-dn'}`}
            initial={{ scale: 0, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 400, damping: 20, delay: 0.12 + idx * 0.07 }}
          >
            {up ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}
            {delta}
          </motion.span>
        )}
      </div>
      {loading
        ? <Skel h={38} r={6} />
        : (
          <SpringMetric
            value={value}
            prefix={prefix}
            suffix={suffix}
            className="metric-lg"
            stiffness={65}
            damping={16}
          />
        )
      }
      <div style={{ marginTop: 12 }}>
        {loading ? <Skel h={58} r={6} /> : <ApexChart {...sparkConfig(color, spark)} />}
      </div>
    </motion.div>
  );
}

/* ── Command Center ─────────────────────────────────────────────── */
function CommandCenter({ data, loading }: { data: DashData | null; loading: boolean }) {
  if (loading || !data) {
    return (
      <motion.div className="g5cmd" variants={sectionVariants}>
        {[0, 1, 2, 3, 4].map(i => <Skel key={i} h={88} r={14} />)}
      </motion.div>
    );
  }

  const { funnel, curr, prev: prevD, email } = data;
  const cs = curr?.summary, ps = prevD?.summary;
  const revMoM = cs && ps && ps.totalIncome > 0 ? ((cs.totalIncome - ps.totalIncome) / ps.totalIncome) * 100 : 0;
  const totalPipeline = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;
  const worstDrop = [
    { lbl: 'Lead→Contato',   v: funnel.lead > 0       ? (1 - funnel.contacted  / funnel.lead)       * 100 : 0 },
    { lbl: 'Contato→Qualif', v: funnel.contacted > 0  ? (1 - funnel.qualified  / funnel.contacted)  * 100 : 0 },
    { lbl: 'Qualif.→Neg.',   v: funnel.qualified > 0  ? (1 - funnel.negotiating/ funnel.qualified)  * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b);
  const cashFlow = cs?.remainingBalance ?? 0;
  const topExpense = data.expenses[0];
  const convRate = (totalPipeline + funnel.customer) > 0 ? (funnel.customer / (totalPipeline + funnel.customer)) * 100 : 0;

  const answers = [
    { q: 'O que funciona?',         icon: <CheckCircle size={15} />, c: revMoM >= 0 ? '#00D4AA' : '#F59E0B',
      text: revMoM > 0 ? `Receita +${revMoM.toFixed(1)}% vs mês ant.` : 'Receita estável', href: '/finance' },
    { q: 'O que está quebrado?',    icon: <AlertTriangle size={15} />, c: worstDrop.v > 50 ? '#EF4444' : '#F59E0B',
      text: totalPipeline > 0 ? `${worstDrop.v.toFixed(0)}% drop em ${worstDrop.lbl}` : 'Pipeline vazio', href: '/leads' },
    { q: 'Onde perde dinheiro?',    icon: <DollarSign size={15} />, c: cashFlow < 0 ? '#EF4444' : '#F59E0B',
      text: cashFlow < 0 ? `Fluxo negativo: ${S(cashFlow)}` : topExpense ? `${topExpense.name}: ${topExpense.pct.toFixed(0)}% das despesas` : 'Verifique despesas', href: '/finance' },
    { q: 'O que fazer agora?',      icon: <Zap size={15} />, c: '#818CF8',
      text: totalPipeline > 0 ? `${totalPipeline} leads aguardam ação` : 'Adicione leads', href: '/leads' },
    { q: 'Qual ação gera receita?', icon: <Target size={15} />, c: '#00D4AA',
      text: email && email.openRate > 0 ? `Email: ${email.openRate.toFixed(1)}% abertura` : convRate > 0 ? `Conversão: ${convRate.toFixed(1)}%` : 'Analise canais', href: '/leads' },
  ];

  return (
    <motion.div className="g5cmd" variants={stagger5} initial="hidden" whileInView="show" viewport={{ once: true, margin: '-40px' }}>
      {answers.map((a, i) => (
        <Link key={i} href={a.href} style={{ textDecoration: 'none' }}>
          <motion.div
            variants={popVariants}
            whileHover={{ y: -4, scale: 1.025, boxShadow: `0 12px 36px ${a.c}28`, transition: { type: 'spring', stiffness: 380, damping: 24 } }}
            whileTap={{ scale: 0.965 }}
            style={{
              background: `${a.c}12`, border: `1px solid ${a.c}30`,
              borderRadius: 14, padding: '14px 16px', cursor: 'pointer',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <motion.span
                style={{ color: a.c }}
                animate={{ rotate: [0, 8, -8, 0] }}
                transition={{ duration: 2.5, repeat: Infinity, delay: i * 0.4, ease: 'easeInOut' }}
              >
                {a.icon}
              </motion.span>
              <span style={{ fontSize: 10, fontWeight: 700, color: a.c, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{a.q}</span>
            </div>
            <div style={{ fontSize: 12, color: '#d4d4d8', lineHeight: 1.45, fontWeight: 500 }}>{a.text}</div>
            <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 3, fontSize: 10, color: a.c, fontWeight: 600 }}>
              Ver detalhes <ArrowRight size={9} />
            </div>
          </motion.div>
        </Link>
      ))}
    </motion.div>
  );
}

/* ── Hero Card ──────────────────────────────────────────────────── */
function HeroCard({ data, loading, mounted }: { data: DashData | null; loading: boolean; mounted: boolean }) {
  const income       = data?.curr?.summary?.totalIncome ?? 0;
  const prev         = data?.prev?.summary?.totalIncome ?? 0;
  const mom          = prev > 0 ? ((income - prev) / prev) * 100 : 0;
  const cashFlow     = data?.curr?.summary?.remainingBalance ?? 0;
  const totalExpenses= data?.curr?.summary?.totalExpenses ?? 0;
  const GOAL         = 50000;
  const goalPct      = Math.min((income / GOAL) * 100, 100);
  const heroCfg      = mounted && data?.trend?.length ? heroChartConfig(data.trend) : null;

  return (
    <motion.div variants={sectionVariants} className="hero-card">
      {/* Particles — absolute, pointerEvents none */}
      <HeroParticles />

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 32, alignItems: 'center', position: 'relative', zIndex: 1 }}>
        {/* Left */}
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
            <span className="label" style={{ color: '#52525b' }}>Receita do Mês</span>
            <span className="live-dot" />
            <span style={{ fontSize: 10, color: '#52525b', fontWeight: 600 }}>AO VIVO</span>
          </div>

          <AnimatePresence mode="wait">
            {loading
              ? <Skel key="sk" h={62} r={8} w={260} />
              : (
                <motion.div
                  key="metric"
                  initial={{ opacity: 0, scale: 0.85, y: 12 }}
                  animate={{ opacity: 1, scale: 1, y: 0 }}
                  transition={{ type: 'spring', stiffness: 280, damping: 20, delay: 0.1 }}
                >
                  <SpringMetric
                    value={income}
                    prefix="R$ "
                    className="metric-huge teal"
                    stiffness={55}
                    damping={13}
                  />
                </motion.div>
              )
            }
          </AnimatePresence>

          <motion.div
            style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 14, flexWrap: 'wrap' }}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: loading ? 0 : 1, y: 0 }}
            transition={{ delay: 0.35, duration: 0.4 }}
          >
            <motion.span
              className={`badge ${mom >= 0 ? 'badge-up' : 'badge-dn'}`}
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              transition={{ type: 'spring', stiffness: 400, damping: 18, delay: 0.4 }}
            >
              {mom >= 0 ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
              {Math.abs(mom).toFixed(1)}% vs mês ant.
            </motion.span>
            {cashFlow >= 0
              ? <span className="badge badge-teal"><DollarSign size={10} /> Fluxo: {S(cashFlow)}</span>
              : <span className="badge badge-dn">Fluxo: {S(cashFlow)}</span>
            }
          </motion.div>

          <div style={{ marginTop: 22 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <span className="sub" style={{ fontSize: 12 }}>Progresso à meta mensal</span>
              {!loading && (
                <motion.span
                  style={{ fontSize: 12, fontWeight: 700, color: goalPct >= 80 ? '#00D4AA' : goalPct >= 60 ? '#F59E0B' : '#EF4444' }}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ delay: 0.5 }}
                >
                  {goalPct.toFixed(0)}%
                </motion.span>
              )}
            </div>
            {loading
              ? <Skel h={6} r={999} />
              : <AnimatedBar pct={goalPct} color={goalPct >= 80 ? '#00D4AA' : goalPct >= 60 ? '#F59E0B' : '#EF4444'} h={6} />
            }
          </div>
        </div>

        {/* Right */}
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
            <span className="label">Tendência — 6 meses</span>
            <Link href="/finance" className="cta-link">Detalhes <ArrowRight size={11} /></Link>
          </div>
          {loading || !heroCfg ? <Skel h={130} r={8} /> : <ApexChart {...heroCfg} />}
          <hr className="divider" style={{ margin: '16px 0' }} />
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            {loading ? [0, 1].map(i => <Skel key={i} h={42} r={8} />) : [
              { lbl: 'Despesas', val: totalExpenses, c: '#F59E0B' },
              { lbl: 'Saldo',    val: cashFlow,      c: cashFlow >= 0 ? '#00D4AA' : '#EF4444' },
            ].map(m => (
              <motion.div
                key={m.lbl}
                initial={{ opacity: 0, x: 10 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.45, duration: 0.35, ease: [0.16, 1, 0.3, 1] }}
              >
                <div className="label" style={{ marginBottom: 5 }}>{m.lbl}</div>
                <SpringMetric
                  value={Math.abs(m.val)}
                  prefix="R$ "
                  suffix={m.val < 0 ? ' ↓' : ''}
                  className="metric-md"
                  style={{ color: m.c, fontFamily: 'monospace' } as React.CSSProperties}
                  stiffness={70}
                  damping={16}
                />
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </motion.div>
  );
}

/* ── MAIN PAGE ──────────────────────────────────────────────────── */
export default function DashboardPage() {
  const [mounted, setMounted]     = useState(false);
  const [loading, setLoading]     = useState(true);
  const [data, setData]           = useState<DashData | null>(null);
  const [tab, setTab]             = useState('overview');
  const [updated, setUpdated]     = useState<Date | null>(null);

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

      const [emailRes, agendaRes] = await Promise.allSettled([
        apiFetch<{ overallStats: EmailStats }>('/email-campaigns/analytics?days=30').catch(() => null),
        agendaService.getByDateRange(
          new Date(yr, mo - 1, now.getDate()).toISOString(),
          new Date(yr, mo - 1, now.getDate(), 23, 59).toISOString()
        ).catch(() => []),
      ]);

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const get = (r: PromiseSettledResult<any>): any => r.status === 'fulfilled' ? r.value : null;

      const currMonth  = get(mRes)  as PersonalFinanceMonthResponse | null;
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

      const emailData  = get(emailRes) as { overallStats: EmailStats } | null;
      const rawAgenda  = get(agendaRes) as { startTime?: string; title?: string }[] | null;
      const agenda     = (rawAgenda ?? []).map(a => ({
        time:  a?.startTime ? new Date(a.startTime).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : '--:--',
        title: a?.title ?? 'Compromisso',
      }));

      setData({ funnel, curr: currMonth, prev: prevMonth1, trend, email: emailData?.overallStats ?? null, expenses, agenda });
      setUpdated(new Date());
    } catch (e) { console.error('[dash]', e); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { setMounted(true); load(); }, [load]);

  /* derived */
  const cs         = data?.curr?.summary;
  const income     = cs?.totalIncome    ?? 0;
  const expenses   = cs?.totalExpenses  ?? 0;
  const cashFlow   = cs?.remainingBalance ?? 0;
  const totalLeads = (data?.funnel.lead ?? 0) + (data?.funnel.contacted ?? 0) + (data?.funnel.qualified ?? 0) + (data?.funnel.negotiating ?? 0);
  const openRate   = data?.email?.openRate ?? 0;
  const prev       = data?.prev?.summary;
  const revMoM     = cs && prev && prev.totalIncome > 0 ? ((income - prev.totalIncome) / prev.totalIncome) * 100 : 0;
  const expMoM     = cs && prev && prev.totalExpenses > 0 ? ((expenses - prev.totalExpenses) / prev.totalExpenses) * 100 : 0;

  const trendIncome = data?.trend.map(t => t.income) ?? [];
  const trendExp    = data?.trend.map(t => t.expense) ?? [];
  const trendCfg    = mounted && data?.trend?.length ? trendConfig(data.trend)    : null;
  const funnelCfg   = mounted && data?.funnel        ? funnelConfig(data.funnel)   : null;
  const expCfg      = mounted && data?.expenses?.length ? expenseConfig(data.expenses) : null;

  const kpis = [
    { label: 'Receita do Mês',          value: income,    prefix: 'R$ ', suffix: '',  delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, spark: trendIncome,  color: '#00D4AA' },
    { label: 'Total Despesas',          value: expenses,  prefix: 'R$ ', suffix: '',  delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, spark: trendExp,     color: '#F59E0B' },
    { label: 'Leads no Funil',          value: totalLeads,prefix: '',    suffix: '',  spark: [totalLeads*.5, totalLeads*.7, totalLeads*.8, totalLeads*.9, totalLeads*.95, totalLeads], color: '#818CF8', up: true },
    { label: 'Taxa de Abertura Email',  value: openRate,  prefix: '',    suffix: '%', spark: [openRate*.6, openRate*.7, openRate*.8, openRate*.9, openRate*.95, openRate], color: '#F472B6', up: openRate >= 20 },
  ];

  const tabs = [{ id: 'overview', label: 'Visão Geral' }, { id: 'pipeline', label: 'Pipeline' }, { id: 'finance', label: 'Financeiro' }];

  return (
    <>
      <style>{CSS}</style>
      <motion.div
        className="dsh"
        variants={pageVariants}
        initial="hidden"
        animate="show"
        style={{ display: 'flex', flexDirection: 'column', gap: 18, paddingBottom: 48 }}
      >
        {/* ── Topbar ── */}
        <motion.div variants={sectionVariants} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <h1 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: '#f4f4f5' }}>Dashboard</h1>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 3 }}>
              <span className="live-dot" />
              <span style={{ fontSize: 12, color: '#52525b' }}>
                {updated ? `Atualizado ${updated.toLocaleTimeString('pt-BR')}` : 'Carregando…'}
              </span>
              <span style={{ color: '#3f3f46', fontSize: 12 }}>·</span>
              <span style={{ fontSize: 12, color: '#52525b' }}>
                {new Date().toLocaleDateString('pt-BR', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })}
              </span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <motion.button
              onClick={load}
              disabled={loading}
              className="btn-ghost"
              whileHover={{ scale: 1.04 }}
              whileTap={{ scale: 0.94 }}
              style={{ opacity: loading ? 0.5 : 1 }}
            >
              <motion.span animate={{ rotate: loading ? 360 : 0 }} transition={{ duration: 1, repeat: loading ? Infinity : 0, ease: 'linear' }}>
                <RefreshCw size={13} />
              </motion.span>
              Atualizar
            </motion.button>
            {tabs.map(t => (
              <motion.button
                key={t.id}
                onClick={() => setTab(t.id)}
                whileHover={{ scale: 1.04 }}
                whileTap={{ scale: 0.93 }}
                style={{ padding: '7px 14px', borderRadius: 10, border: 'none', fontSize: 12, fontWeight: 600, cursor: 'pointer', background: tab === t.id ? '#00D4AA' : 'rgba(255,255,255,0.05)', color: tab === t.id ? '#000' : '#71717a', transition: 'background 0.2s, color 0.2s' }}
              >
                {t.label}
              </motion.button>
            ))}
          </div>
        </motion.div>

        {/* ── Hero ── */}
        <HeroCard data={data} loading={loading} mounted={mounted} />

        {/* ── Command Center ── */}
        <CommandCenter data={data} loading={loading} />

        {/* ── KPI Row ── */}
        <motion.div
          className="g4"
          variants={stagger7}
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, margin: '-40px' }}
        >
          {kpis.map((k, i) => (
            <KpiCard key={k.label} {...k} loading={loading} idx={i} />
          ))}
        </motion.div>

        {/* ── Revenue Trend + Email ── */}
        <div className="g73">
          <motion.div
            variants={sectionVariants}
            initial="hidden"
            whileInView="show"
            viewport={{ once: true, margin: '-40px' }}
          >
            <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Receita vs Despesas</div>
                  <div className="sub" style={{ marginTop: 2 }}>Últimos 6 meses</div>
                </div>
                <div style={{ display: 'flex', gap: 8 }}>
                  <span className={`badge ${revMoM >= 0 ? 'badge-up' : 'badge-dn'}`}>{revMoM >= 0 ? '↗' : '↘'} {Math.abs(revMoM).toFixed(1)}%</span>
                  <Link href="/finance" className="cta-link" style={{ fontSize: 11 }}>Ver tudo <ArrowRight size={10} /></Link>
                </div>
              </div>
              {!mounted || loading || !trendCfg
                ? <Skel h={220} r={10} />
                : <ApexChart {...trendCfg} />
              }
            </motion.div>
          </motion.div>

          <motion.div
            variants={sectionVariants}
            initial="hidden"
            whileInView="show"
            viewport={{ once: true, margin: '-40px' }}
          >
            <motion.div className="card" whileHover={cardHover} whileTap={cardTap} style={{ display: 'flex', flexDirection: 'column', gap: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Email Marketing</div>
                  <div className="sub" style={{ marginTop: 2 }}>Últimos 30 dias</div>
                </div>
                <motion.span animate={{ rotate: [0, 12, -8, 0] }} transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}>
                  <Mail size={16} style={{ color: '#F472B6' }} />
                </motion.span>
              </div>
              {loading
                ? <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>{[0, 1, 2, 3].map(i => <Skel key={i} h={22} r={6} />)}</div>
                : data?.email
                  ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                      {[
                        { lbl: 'Enviados', val: data.email.totalEmailsSent.toLocaleString('pt-BR'), pct: 100, c: '#6366F1' },
                        { lbl: 'Abertos',  val: `${data.email.openRate.toFixed(1)}%`,              pct: data.email.openRate,       c: '#10B981' },
                        { lbl: 'Cliques',  val: `${data.email.clickRate.toFixed(1)}%`,             pct: data.email.clickRate * 3,  c: '#F59E0B' },
                      ].map((s, i) => (
                        <motion.div
                          key={s.lbl}
                          initial={{ opacity: 0, x: 16 }}
                          animate={{ opacity: 1, x: 0 }}
                          transition={{ delay: i * 0.1 + 0.2, duration: 0.4, ease: [0.16, 1, 0.3, 1] }}
                        >
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                            <span className="sub" style={{ fontSize: 12 }}>{s.lbl}</span>
                            <span style={{ fontSize: 13, fontWeight: 700, color: '#e4e4e7', fontFamily: 'monospace' }}>{s.val}</span>
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
                  )
                  : (
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: '#52525b', padding: '24px 0' }}>
                      <Mail size={28} />
                      <div className="sub">Sem dados de email</div>
                      <Link href="/email-marketing" className="cta-link">Criar campanha →</Link>
                    </div>
                  )
              }
            </motion.div>
          </motion.div>
        </div>

        {/* ── Funnel + Expenses ── */}
        <div className="g2">
          <motion.div variants={sectionVariants} initial="hidden" whileInView="show" viewport={{ once: true, margin: '-40px' }}>
            <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Funil de Vendas</div>
                  <div className="sub" style={{ marginTop: 2 }}>{totalLeads + (data?.funnel.customer ?? 0)} contatos · {data?.funnel.customer ?? 0} clientes</div>
                </div>
                <Link href="/leads" className="cta-link" style={{ fontSize: 11 }}>Ver leads <ArrowRight size={10} /></Link>
              </div>
              {!mounted || loading || !funnelCfg
                ? <Skel h={200} r={10} />
                : <ApexChart {...funnelCfg} />
              }
              {data && !loading && (
                <motion.div
                  style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 8, marginTop: 14, paddingTop: 14, borderTop: '1px solid rgba(255,255,255,0.06)' }}
                  variants={stagger5}
                  initial="hidden"
                  animate="show"
                >
                  {[
                    { lbl: 'Lead',    n: data.funnel.lead,        c: '#6366F1' },
                    { lbl: 'Contato', n: data.funnel.contacted,   c: '#8B5CF6' },
                    { lbl: 'Qualif.', n: data.funnel.qualified,   c: '#A78BFA' },
                    { lbl: 'Neg.',    n: data.funnel.negotiating, c: '#EC4899' },
                    { lbl: 'Cliente', n: data.funnel.customer,    c: '#00D4AA' },
                  ].map(s => (
                    <motion.div key={s.lbl} variants={popVariants} style={{ textAlign: 'center' }}>
                      <SpringMetric value={s.n} className="" style={{ fontSize: 18, fontWeight: 700, color: s.c, fontFamily: 'monospace' } as React.CSSProperties} stiffness={80} damping={16} />
                      <div className="label" style={{ fontSize: 9 }}>{s.lbl}</div>
                    </motion.div>
                  ))}
                </motion.div>
              )}
            </motion.div>
          </motion.div>

          <motion.div variants={sectionVariants} initial="hidden" whileInView="show" viewport={{ once: true, margin: '-40px' }}>
            <motion.div className="card" whileHover={cardHover} whileTap={cardTap}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Despesas por Categoria</div>
                  <div className="sub" style={{ marginTop: 2 }}>Mês atual — {S(expenses)} total</div>
                </div>
                {cashFlow < 0 && <span className="badge badge-dn">Fluxo negativo</span>}
              </div>
              {!mounted || loading
                ? <Skel h={195} r={10} />
                : expCfg && data?.expenses?.length
                  ? <ApexChart {...expCfg} />
                  : (
                    <div style={{ height: 195, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: '#52525b' }}>
                      <motion.div animate={{ y: [0, -6, 0] }} transition={{ duration: 2.4, repeat: Infinity, ease: 'easeInOut' }}>
                        <DollarSign size={28} />
                      </motion.div>
                      <div className="sub">Sem despesas cadastradas</div>
                      <Link href="/finance" className="cta-link">Registrar →</Link>
                    </div>
                  )
              }
              {data && !loading && (
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 14, paddingTop: 14, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
                  <div>
                    <div className="label" style={{ marginBottom: 4 }}>Saldo Líquido</div>
                    <SpringMetric value={Math.abs(cashFlow)} prefix="R$ " suffix={cashFlow < 0 ? ' ↓' : ''} className="metric-md" style={{ color: cashFlow >= 0 ? '#00D4AA' : '#EF4444' } as React.CSSProperties} />
                  </div>
                  <Link href="/finance" className="cta-link">Ver finanças <ArrowRight size={11} /></Link>
                </div>
              )}
            </motion.div>
          </motion.div>
        </div>

        {/* ── Pipeline Stages + Agenda ── */}
        <div className="g63">
          <motion.div variants={sectionVariants} initial="hidden" whileInView="show" viewport={{ once: true, margin: '-40px' }}>
            <motion.div className="card" whileHover={{ boxShadow: '0 16px 48px rgba(0,0,0,0.35)', borderColor: 'rgba(255,255,255,0.14)' }} transition={{ duration: 0.2 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Pipeline — Conversão por Etapa</div>
                  <div className="sub" style={{ marginTop: 2 }}>Taxa entre estágios</div>
                </div>
                <Link href="/leads" className="cta-link" style={{ fontSize: 11 }}>Ver todos <ArrowRight size={10} /></Link>
              </div>
              {loading
                ? <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 10 }}>{[0,1,2,3,4].map(i => <Skel key={i} h={100} r={12} />)}</div>
                : data && (
                  <motion.div
                    style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 10 }}
                    variants={stagger5}
                    initial="hidden"
                    animate="show"
                  >
                    {[
                      { lbl: 'Leads',        n: data.funnel.lead,        c: '#6366F1', next: data.funnel.contacted   },
                      { lbl: 'Em Contato',   n: data.funnel.contacted,   c: '#8B5CF6', next: data.funnel.qualified   },
                      { lbl: 'Qualificados', n: data.funnel.qualified,   c: '#A78BFA', next: data.funnel.negotiating },
                      { lbl: 'Negociando',   n: data.funnel.negotiating, c: '#EC4899', next: data.funnel.customer    },
                      { lbl: 'Clientes',     n: data.funnel.customer,    c: '#00D4AA', next: null                    },
                    ].map((s) => {
                      const conv = s.n > 0 && s.next !== null ? (s.next / s.n) * 100 : null;
                      return (
                        <motion.div
                          key={s.lbl}
                          className="stage-card"
                          variants={popVariants}
                          whileHover={{ y: -3, scale: 1.03, borderColor: `${s.c}55`, transition: { type: 'spring', stiffness: 380, damping: 22 } }}
                        >
                          <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 10 }}>
                            <motion.div
                              style={{ width: 8, height: 8, borderRadius: '50%', background: s.c }}
                              animate={{ scale: [1, 1.3, 1] }}
                              transition={{ duration: 2.5, repeat: Infinity, ease: 'easeInOut' }}
                            />
                            <span className="label" style={{ fontSize: 9 }}>{s.lbl}</span>
                          </div>
                          <SpringMetric value={s.n} className="" style={{ fontSize: 28, fontWeight: 800, color: s.c, fontFamily: 'monospace', lineHeight: 1 } as React.CSSProperties} stiffness={80} damping={16} />
                          {conv !== null ? (
                            <div style={{ marginTop: 10 }}>
                              <AnimatedBar pct={conv} color={s.c} h={3} />
                              <div style={{ fontSize: 10, fontWeight: 700, color: conv < 30 ? '#EF4444' : s.c, marginTop: 4 }}>{conv.toFixed(0)}% →</div>
                            </div>
                          ) : (
                            <motion.div
                              style={{ marginTop: 10, display: 'flex', alignItems: 'center', gap: 3 }}
                              animate={{ scale: [1, 1.08, 1] }}
                              transition={{ duration: 2, repeat: Infinity }}
                            >
                              <CheckCircle size={10} color="#00D4AA" />
                              <span style={{ fontSize: 10, color: '#00D4AA', fontWeight: 700 }}>Final</span>
                            </motion.div>
                          )}
                        </motion.div>
                      );
                    })}
                  </motion.div>
                )
              }
            </motion.div>
          </motion.div>

          <motion.div variants={sectionVariants} initial="hidden" whileInView="show" viewport={{ once: true, margin: '-40px' }}>
            <motion.div className="card" whileHover={cardHover} whileTap={cardTap} style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Agenda de Hoje</div>
                  <div className="sub" style={{ marginTop: 2 }}>{new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'short' })}</div>
                </div>
                <Link href="/agenda" style={{ display: 'inline-flex', alignItems: 'center', padding: '5px 8px', borderRadius: 8, background: 'rgba(255,255,255,0.06)', color: '#71717a', textDecoration: 'none' }}>
                  <Activity size={13} />
                </Link>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8, flex: 1 }}>
                <AnimatePresence>
                  {loading ? [0, 1, 2].map(i => <Skel key={i} h={60} r={10} />) :
                    data?.agenda?.length ? data.agenda.slice(0, 5).map((ev, i) => (
                      <motion.div
                        key={i}
                        initial={{ opacity: 0, x: 16, scale: 0.95 }}
                        animate={{ opacity: 1, x: 0, scale: 1 }}
                        transition={{ type: 'spring', stiffness: 300, damping: 24, delay: i * 0.08 }}
                        style={{ display: 'flex', gap: 10, padding: '10px 12px', borderRadius: 12, background: 'rgba(0,212,170,0.05)', border: '1px solid rgba(0,212,170,0.12)' }}
                      >
                        <div style={{ width: 3, borderRadius: 999, background: '#00D4AA', flexShrink: 0 }} />
                        <div>
                          <div className="sub" style={{ fontSize: 10, color: '#00D4AA', marginBottom: 2 }}>{ev.time}</div>
                          <div style={{ fontSize: 13, fontWeight: 600, color: '#e4e4e7' }}>{ev.title}</div>
                        </div>
                      </motion.div>
                    )) : (
                      <motion.div
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: '#52525b', padding: '20px 0' }}
                      >
                        <motion.div
                          animate={{ rotate: [0, 8, -8, 0], y: [0, -4, 0] }}
                          transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
                          style={{ fontSize: 28 }}
                        >
                          📅
                        </motion.div>
                        <div className="sub">Nenhum compromisso hoje</div>
                        <Link href="/agenda" className="cta-link">Agendar →</Link>
                      </motion.div>
                    )
                  }
                </AnimatePresence>
              </div>
            </motion.div>
          </motion.div>
        </div>

      </motion.div>
    </>
  );
}
