'use client';

import dynamic from 'next/dynamic';
import Link from 'next/link';
import { useEffect, useRef, useState, useCallback } from 'react';
import {
  ArrowUpRight, ArrowDownRight, ArrowRight, RefreshCw,
  TrendingUp, TrendingDown, Zap, Target, AlertTriangle,
  CheckCircle, DollarSign, Users, Mail, Activity
} from 'lucide-react';
import { financeService, PersonalFinanceMonthResponse } from '@/services/finance';
import { getLeads, CustomerStatus as LeadStatus } from '@/services/leads';
import { agendaService } from '@/services/agenda';
import { apiFetch } from '@/services/api';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

/* ── utils ────────────────────────────────────────────── */
function prevMonthOf(y: number, m: number, offset: number): [number, number] {
  let mo = m - offset, yr = y;
  while (mo <= 0) { mo += 12; yr--; }
  return [yr, mo];
}
const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });
const S = (v: number) => Math.abs(v) >= 1_000_000 ? `R$ ${(v / 1_000_000).toFixed(1)}M` : Math.abs(v) >= 1_000 ? `R$ ${(v / 1_000).toFixed(1)}k` : R(v);
const MONTHS = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];

/* ── hooks ────────────────────────────────────────────── */
function useCountUp(target: number, duration = 1000, dec = 0) {
  const [val, setVal] = useState(0);
  const divRef = useRef<HTMLElement>(null);
  useEffect(() => {
    const el = divRef.current; if (!el || target === 0) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      const t0 = performance.now();
      const tick = (now: number) => {
        const p = Math.min((now - t0) / duration, 1);
        const ease = 1 - Math.pow(1 - p, 4);
        setVal(parseFloat((ease * target).toFixed(dec)));
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }, { threshold: 0.1 });
    io.observe(el);
    return () => io.disconnect();
  }, [target, duration, dec]);
  return { val, divRef };
}

function useReveal(delay = 0) {
  const [vis, setVis] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current; if (!el) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      setTimeout(() => setVis(true), delay);
    }, { threshold: 0.05 });
    io.observe(el);
    return () => io.disconnect();
  }, [delay]);
  return { vis, ref };
}

/* ── primitives ───────────────────────────────────────── */
function Reveal({ children, delay = 0, style = {} }: { children: React.ReactNode; delay?: number; style?: React.CSSProperties }) {
  const { vis, ref } = useReveal(delay);
  return (
    <div ref={ref} style={{ opacity: vis ? 1 : 0, transform: vis ? 'none' : 'translateY(20px)', transition: `opacity .5s cubic-bezier(.22,1,.36,1) ${delay}ms, transform .5s cubic-bezier(.22,1,.36,1) ${delay}ms`, ...style }}>
      {children}
    </div>
  );
}

function Bar({ pct, color, h = 4 }: { pct: number; color: string; h?: number }) {
  const [w, setW] = useState(0);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current; if (!el) return;
    const io = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return; io.disconnect();
      setTimeout(() => setW(Math.min(pct, 100)), 80);
    }, { threshold: 0.2 });
    io.observe(el);
    return () => io.disconnect();
  }, [pct]);
  return (
    <div ref={ref} style={{ height: h, background: 'rgba(255,255,255,0.07)', borderRadius: 999, overflow: 'hidden' }}>
      <div style={{ height: '100%', width: `${w}%`, background: color, borderRadius: 999, transition: 'width 1.1s cubic-bezier(.22,1,.36,1)' }} />
    </div>
  );
}

function Skel({ h = 20, r = 8, w = '100%' }: { h?: number; r?: number; w?: string | number }) {
  return <div style={{ height: h, borderRadius: r, width: w, background: 'rgba(255,255,255,0.07)', animation: 'pulse 1.5s ease infinite' }} />;
}

/* ── types ─────────────────────────────────────────────── */
interface EmailStats { totalCampaigns: number; totalEmailsSent: number; openRate: number; clickRate: number; totalOpened: number; }
interface Funnel { lead: number; contacted: number; qualified: number; negotiating: number; customer: number; }
interface MonthPoint { label: string; income: number; expense: number; }
interface CatExpense { name: string; total: number; pct: number; }
interface DashData {
  funnel: Funnel;
  curr: PersonalFinanceMonthResponse | null;
  prev: PersonalFinanceMonthResponse | null;
  trend: MonthPoint[];
  email: EmailStats | null;
  expenses: CatExpense[];
  agenda: { time: string; title: string }[];
}

/* ── ApexCharts configs ─────────────────────────────────── */
function sparkConfig(color: string, data: number[]) {
  return {
    type: 'area' as const, height: 56,
    series: [{ data: data.length ? data : [0] }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, animations: { enabled: true, speed: 900 } },
      stroke: { curve: 'smooth' as const, width: 2 },
      fill: { type: 'gradient', gradient: { opacityFrom: 0.3, opacityTo: 0 } },
      colors: [color], tooltip: { enabled: false },
    },
  };
}

function heroChartConfig(trend: MonthPoint[]) {
  return {
    type: 'area' as const, height: 120,
    series: [{ name: 'Receita', data: trend.map(t => Math.round(t.income)) }],
    options: {
      chart: { sparkline: { enabled: true }, toolbar: { show: false }, background: 'transparent', animations: { enabled: true, easing: 'easeinout', speed: 1200 } },
      stroke: { curve: 'smooth' as const, width: 3 },
      fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0, stops: [0, 100] } },
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
      chart: { toolbar: { show: false }, background: 'transparent', animations: { enabled: true, speed: 900 } },
      colors: ['#00D4AA', '#F59E0B'],
      stroke: { curve: 'smooth' as const, width: [2, 2] },
      fill: { type: ['gradient', 'none'], gradient: { opacityFrom: 0.2, opacityTo: 0 } },
      xaxis: { categories: trend.map(t => t.label), labels: { style: { colors: '#52525b', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { labels: { style: { colors: '#52525b', fontSize: '11px' }, formatter: (v: number) => `R$${(v / 1000).toFixed(0)}k` } },
      grid: { borderColor: 'rgba(255,255,255,0.04)', strokeDashArray: 4 },
      legend: { labels: { colors: '#71717a' }, position: 'top' as const },
      tooltip: { theme: 'dark' as const },
      markers: { size: 0 },
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
    type: 'bar' as const, height: 190,
    series: [{ name: 'Total', data: stages.map(s => s.v) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: { enabled: true, speed: 900 } },
      plotOptions: { bar: { horizontal: true, barHeight: '58%', borderRadius: 6, distributed: true } },
      colors: ['#6366F1', '#8B5CF6', '#A78BFA', '#EC4899', '#00D4AA'],
      xaxis: { labels: { style: { colors: '#52525b', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
      yaxis: { categories: stages.map(s => s.name), labels: { style: { colors: '#a1a1aa', fontSize: '12px' } } },
      grid: { borderColor: 'rgba(255,255,255,0.04)', xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      dataLabels: { enabled: true, style: { colors: ['#fff'], fontSize: '11px', fontWeight: 600 }, formatter: (v: number) => v > 0 ? String(v) : '' },
      legend: { show: false },
      tooltip: { theme: 'dark' as const },
    },
  };
}

function expenseConfig(cats: CatExpense[]) {
  return {
    type: 'bar' as const, height: 190,
    series: [{ name: 'Valor', data: cats.map(c => Math.round(c.total)) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, animations: { enabled: true, speed: 800 } },
      plotOptions: { bar: { horizontal: true, barHeight: '52%', borderRadius: 5, distributed: true } },
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

/* ── CSS ─────────────────────────────────────────────────── */
const CSS = `
  @keyframes pulse { 0%,100%{opacity:.5} 50%{opacity:1} }
  @keyframes spin { to{transform:rotate(360deg)} }
  @keyframes live { 0%,100%{transform:scale(1);opacity:1} 50%{transform:scale(1.5);opacity:.6} }
  @keyframes glow { 0%,100%{box-shadow:0 0 12px #00D4AA44} 50%{box-shadow:0 0 24px #00D4AA88} }

  .dsh { font-family: -apple-system, BlinkMacSystemFont, 'Inter', sans-serif; }
  .card {
    background: rgba(255,255,255,0.035);
    border: 1px solid rgba(255,255,255,0.08);
    border-radius: 20px;
    padding: 24px;
    transition: border-color .2s, box-shadow .2s, transform .2s;
  }
  .card:hover {
    border-color: rgba(255,255,255,0.14);
    box-shadow: 0 16px 48px rgba(0,0,0,0.35);
    transform: translateY(-1px);
  }
  .card-sm { border-radius: 14px; padding: 18px; }
  .hero-card {
    background: linear-gradient(135deg, rgba(0,212,170,0.08) 0%, rgba(99,102,241,0.06) 50%, rgba(0,0,0,0) 100%);
    border: 1px solid rgba(0,212,170,0.18);
    border-radius: 24px;
    padding: 32px 36px;
    position: relative;
    overflow: hidden;
    transition: border-color .2s, box-shadow .2s;
  }
  .hero-card:hover { border-color: rgba(0,212,170,0.3); box-shadow: 0 24px 64px rgba(0,212,170,0.1); }
  .hero-card::before {
    content:''; position:absolute; inset:0;
    background: radial-gradient(ellipse at 80% 50%, rgba(0,212,170,0.06) 0%, transparent 65%);
    pointer-events: none;
  }
  .metric-huge { font-size: 54px; font-weight: 800; color: #f4f4f5; letter-spacing: -0.04em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; line-height: 1; }
  .metric-lg { font-size: 30px; font-weight: 700; color: #f4f4f5; letter-spacing: -0.03em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; }
  .metric-md { font-size: 22px; font-weight: 700; color: #f4f4f5; letter-spacing: -0.02em; font-family: 'SF Mono', 'Fira Code', ui-monospace, monospace; }
  .label { font-size: 11px; font-weight: 600; color: #71717a; text-transform: uppercase; letter-spacing: 0.08em; }
  .sub { font-size: 13px; color: #71717a; }
  .teal { color: #00D4AA; }
  .green { color: #10B981; }
  .amber { color: #F59E0B; }
  .red { color: #EF4444; }
  .indigo { color: #818CF8; }
  .pink { color: #F472B6; }
  .txt { color: #e4e4e7; }
  .txt2 { color: #a1a1aa; }
  .txt3 { color: #71717a; }
  .badge { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
  .badge-up { background: rgba(16,185,129,0.15); color: #10B981; }
  .badge-dn { background: rgba(239,68,68,0.15); color: #EF4444; }
  .badge-warn { background: rgba(245,158,11,0.15); color: #F59E0B; }
  .badge-neu { background: rgba(113,113,122,0.15); color: #a1a1aa; }
  .badge-teal { background: rgba(0,212,170,0.15); color: #00D4AA; }
  .live-dot { width: 7px; height: 7px; border-radius: 50%; background: #00D4AA; animation: live 2s infinite; display: inline-block; }
  .divider { border: none; border-top: 1px solid rgba(255,255,255,0.06); margin: 0; }
  .g4 { display: grid; grid-template-columns: repeat(4,1fr); gap: 14px; }
  .g3 { display: grid; grid-template-columns: repeat(3,1fr); gap: 14px; }
  .g2 { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
  .g73 { display: grid; grid-template-columns: 1fr 310px; gap: 14px; }
  .g63 { display: grid; grid-template-columns: 1fr 280px; gap: 14px; }
  .g5cmd { display: grid; grid-template-columns: repeat(5,1fr); gap: 10px; }
  .cta-link { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; font-weight: 600; color: #00D4AA; text-decoration: none; }
  .cta-link:hover { text-decoration: underline; }
  .stage-card {
    background: rgba(255,255,255,0.03);
    border: 1px solid rgba(255,255,255,0.07);
    border-radius: 14px; padding: 16px;
    transition: border-color .15s, box-shadow .15s;
  }
  .stage-card:hover { border-color: rgba(255,255,255,0.14); box-shadow: 0 8px 24px rgba(0,0,0,0.2); }
  .btn-ghost { display: flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 10px; border: none; font-size: 12px; font-weight: 600; cursor: pointer; background: rgba(255,255,255,0.06); color: #a1a1aa; transition: all .15s; }
  .btn-ghost:hover { background: rgba(255,255,255,0.1); color: #e4e4e7; }
  .btn-primary { background: #00D4AA; color: #000; }
  .btn-primary:hover { background: #00bfa0; }

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

/* ── KPI Card ─────────────────────────────────────────────── */
function KpiCard({ label, value, prefix = '', suffix = '', delta, up, spark, color, loading, idx }: {
  label: string; value: number; prefix?: string; suffix?: string;
  delta?: string; up?: boolean; spark: number[]; color: string; loading: boolean; idx: number;
}) {
  const { val, divRef } = useCountUp(value, 1000, suffix === '%' ? 1 : 0);
  const display = `${prefix}${suffix === '%' ? val.toFixed(1) : Math.round(val).toLocaleString('pt-BR')}${suffix}`;
  return (
    <Reveal delay={idx * 60}>
      <div className="card card-sm" style={{ height: '100%' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12 }}>
          <span className="label">{label}</span>
          {delta && <span className={`badge ${up ? 'badge-up' : 'badge-dn'}`}>{up ? <ArrowUpRight size={10} /> : <ArrowDownRight size={10} />}{delta}</span>}
        </div>
        {loading ? <Skel h={38} r={6} /> : <span ref={divRef as React.RefObject<HTMLSpanElement>} className="metric-lg">{display}</span>}
        <div style={{ marginTop: 12 }}>
          {loading ? <Skel h={56} r={6} /> : <ApexChart {...sparkConfig(color, spark)} />}
        </div>
      </div>
    </Reveal>
  );
}

/* ── Command Center ─────────────────────────────────────────── */
function CommandCenter({ data, loading }: { data: DashData | null; loading: boolean }) {
  if (loading || !data) return (
    <div className="g5cmd">{[0, 1, 2, 3, 4].map(i => <Skel key={i} h={84} r={14} />)}</div>
  );
  const { funnel, curr, prev, email } = data;
  const cs = curr?.summary, ps = prev?.summary;
  const revMoM = cs && ps && ps.totalIncome > 0 ? ((cs.totalIncome - ps.totalIncome) / ps.totalIncome) * 100 : 0;
  const totalPipeline = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;
  const worstDrop = [
    { lbl: 'Lead→Contato', v: funnel.lead > 0 ? (1 - funnel.contacted / funnel.lead) * 100 : 0 },
    { lbl: 'Contato→Qualif.', v: funnel.contacted > 0 ? (1 - funnel.qualified / funnel.contacted) * 100 : 0 },
    { lbl: 'Qualif.→Neg.', v: funnel.qualified > 0 ? (1 - funnel.negotiating / funnel.qualified) * 100 : 0 },
  ].reduce((a, b) => a.v > b.v ? a : b);
  const convRate = (totalPipeline + funnel.customer) > 0 ? (funnel.customer / (totalPipeline + funnel.customer)) * 100 : 0;
  const topExpense = data.expenses[0];
  const cashFlow = cs?.remainingBalance ?? 0;

  const items = [
    {
      icon: <CheckCircle size={15} />,
      q: 'O que funciona?',
      answer: revMoM > 0 ? `Receita +${revMoM.toFixed(1)}% vs mês anterior` : revMoM === 0 ? 'Receita estável' : `Receita ${revMoM.toFixed(1)}% vs mês ant.`,
      c: revMoM >= 0 ? '#00D4AA' : '#F59E0B',
      href: '/finance',
    },
    {
      icon: <AlertTriangle size={15} />,
      q: 'O que está quebrado?',
      answer: totalPipeline > 0 ? `${worstDrop.v.toFixed(0)}% drop em ${worstDrop.lbl}` : 'Pipeline vazio — prospecte!',
      c: worstDrop.v > 50 ? '#EF4444' : '#F59E0B',
      href: '/leads',
    },
    {
      icon: <DollarSign size={15} />,
      q: 'Onde perde dinheiro?',
      answer: cashFlow < 0 ? `Fluxo negativo: ${S(cashFlow)}` : topExpense ? `${topExpense.name}: ${topExpense.pct.toFixed(0)}% das despesas` : 'Verifique despesas',
      c: cashFlow < 0 ? '#EF4444' : '#F59E0B',
      href: '/finance',
    },
    {
      icon: <Zap size={15} />,
      q: 'O que fazer agora?',
      answer: totalPipeline > 0 ? `${totalPipeline} leads aguardam ação` : funnel.customer > 0 ? `${funnel.customer} clientes ativos` : 'Adicione leads',
      c: '#818CF8',
      href: '/leads',
    },
    {
      icon: <Target size={15} />,
      q: 'Qual ação gera receita?',
      answer: email && email.openRate > 0 ? `Email: ${email.openRate.toFixed(1)}% abertura — ${email.totalEmailsSent.toLocaleString()} enviados` : convRate > 0 ? `Conversão atual: ${convRate.toFixed(1)}%` : 'Analise canais',
      c: '#00D4AA',
      href: email ? '/analytics' : '/leads',
    },
  ];

  return (
    <Reveal>
      <div className="g5cmd">
        {items.map((item, i) => (
          <Link key={i} href={item.href} style={{ textDecoration: 'none' }}>
            <div style={{ background: `${item.c}12`, border: `1px solid ${item.c}30`, borderRadius: 14, padding: '14px 16px', cursor: 'pointer', transition: 'all .18s' }}
              onMouseEnter={e => { const d = e.currentTarget as HTMLElement; d.style.border = `1px solid ${item.c}66`; d.style.transform = 'translateY(-2px)'; d.style.boxShadow = `0 8px 24px ${item.c}18`; }}
              onMouseLeave={e => { const d = e.currentTarget as HTMLElement; d.style.border = `1px solid ${item.c}30`; d.style.transform = ''; d.style.boxShadow = ''; }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
                <span style={{ color: item.c }}>{item.icon}</span>
                <span style={{ fontSize: 10, fontWeight: 700, color: item.c, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{item.q}</span>
              </div>
              <div style={{ fontSize: 12, color: '#d4d4d8', lineHeight: 1.45, fontWeight: 500 }}>{item.answer}</div>
              <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 3, fontSize: 10, color: item.c, fontWeight: 600 }}>Ver detalhes <ArrowRight size={9} /></div>
            </div>
          </Link>
        ))}
      </div>
    </Reveal>
  );
}

/* ── Hero Card ─────────────────────────────────────────────── */
function HeroCard({ data, loading, mounted }: { data: DashData | null; loading: boolean; mounted: boolean }) {
  const income = data?.curr?.summary?.totalIncome ?? 0;
  const prev = data?.prev?.summary?.totalIncome ?? 0;
  const mom = prev > 0 ? ((income - prev) / prev) * 100 : 0;
  const cashFlow = data?.curr?.summary?.remainingBalance ?? 0;
  const totalExpenses = data?.curr?.summary?.totalExpenses ?? 0;
  const { val: heroVal, divRef: heroRef } = useCountUp(income, 1200, 0);
  const GOAL = 50000; // fallback — ideally from /financial-goals endpoint
  const goalPct = Math.min((income / GOAL) * 100, 100);
  const heroCfg = mounted && data?.trend?.length ? heroChartConfig(data.trend) : null;

  return (
    <Reveal>
      <div className="hero-card">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 32, alignItems: 'center' }}>
          {/* Left: big metric */}
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8 }}>
              <span className="label" style={{ color: '#52525b' }}>Receita do Mês</span>
              <span className="live-dot" style={{ marginLeft: 4 }} />
              <span style={{ fontSize: 10, color: '#52525b', fontWeight: 600 }}>AO VIVO</span>
            </div>
            {loading
              ? <Skel h={62} r={8} w={260} />
              : <div ref={heroRef as React.RefObject<HTMLDivElement>} className="metric-huge teal">
                  R${' '}{Math.round(heroVal).toLocaleString('pt-BR')}
                </div>
            }
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 12, flexWrap: 'wrap' }}>
              {!loading && (
                <span className={`badge ${mom >= 0 ? 'badge-up' : 'badge-dn'}`}>
                  {mom >= 0 ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
                  {Math.abs(mom).toFixed(1)}% vs mês ant.
                </span>
              )}
              {!loading && cashFlow >= 0 && (
                <span className="badge badge-teal"><DollarSign size={10} /> Fluxo: {S(cashFlow)}</span>
              )}
              {!loading && cashFlow < 0 && (
                <span className="badge badge-dn">Fluxo: {S(cashFlow)}</span>
              )}
            </div>
            {/* Goal progress */}
            <div style={{ marginTop: 20 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <span className="sub" style={{ fontSize: 12 }}>Progresso à meta mensal</span>
                {!loading && <span style={{ fontSize: 12, fontWeight: 700, color: goalPct >= 80 ? '#00D4AA' : goalPct >= 60 ? '#F59E0B' : '#EF4444' }}>{goalPct.toFixed(0)}%</span>}
              </div>
              {loading ? <Skel h={6} r={999} /> : <Bar pct={goalPct} color={goalPct >= 80 ? '#00D4AA' : goalPct >= 60 ? '#F59E0B' : '#EF4444'} h={6} />}
            </div>
          </div>

          {/* Right: mini trend chart + stats */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
              <span className="label">Tendência — 6 meses</span>
              <Link href="/finance" className="cta-link">Detalhes <ArrowRight size={11} /></Link>
            </div>
            {loading || !heroCfg ? <Skel h={120} r={8} /> : <ApexChart {...heroCfg} />}
            <hr className="divider" style={{ margin: '14px 0' }} />
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              {loading ? [0, 1].map(i => <Skel key={i} h={40} r={8} />) : [
                { lbl: 'Despesas', val: totalExpenses, c: '#F59E0B' },
                { lbl: 'Saldo', val: cashFlow, c: cashFlow >= 0 ? '#00D4AA' : '#EF4444' },
              ].map(m => (
                <div key={m.lbl}>
                  <div className="label" style={{ marginBottom: 4 }}>{m.lbl}</div>
                  <div style={{ fontSize: 18, fontWeight: 700, color: m.c, fontFamily: 'monospace' }}>{S(Math.abs(m.val))}{m.val < 0 ? ' ↓' : ''}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </Reveal>
  );
}

/* ── MAIN ───────────────────────────────────────────────────── */
export default function DashboardPage() {
  const [mounted, setMounted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<DashData | null>(null);
  const [tab, setTab] = useState('overview');
  const [updated, setUpdated] = useState<Date | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const now = new Date();
      const yr = now.getFullYear(), mo = now.getMonth() + 1;
      const [py, pm] = prevMonthOf(yr, mo, 1);
      const months6: [number, number][] = Array.from({ length: 6 }, (_, i) => prevMonthOf(yr, mo, 5 - i));

      const [mRes, p1Res, m6_0, m6_1, m6_2, m6_3, m6_4, m6_5] = await Promise.allSettled([
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

      const currMonth = get(mRes) as PersonalFinanceMonthResponse | null;
      const prevMonth = get(p1Res) as PersonalFinanceMonthResponse | null;

      const monthsArr = [m6_0, m6_1, m6_2, m6_3, m6_4, m6_5].map(r => get(r) as PersonalFinanceMonthResponse | null);
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

      setData({ funnel, curr: currMonth, prev: prevMonth, trend, email: emailData?.overallStats ?? null, expenses, agenda });
      setUpdated(new Date());
    } catch (e) { console.error('[dash]', e); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { setMounted(true); load(); }, [load]);

  /* derived */
  const cs = data?.curr?.summary;
  const income = cs?.totalIncome ?? 0;
  const expenses = cs?.totalExpenses ?? 0;
  const cashFlow = cs?.remainingBalance ?? 0;
  const totalLeads = (data?.funnel.lead ?? 0) + (data?.funnel.contacted ?? 0) + (data?.funnel.qualified ?? 0) + (data?.funnel.negotiating ?? 0);
  const openRate = data?.email?.openRate ?? 0;
  const prev = data?.prev?.summary;
  const revMoM = cs && prev && prev.totalIncome > 0 ? ((income - prev.totalIncome) / prev.totalIncome) * 100 : 0;
  const expMoM = cs && prev && prev.totalExpenses > 0 ? ((expenses - prev.totalExpenses) / prev.totalExpenses) * 100 : 0;

  const trendIncome = data?.trend.map(t => t.income) ?? [];
  const trendExp = data?.trend.map(t => t.expense) ?? [];
  const trendCfg = mounted && data?.trend?.length ? trendConfig(data.trend) : null;
  const funnelCfg = mounted && data?.funnel ? funnelConfig(data.funnel) : null;
  const expCfg = mounted && data?.expenses?.length ? expenseConfig(data.expenses) : null;

  const kpis = [
    { label: 'Receita do Mês', value: income, prefix: 'R$ ', suffix: '', delta: revMoM !== 0 ? `${revMoM > 0 ? '+' : ''}${revMoM.toFixed(1)}%` : undefined, up: revMoM >= 0, spark: trendIncome, color: '#00D4AA' },
    { label: 'Total Despesas', value: expenses, prefix: 'R$ ', suffix: '', delta: expMoM !== 0 ? `${expMoM > 0 ? '+' : ''}${expMoM.toFixed(1)}%` : undefined, up: expMoM <= 0, spark: trendExp, color: '#F59E0B' },
    { label: 'Leads no Funil', value: totalLeads, prefix: '', suffix: '', spark: [totalLeads * 0.5, totalLeads * 0.7, totalLeads * 0.8, totalLeads * 0.9, totalLeads * 0.95, totalLeads], color: '#818CF8', up: true },
    { label: 'Taxa de Abertura Email', value: openRate, prefix: '', suffix: '%', spark: [openRate * 0.6, openRate * 0.7, openRate * 0.8, openRate * 0.9, openRate * 0.95, openRate], color: '#F472B6', up: openRate >= 20 },
  ];

  const tabs = [
    { id: 'overview', label: 'Visão Geral' },
    { id: 'pipeline', label: 'Pipeline' },
    { id: 'finance', label: 'Financeiro' },
  ];

  return (
    <>
      <style>{CSS}</style>
      <div className="dsh" style={{ display: 'flex', flexDirection: 'column', gap: 18, paddingBottom: 48 }}>

        {/* ── Topbar ── */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <h1 style={{ margin: 0, fontSize: 20, fontWeight: 700, color: '#f4f4f5' }}>Dashboard</h1>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 3 }}>
              <span className="live-dot" />
              <span style={{ fontSize: 12, color: '#52525b' }}>
                {updated ? `Atualizado ${updated.toLocaleTimeString('pt-BR')}` : 'Carregando…'}
              </span>
              <span style={{ color: '#3f3f46', fontSize: 12 }}>·</span>
              <span style={{ fontSize: 12, color: '#52525b' }}>{new Date().toLocaleDateString('pt-BR', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })}</span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
            <button onClick={load} disabled={loading} className="btn-ghost" style={{ opacity: loading ? 0.5 : 1 }}>
              <RefreshCw size={13} style={{ animation: loading ? 'spin 1s linear infinite' : 'none' }} />
              Atualizar
            </button>
            {tabs.map(t => (
              <button key={t.id} onClick={() => setTab(t.id)}
                style={{ padding: '7px 14px', borderRadius: 10, border: 'none', fontSize: 12, fontWeight: 600, cursor: 'pointer', transition: 'all .15s', background: tab === t.id ? '#00D4AA' : 'rgba(255,255,255,0.05)', color: tab === t.id ? '#000' : '#71717a' }}>
                {t.label}
              </button>
            ))}
          </div>
        </div>

        {/* ── Hero ── */}
        <HeroCard data={data} loading={loading} mounted={mounted} />

        {/* ── Command Center — 5 Questions ── */}
        <CommandCenter data={data} loading={loading} />

        {/* ── KPI Row ── */}
        <div className="g4">
          {kpis.map((k, i) => (
            <KpiCard key={k.label} label={k.label} value={k.value} prefix={k.prefix} suffix={k.suffix}
              delta={k.delta} up={k.up} spark={k.spark} color={k.color} loading={loading} idx={i} />
          ))}
        </div>

        {/* ── Revenue Trend + Email ── */}
        <div className="g73">
          <Reveal>
            <div className="card">
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
            </div>
          </Reveal>

          <Reveal delay={80}>
            <div className="card" style={{ display: 'flex', flexDirection: 'column', gap: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Email Marketing</div>
                  <div className="sub" style={{ marginTop: 2 }}>Últimos 30 dias</div>
                </div>
                <Mail size={16} style={{ color: '#F472B6' }} />
              </div>
              {loading
                ? <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>{[0, 1, 2, 3].map(i => <Skel key={i} h={22} r={6} />)}</div>
                : data?.email
                  ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                      {[
                        { lbl: 'Enviados', val: data.email.totalEmailsSent.toLocaleString('pt-BR'), pct: 100, c: '#6366F1' },
                        { lbl: 'Abertos', val: `${data.email.openRate.toFixed(1)}%`, pct: data.email.openRate, c: '#10B981' },
                        { lbl: 'Cliques', val: `${data.email.clickRate.toFixed(1)}%`, pct: data.email.clickRate * 3, c: '#F59E0B' },
                      ].map(s => (
                        <div key={s.lbl}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                            <span className="sub" style={{ fontSize: 12 }}>{s.lbl}</span>
                            <span style={{ fontSize: 13, fontWeight: 700, color: '#e4e4e7', fontFamily: 'monospace' }}>{s.val}</span>
                          </div>
                          <Bar pct={s.pct > 100 ? 100 : s.pct} color={s.c} h={3} />
                        </div>
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
            </div>
          </Reveal>
        </div>

        {/* ── Funnel + Expenses ── */}
        <div className="g2">
          <Reveal>
            <div className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Funil de Vendas</div>
                  <div className="sub" style={{ marginTop: 2 }}>{totalLeads + (data?.funnel.customer ?? 0)} contatos · {data?.funnel.customer ?? 0} clientes</div>
                </div>
                <Link href="/leads" className="cta-link" style={{ fontSize: 11 }}>Ver leads <ArrowRight size={10} /></Link>
              </div>
              {!mounted || loading || !funnelCfg
                ? <Skel h={190} r={10} />
                : <ApexChart {...funnelCfg} />
              }
              {data && !loading && (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 8, marginTop: 14, paddingTop: 14, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
                  {[
                    { lbl: 'Lead', n: data.funnel.lead, c: '#6366F1' },
                    { lbl: 'Contato', n: data.funnel.contacted, c: '#8B5CF6' },
                    { lbl: 'Qualif.', n: data.funnel.qualified, c: '#A78BFA' },
                    { lbl: 'Neg.', n: data.funnel.negotiating, c: '#EC4899' },
                    { lbl: 'Cliente', n: data.funnel.customer, c: '#00D4AA' },
                  ].map(s => (
                    <div key={s.lbl} style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: 18, fontWeight: 700, color: s.c, fontFamily: 'monospace' }}>{s.n}</div>
                      <div className="label" style={{ fontSize: 9 }}>{s.lbl}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </Reveal>

          <Reveal delay={80}>
            <div className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 18 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Despesas por Categoria</div>
                  <div className="sub" style={{ marginTop: 2 }}>Mês atual — {S(expenses)} total</div>
                </div>
                {cashFlow < 0 && <span className="badge badge-dn">Fluxo negativo</span>}
              </div>
              {!mounted || loading
                ? <Skel h={190} r={10} />
                : expCfg && data?.expenses?.length
                  ? <ApexChart {...expCfg} />
                  : (
                    <div style={{ height: 190, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: '#52525b' }}>
                      <DollarSign size={28} />
                      <div className="sub">Sem despesas cadastradas</div>
                      <Link href="/finance" className="cta-link">Registrar →</Link>
                    </div>
                  )
              }
              {data && !loading && (
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 14, paddingTop: 14, borderTop: '1px solid rgba(255,255,255,0.06)' }}>
                  <div>
                    <div className="label" style={{ marginBottom: 4 }}>Saldo Líquido</div>
                    <div className="metric-md" style={{ color: cashFlow >= 0 ? '#00D4AA' : '#EF4444' }}>{S(cashFlow)}</div>
                  </div>
                  <Link href="/finance" className="cta-link">Ver finanças <ArrowRight size={11} /></Link>
                </div>
              )}
            </div>
          </Reveal>
        </div>

        {/* ── Pipeline Stages + Agenda ── */}
        <div className="g63">
          <Reveal>
            <div className="card">
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
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 10 }}>
                    {[
                      { lbl: 'Leads', n: data.funnel.lead, c: '#6366F1', next: data.funnel.contacted },
                      { lbl: 'Em Contato', n: data.funnel.contacted, c: '#8B5CF6', next: data.funnel.qualified },
                      { lbl: 'Qualificados', n: data.funnel.qualified, c: '#A78BFA', next: data.funnel.negotiating },
                      { lbl: 'Negociando', n: data.funnel.negotiating, c: '#EC4899', next: data.funnel.customer },
                      { lbl: 'Clientes', n: data.funnel.customer, c: '#00D4AA', next: null },
                    ].map((s, i) => {
                      const conv = s.n > 0 && s.next !== null ? (s.next / s.n) * 100 : null;
                      return (
                        <Reveal key={s.lbl} delay={i * 40}>
                          <div className="stage-card">
                            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 10 }}>
                              <div style={{ width: 8, height: 8, borderRadius: '50%', background: s.c }} />
                              <span className="label" style={{ fontSize: 9 }}>{s.lbl}</span>
                            </div>
                            <div style={{ fontSize: 28, fontWeight: 800, color: s.c, fontFamily: 'monospace', lineHeight: 1 }}>{s.n}</div>
                            {conv !== null ? (
                              <div style={{ marginTop: 10 }}>
                                <Bar pct={conv} color={s.c} h={3} />
                                <div style={{ fontSize: 10, fontWeight: 700, color: conv < 30 ? '#EF4444' : s.c, marginTop: 4 }}>{conv.toFixed(0)}% →</div>
                              </div>
                            ) : (
                              <div style={{ marginTop: 10, display: 'flex', alignItems: 'center', gap: 3 }}>
                                <CheckCircle size={10} color="#00D4AA" />
                                <span style={{ fontSize: 10, color: '#00D4AA', fontWeight: 700 }}>Final</span>
                              </div>
                            )}
                          </div>
                        </Reveal>
                      );
                    })}
                  </div>
                )
              }
            </div>
          </Reveal>

          <Reveal delay={100}>
            <div className="card" style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <div>
                  <div style={{ fontSize: 14, fontWeight: 600, color: '#e4e4e7' }}>Agenda de Hoje</div>
                  <div className="sub" style={{ marginTop: 2 }}>{new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'short' })}</div>
                </div>
                <Link href="/agenda" style={{ display: 'inline-flex', alignItems: 'center', padding: '5px 8px', borderRadius: 8, background: 'rgba(255,255,255,0.06)', color: '#71717a', textDecoration: 'none', transition: 'all .15s' }}>
                  <Activity size={13} />
                </Link>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 8, flex: 1 }}>
                {loading ? [0, 1, 2].map(i => <Skel key={i} h={60} r={10} />) :
                  data?.agenda?.length ? data.agenda.slice(0, 5).map((ev, i) => (
                    <Reveal key={i} delay={i * 50}>
                      <div style={{ display: 'flex', gap: 10, padding: '10px 12px', borderRadius: 12, background: 'rgba(0,212,170,0.05)', border: '1px solid rgba(0,212,170,0.12)' }}>
                        <div style={{ width: 3, borderRadius: 999, background: '#00D4AA', flexShrink: 0 }} />
                        <div>
                          <div className="sub" style={{ fontSize: 10, color: '#00D4AA', marginBottom: 2 }}>{ev.time}</div>
                          <div style={{ fontSize: 13, fontWeight: 600, color: '#e4e4e7' }}>{ev.title}</div>
                        </div>
                      </div>
                    </Reveal>
                  )) : (
                    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 8, color: '#52525b', padding: '20px 0' }}>
                      <div style={{ fontSize: 28 }}>📅</div>
                      <div className="sub">Nenhum compromisso hoje</div>
                      <Link href="/agenda" className="cta-link">Agendar →</Link>
                    </div>
                  )
                }
              </div>
            </div>
          </Reveal>
        </div>

      </div>
    </>
  );
}
