'use client';

import dynamic from 'next/dynamic';
import { useEffect, useRef, useState } from 'react';
import {
  Activity, AlertCircle, BarChart2, Bell, BookOpen, Bot,
  Briefcase, Calendar, ChevronRight, Circle, CreditCard,
  DollarSign, FileText, Flame, Globe, HelpCircle, Home,
  LayoutDashboard, Link2, ListChecks, LogOut, Mail, Megaphone,
  MessageSquare, Package, Phone, Plus, RefreshCw, Repeat,
  Search, Settings, Shield, Star, Tag, Target, TrendingUp,
  Upload, UserPlus, Users, Wallet, Zap
} from 'lucide-react';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

/* ── useCountUp hook ── */
function useCountUp(target: number, duration = 1400, decimals = 0) {
  const [val, setVal] = useState(0);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const obs = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return;
      obs.disconnect();
      const start = performance.now();
      const tick = (now: number) => {
        const p = Math.min((now - start) / duration, 1);
        const ease = 1 - Math.pow(1 - p, 3);
        setVal(parseFloat((ease * target).toFixed(decimals)));
        if (p < 1) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }, { threshold: 0.3 });
    obs.observe(el);
    return () => obs.disconnect();
  }, [target, duration, decimals]);
  return { val, ref };
}

/* ── AnimBar ── */
function AnimBar({ pct, color }: { pct: number; color: string }) {
  const [w, setW] = useState(0);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const obs = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return;
      obs.disconnect();
      setTimeout(() => setW(pct), 80);
    }, { threshold: 0.3 });
    obs.observe(el);
    return () => obs.disconnect();
  }, [pct]);
  return (
    <div ref={ref} style={{ height: 6, borderRadius: 999, background: 'rgba(255,255,255,0.08)', overflow: 'hidden' }}>
      <div style={{ height: '100%', width: `${w}%`, background: color, borderRadius: 999, transition: 'width 1s cubic-bezier(.22,1,.36,1)' }} />
    </div>
  );
}

/* ── Rv (reveal wrapper) ── */
function Rv({ children, delay = 0 }: { children: React.ReactNode; delay?: number }) {
  const [vis, setVis] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const obs = new IntersectionObserver(([e]) => {
      if (!e.isIntersecting) return;
      obs.disconnect();
      setTimeout(() => setVis(true), delay);
    }, { threshold: 0.1 });
    obs.observe(el);
    return () => obs.disconnect();
  }, [delay]);
  return (
    <div ref={ref} style={{ opacity: vis ? 1 : 0, transform: vis ? 'none' : 'translateY(18px)', transition: `opacity .5s ease ${delay}ms, transform .5s ease ${delay}ms` }}>
      {children}
    </div>
  );
}

/* ── Chart configs ── */
const sparkBase = (color: string, data: number[]) => ({
  type: 'area' as const,
  height: 52,
  series: [{ data }],
  options: {
    chart: { sparkline: { enabled: true }, toolbar: { show: false }, animations: { enabled: true, easing: 'easeinout', speed: 900 } },
    stroke: { curve: 'smooth' as const, width: 2 },
    fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0, stops: [0, 100] } },
    colors: [color],
    tooltip: { enabled: false },
  },
});

const sparkCfgs = [
  sparkBase('#6EE7B7', [18, 25, 22, 30, 28, 35, 32, 40, 38, 45, 42, 50]),
  sparkBase('#93C5FD', [12, 18, 15, 22, 19, 26, 23, 29, 27, 33, 31, 38]),
  sparkBase('#FCA5A5', [8, 11, 9, 14, 12, 17, 14, 19, 16, 22, 19, 25]),
  sparkBase('#FCD34D', [5, 8, 6, 10, 8, 13, 11, 15, 13, 18, 16, 21]),
];

const heroCfg = {
  type: 'line' as const,
  height: 220,
  series: [
    { name: 'Receita', type: 'area', data: [42, 55, 48, 62, 58, 71, 68, 82, 79, 91, 88, 98] },
    { name: 'Leads', type: 'column', data: [18, 24, 21, 29, 26, 33, 31, 38, 35, 43, 40, 47] },
    { name: 'Meta', type: 'line', data: [50, 50, 60, 60, 70, 70, 80, 80, 90, 90, 95, 95] },
  ],
  options: {
    chart: { toolbar: { show: false }, background: 'transparent', animations: { enabled: true, easing: 'easeinout', speed: 1000 } },
    colors: ['#10B981', '#6366F1', '#F59E0B'],
    stroke: { curve: 'smooth' as const, width: [2, 0, 2] },
    fill: { type: ['gradient', 'solid', 'none'], gradient: { shadeIntensity: 1, opacityFrom: 0.3, opacityTo: 0, stops: [0, 100] } },
    plotOptions: { bar: { columnWidth: '40%', borderRadius: 4 } },
    xaxis: { categories: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'], labels: { style: { colors: '#9CA3AF', fontSize: '11px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
    yaxis: { labels: { style: { colors: '#9CA3AF', fontSize: '11px' } } },
    grid: { borderColor: 'rgba(255,255,255,0.05)', strokeDashArray: 4 },
    legend: { labels: { colors: '#9CA3AF' }, position: 'top' as const },
    tooltip: { theme: 'dark' as const },
  },
};

const donutCfg = {
  type: 'donut' as const,
  height: 200,
  series: [42, 28, 18, 12],
  options: {
    chart: { background: 'transparent', toolbar: { show: false } },
    colors: ['#10B981', '#6366F1', '#F59E0B', '#EF4444'],
    labels: ['Clientes', 'Leads Quentes', 'Em Negociação', 'Perdidos'],
    legend: { show: false },
    dataLabels: { enabled: false },
    plotOptions: { pie: { donut: { size: '72%', labels: { show: true, total: { show: true, label: 'Total', color: '#9CA3AF', fontSize: '12px', formatter: () => '847' }, value: { color: '#F9FAFB', fontSize: '24px', fontWeight: 700 } } } } },
    stroke: { width: 0 },
    tooltip: { theme: 'dark' as const },
  },
};

const barsCfg = {
  type: 'bar' as const,
  height: 200,
  series: [{ name: 'Conversões', data: [88, 72, 65, 58, 51, 44, 38] }],
  options: {
    chart: { background: 'transparent', toolbar: { show: false } },
    plotOptions: { bar: { horizontal: true, borderRadius: 4, barHeight: '60%', distributed: true } },
    colors: ['#10B981', '#6366F1', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', '#14B8A6'],
    xaxis: { labels: { style: { colors: '#9CA3AF', fontSize: '11px' } }, axisBorder: { show: false } },
    yaxis: { categories: ['Email', 'WhatsApp', 'Google', 'Instagram', 'Indicação', 'Direto', 'LinkedIn'], labels: { style: { colors: '#9CA3AF', fontSize: '11px' } } },
    grid: { borderColor: 'rgba(255,255,255,0.05)', xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
    dataLabels: { enabled: false },
    tooltip: { theme: 'dark' as const },
    legend: { show: false },
  },
};

const heatCfg = {
  type: 'heatmap' as const,
  height: 160,
  series: [
    { name: 'Seg', data: [{ x: '8h', y: 3 }, { x: '10h', y: 7 }, { x: '12h', y: 5 }, { x: '14h', y: 9 }, { x: '16h', y: 6 }, { x: '18h', y: 4 }, { x: '20h', y: 2 }] },
    { name: 'Ter', data: [{ x: '8h', y: 5 }, { x: '10h', y: 9 }, { x: '12h', y: 8 }, { x: '14h', y: 12 }, { x: '16h', y: 10 }, { x: '18h', y: 7 }, { x: '20h', y: 3 }] },
    { name: 'Qua', data: [{ x: '8h', y: 4 }, { x: '10h', y: 8 }, { x: '12h', y: 6 }, { x: '14h', y: 10 }, { x: '16h', y: 8 }, { x: '18h', y: 5 }, { x: '20h', y: 2 }] },
    { name: 'Qui', data: [{ x: '8h', y: 6 }, { x: '10h', y: 11 }, { x: '12h', y: 9 }, { x: '14h', y: 14 }, { x: '16h', y: 11 }, { x: '18h', y: 8 }, { x: '20h', y: 4 }] },
    { name: 'Sex', data: [{ x: '8h', y: 7 }, { x: '10h', y: 13 }, { x: '12h', y: 11 }, { x: '14h', y: 15 }, { x: '16h', y: 12 }, { x: '18h', y: 9 }, { x: '20h', y: 5 }] },
  ],
  options: {
    chart: { background: 'transparent', toolbar: { show: false } },
    colors: ['#10B981'],
    dataLabels: { enabled: false },
    xaxis: { labels: { style: { colors: '#9CA3AF', fontSize: '10px' } }, axisBorder: { show: false }, axisTicks: { show: false } },
    yaxis: { labels: { style: { colors: '#9CA3AF', fontSize: '10px' } } },
    grid: { show: false },
    tooltip: { theme: 'dark' as const },
    plotOptions: { heatmap: { shadeIntensity: 0.8, radius: 2, useFillColorAsStroke: false, colorScale: { ranges: [{ from: 0, to: 4, color: '#1F2937' }, { from: 5, to: 9, color: '#065F46' }, { from: 10, to: 15, color: '#10B981' }] } } },
  },
};

const radialCfg = {
  type: 'radialBar' as const,
  height: 260,
  series: [87, 73, 91, 64],
  options: {
    chart: { background: 'transparent', toolbar: { show: false } },
    colors: ['#10B981', '#6366F1', '#F59E0B', '#EF4444'],
    plotOptions: { radialBar: { hollow: { size: '30%' }, track: { background: 'rgba(255,255,255,0.05)', margin: 5 }, dataLabels: { show: true, name: { show: true, color: '#9CA3AF', fontSize: '11px', offsetY: 2 }, value: { show: true, color: '#F9FAFB', fontSize: '13px', fontWeight: 700, offsetY: -2 } } } },
    labels: ['Conv.', 'Reten.', 'Sat.', 'LTV'],
    legend: { show: false },
    stroke: { lineCap: 'round' as const },
    tooltip: { theme: 'dark' as const },
  },
};

/* ── KPI data ── */
const kpis = [
  { label: 'Receita Total', value: 284750, prefix: 'R$', suffix: '', decimals: 0, delta: '+18.4%', up: true, spark: 0 },
  { label: 'Novos Leads', value: 847, prefix: '', suffix: '', decimals: 0, delta: '+12.1%', up: true, spark: 1 },
  { label: 'Taxa de Churn', value: 2.3, prefix: '', suffix: '%', decimals: 1, delta: '-0.5%', up: false, spark: 2 },
  { label: 'LTV Médio', value: 4280, prefix: 'R$', suffix: '', decimals: 0, delta: '+7.2%', up: true, spark: 3 },
];

const pipelineStages = [
  { label: 'Prospecção', count: 142, value: 'R$ 284k', pct: 100, color: '#6366F1' },
  { label: 'Qualificação', count: 89, value: 'R$ 178k', pct: 63, color: '#8B5CF6' },
  { label: 'Proposta', count: 54, value: 'R$ 108k', pct: 38, color: '#EC4899' },
  { label: 'Negociação', count: 31, value: 'R$ 62k', pct: 22, color: '#F59E0B' },
  { label: 'Fechamento', count: 18, value: 'R$ 36k', pct: 13, color: '#10B981' },
];

const aiInsights = [
  { icon: '🔥', text: 'Lead Ana Costa está 94% pronto para fechar — contate hoje', type: 'hot' },
  { icon: '⚠️', text: '12 leads sem contato há 7+ dias no estágio Proposta', type: 'warn' },
  { icon: '📈', text: 'Maio +23% vs Abril — acima da meta trimestral', type: 'up' },
  { icon: '🎯', text: 'WhatsApp convertendo 2.4x melhor que Email este mês', type: 'info' },
  { icon: '💰', text: 'Meta R$ 300k — faltam R$ 15.250 (5.1%)', type: 'goal' },
];

const deals = [
  { name: 'Ana Costa', company: 'Softworks', value: 'R$ 18.500', stage: 'Negociação', prob: 94, avatar: 'AC' },
  { name: 'Bruno Lima', company: 'FinTech SA', value: 'R$ 32.000', stage: 'Proposta', prob: 71, avatar: 'BL' },
  { name: 'Carla Dias', company: 'EduPlus', value: 'R$ 9.800', stage: 'Qualificação', prob: 58, avatar: 'CD' },
  { name: 'Diego Moura', company: 'LogiMax', value: 'R$ 24.600', stage: 'Fechamento', prob: 88, avatar: 'DM' },
  { name: 'Elena Torres', company: 'MedCare', value: 'R$ 41.200', stage: 'Proposta', prob: 65, avatar: 'ET' },
];

const agenda = [
  { time: '09:00', title: 'Demo — Softworks', type: 'call', color: '#10B981' },
  { time: '11:30', title: 'Proposta FinTech SA', type: 'meet', color: '#6366F1' },
  { time: '14:00', title: 'Review Pipeline Q2', type: 'internal', color: '#F59E0B' },
  { time: '16:30', title: 'Onboarding EduPlus', type: 'call', color: '#EC4899' },
];

const hotLeads = [
  { name: 'Fernanda Rocha', score: 97, company: 'RetailX', src: 'Instagram', avatar: 'FR' },
  { name: 'Gustavo Pires', score: 91, company: 'TechHub', src: 'LinkedIn', avatar: 'GP' },
  { name: 'Helena Matos', score: 88, company: 'AgriTech', src: 'Indicação', avatar: 'HM' },
  { name: 'Igor Santos', score: 85, company: 'BioLab', src: 'Google', avatar: 'IS' },
];

const activities = [
  { icon: '📧', text: 'Email enviado para Ana Costa', time: '2 min' },
  { icon: '📞', text: 'Ligação registrada — Bruno Lima', time: '15 min' },
  { icon: '💬', text: 'WhatsApp — Carla Dias respondeu', time: '38 min' },
  { icon: '🎉', text: 'Diego Moura convertido em cliente!', time: '1h' },
  { icon: '📝', text: 'Proposta enviada — Elena Torres', time: '2h' },
  { icon: '🔔', text: 'Lembrete: Follow-up Gustavo amanhã', time: '3h' },
];

const tasks = [
  { text: 'Enviar proposta para LogiMax', done: false, pri: 'high' },
  { text: 'Atualizar pipeline Q2', done: true, pri: 'med' },
  { text: 'Configurar automação WhatsApp', done: false, pri: 'high' },
  { text: 'Revisar campanhas de email', done: false, pri: 'low' },
  { text: 'Exportar relatório mensal', done: true, pri: 'med' },
];

type NavChild = { label: string; href: string; badge?: string };
type NavItem = { icon: React.ElementType; label: string; href?: string; children?: NavChild[]; badge?: string };
type NavGroup = { section: string; items: NavItem[] };

const navGroups: NavGroup[] = [
  {
    section: 'Principal',
    items: [
      { icon: LayoutDashboard, label: 'Dashboard', href: '/dashboard' },
    ],
  },
  {
    section: 'CRM',
    items: [
      { icon: Users, label: 'Clientes', href: '/customers/' },
      { icon: TrendingUp, label: 'Leads', children: [
        { label: 'Todos os Leads', href: '/leads/' },
        { label: 'Importar Leads', href: '/leads/import' },
      ]},
      { icon: HelpCircle, label: 'Helpdesk', href: '/helpdesk' },
    ],
  },
  {
    section: 'Marketing',
    items: [
      { icon: Mail, label: 'Outreach', href: '/outreach' },
      { icon: Megaphone, label: 'Email Marketing', children: [
        { label: 'Email Marketing', href: '/email-marketing' },
        { label: 'Email Marketing PRO', href: '/email-marketing/pro', badge: 'NEW' },
        { label: 'Campanhas', href: '/campanhas' },
      ]},
      { icon: Globe, label: 'Meta Ads', href: '/ads/' },
      { icon: BarChart2, label: 'Analytics', href: '/analytics' },
    ],
  },
  {
    section: 'Finanças',
    items: [
      { icon: Star, label: 'Morning Briefing', href: '/finance/morning-briefing' },
      { icon: LayoutDashboard, label: 'Dashboard Financeiro', href: '/finance' },
      { icon: Wallet, label: 'Planilha Financeira', href: '/finance/personal-control' },
      { icon: DollarSign, label: 'Transações', children: [
        { label: 'Todas as Transações', href: '/finance/transactions' },
        { label: 'Receitas', href: '/finance/incomes' },
        { label: 'Despesas', href: '/finance/expenses' },
        { label: 'Transferências', href: '/finance/transfers' },
        { label: 'Importar OFX/CSV', href: '/finance/imports' },
      ]},
      { icon: CreditCard, label: 'Cartões de Crédito', href: '/finance/credit-cards' },
      { icon: Briefcase, label: 'Contas', href: '/finance/accounts' },
      { icon: Target, label: 'Planejador', children: [
        { label: 'Planejador Financeiro', href: '/finance/planner' },
        { label: 'Metas Financeiras', href: '/finance/planner/goals' },
        { label: 'Recorrentes', href: '/finance/planner/recurring' },
      ]},
      { icon: FileText, label: 'Imposto de Renda', href: '/finance/tax-documents' },
    ],
  },
  {
    section: 'IA',
    items: [
      { icon: MessageSquare, label: 'Claude Chat', href: '/ai-chat/', badge: 'NEW' },
      { icon: Zap, label: 'Ferramentas IA', children: [
        { label: 'Geração de Imagens', href: '/utilities/image-generation' },
        { label: 'Gerador de Prompts', href: '/utilities/prompt-generator' },
        { label: 'Humanizar Texto', href: '/utilities/humanize-text' },
        { label: 'Otimizador de Email', href: '/utilities/email-subject-optimizer' },
        { label: 'Gerador de Personas', href: '/utilities/lead-persona-generator' },
        { label: 'Teste A/B Outreach', href: '/utilities/outreach-ab-test' },
        { label: 'Batch Social Media', href: '/utilities/social-batch-generator' },
        { label: 'Insights de Clientes', href: '/utilities/customer-insights' },
      ]},
    ],
  },
  {
    section: 'Pessoal',
    items: [
      { icon: Calendar, label: 'Agenda', href: '/agenda' },
      { icon: ListChecks, label: 'Tarefas', href: '/tasks' },
      { icon: Package, label: 'Listas e Compras', href: '/household/checklists' },
      { icon: Tag, label: 'Snippets', href: '/utilities/snippets' },
      { icon: Link2, label: 'Extratores', children: [
        { label: 'HTML → Texto', href: '/tools/html-extractor' },
        { label: 'HTML → Links', href: '/tools/html-url-extractor' },
      ]},
      { icon: Briefcase, label: 'Inventário de Apps', href: '/tools/apps-inventory' },
    ],
  },
  {
    section: 'Admin',
    items: [
      { icon: Users, label: 'Usuários', href: '/users/' },
      { icon: Shield, label: 'Grupos & Permissões', href: '/admin/groups' },
      { icon: Bot, label: 'Provedores IA', href: '/admin/ai' },
      { icon: FileText, label: 'Blog', children: [
        { label: 'Gerenciar Blog', href: '/admin/blog' },
        { label: 'Novo Post', href: '/admin/blog/new' },
      ]},
      { icon: Activity, label: 'Logs do Sistema', href: '/logs/' },
    ],
  },
];

const stageColors: Record<string, string> = {
  'Negociação': '#F59E0B',
  'Proposta': '#6366F1',
  'Qualificação': '#8B5CF6',
  'Fechamento': '#10B981',
};

/* ── KPI Card ── */
function KpiCard({ kpi, idx }: { kpi: typeof kpis[0]; idx: number }) {
  const { val, ref } = useCountUp(kpi.value, 1400, kpi.decimals);
  const display = `${kpi.prefix}${kpi.decimals > 0 ? val.toFixed(kpi.decimals) : Math.round(val).toLocaleString('pt-BR')}${kpi.suffix}`;
  return (
    <Rv delay={idx * 80}>
      <div style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)', borderRadius: 16, padding: '20px 22px', display: 'flex', flexDirection: 'column', gap: 12 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <div style={{ fontSize: 12, color: '#9CA3AF', marginBottom: 4, fontWeight: 500 }}>{kpi.label}</div>
            <div ref={ref} style={{ fontSize: 26, fontWeight: 700, color: '#F9FAFB', fontFamily: 'var(--font-mono)' }}>{display}</div>
          </div>
          <span style={{ fontSize: 12, fontWeight: 600, color: kpi.up ? '#10B981' : '#EF4444', background: kpi.up ? 'rgba(16,185,129,0.12)' : 'rgba(239,68,68,0.12)', padding: '3px 8px', borderRadius: 999 }}>{kpi.delta}</span>
        </div>
        <ApexChart {...sparkCfgs[kpi.spark]} />
      </div>
    </Rv>
  );
}

/* ── CSS string ── */
const CSS = `
  .db-overlay {
    position: fixed; inset: 0; z-index: 100;
    display: flex; background: #0F1A14;
    font-family: var(--font-jakarta, 'Plus Jakarta Sans', sans-serif);
    overflow: hidden;
  }
  .db-sidebar {
    width: 220px; flex-shrink: 0;
    background: #0B1510;
    border-right: 1px solid rgba(255,255,255,0.06);
    display: flex; flex-direction: column;
    overflow-y: auto; overflow-x: hidden;
  }
  .db-sidebar::-webkit-scrollbar { width: 4px; }
  .db-sidebar::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.1); border-radius: 999px; }
  .db-logo {
    padding: 22px 20px 14px;
    display: flex; align-items: center; gap: 10px;
    border-bottom: 1px solid rgba(255,255,255,0.06);
    flex-shrink: 0;
  }
  .db-logo-icon {
    width: 32px; height: 32px; border-radius: 10px;
    background: linear-gradient(135deg, #10B981, #059669);
    display: flex; align-items: center; justify-content: center;
    font-weight: 800; font-size: 14px; color: #fff;
  }
  .db-logo-text { font-size: 15px; font-weight: 700; color: #F9FAFB; }
  .db-logo-sub { font-size: 10px; color: #6B7280; }
  .db-nav { flex: 1; padding: 12px 10px; display: flex; flex-direction: column; gap: 2px; }
  .db-nav-item {
    display: flex; align-items: center; gap: 10px;
    padding: 9px 12px; border-radius: 10px;
    cursor: pointer; transition: background .15s;
    color: #6B7280; font-size: 13px; font-weight: 500;
    text-decoration: none;
  }
  .db-nav-item:hover { background: rgba(255,255,255,0.05); color: #D1D5DB; }
  .db-nav-item.active { background: rgba(16,185,129,0.15); color: #10B981; }
  .db-nav-section { font-size: 10px; font-weight: 600; color: #4B5563; text-transform: uppercase; letter-spacing: .08em; padding: 14px 12px 4px; }
  .db-main {
    flex: 1; display: flex; flex-direction: column; overflow: hidden;
  }
  .db-topbar {
    height: 58px; flex-shrink: 0;
    background: rgba(15,26,20,0.95); backdrop-filter: blur(12px);
    border-bottom: 1px solid rgba(255,255,255,0.06);
    display: flex; align-items: center; padding: 0 24px; gap: 16px;
  }
  .db-search {
    flex: 1; max-width: 340px;
    display: flex; align-items: center; gap: 8px;
    background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.08);
    border-radius: 10px; padding: 8px 14px;
    color: #6B7280; font-size: 13px;
  }
  .db-content {
    flex: 1; overflow-y: auto; padding: 22px 24px 40px;
    display: flex; flex-direction: column; gap: 20px;
  }
  .db-content::-webkit-scrollbar { width: 6px; }
  .db-content::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.08); border-radius: 999px; }
  .db-page-header {
    display: flex; align-items: center; justify-content: space-between;
  }
  .db-page-title { font-size: 22px; font-weight: 700; color: #F9FAFB; }
  .db-page-sub { font-size: 13px; color: #6B7280; margin-top: 2px; }
  .db-btn {
    display: flex; align-items: center; gap: 6px;
    padding: 8px 16px; border-radius: 10px; font-size: 13px; font-weight: 600;
    cursor: pointer; border: none; transition: all .15s;
  }
  .db-btn-primary { background: #10B981; color: #fff; }
  .db-btn-primary:hover { background: #059669; }
  .db-btn-ghost { background: rgba(255,255,255,0.06); color: #D1D5DB; }
  .db-btn-ghost:hover { background: rgba(255,255,255,0.1); }
  .db-grid-4 { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; }
  .db-grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; }
  .db-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
  .db-grid-73 { display: grid; grid-template-columns: 1fr 320px; gap: 16px; }
  .db-grid-63 { display: grid; grid-template-columns: 1fr 300px; gap: 16px; }
  .db-card {
    background: rgba(255,255,255,0.04);
    border: 1px solid rgba(255,255,255,0.08);
    border-radius: 16px; padding: 20px;
  }
  .db-card-header {
    display: flex; align-items: center; justify-content: space-between;
    margin-bottom: 16px;
  }
  .db-card-title { font-size: 14px; font-weight: 600; color: #F9FAFB; }
  .db-card-sub { font-size: 11px; color: #6B7280; margin-top: 2px; }
  .db-badge {
    font-size: 10px; font-weight: 600; padding: 3px 8px; border-radius: 999px;
  }
  .db-badge-green { background: rgba(16,185,129,0.15); color: #10B981; }
  .db-badge-blue { background: rgba(99,102,241,0.15); color: #818CF8; }
  .db-badge-yellow { background: rgba(245,158,11,0.15); color: #FBBF24; }
  .db-badge-red { background: rgba(239,68,68,0.15); color: #F87171; }
  .db-avatar {
    width: 32px; height: 32px; border-radius: 50%;
    background: linear-gradient(135deg, #10B981, #6366F1);
    display: flex; align-items: center; justify-content: center;
    font-size: 11px; font-weight: 700; color: #fff; flex-shrink: 0;
  }
  .db-table { width: 100%; border-collapse: collapse; }
  .db-table th { font-size: 11px; font-weight: 600; color: #6B7280; text-align: left; padding: 8px 12px; border-bottom: 1px solid rgba(255,255,255,0.06); }
  .db-table td { font-size: 13px; color: #D1D5DB; padding: 10px 12px; border-bottom: 1px solid rgba(255,255,255,0.04); }
  .db-table tr:last-child td { border-bottom: none; }
  .db-table tr:hover td { background: rgba(255,255,255,0.02); }
  .db-insight-item {
    display: flex; align-items: flex-start; gap: 10px;
    padding: 10px; border-radius: 10px; margin-bottom: 8px;
    background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.06);
    font-size: 12px; color: #D1D5DB; line-height: 1.5;
  }
  .db-activity-item {
    display: flex; align-items: center; gap: 10px;
    padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.04);
    font-size: 12px; color: #D1D5DB;
  }
  .db-activity-item:last-child { border-bottom: none; }
  .db-task-item {
    display: flex; align-items: center; gap: 10px;
    padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,0.04);
    font-size: 12px;
  }
  .db-task-item:last-child { border-bottom: none; }
  .db-score-bar {
    display: flex; align-items: center; gap: 8px;
  }
  @keyframes db-pulse { 0%,100% { opacity:1; } 50% { opacity:.5; } }
  .db-live { animation: db-pulse 2s infinite; }
  .db-nav-group { position: relative; }
  .db-nav-flyout {
    position: fixed;
    left: 220px;
    width: 210px;
    background: #0D1F18;
    border: 1px solid rgba(255,255,255,0.12);
    border-radius: 10px;
    padding: 6px;
    z-index: 300;
    box-shadow: 0 8px 32px rgba(0,0,0,.6);
  }
  .db-nav-flyout-item {
    display: block;
    padding: 7px 10px;
    border-radius: 7px;
    font-size: 12px;
    color: #9CA3AF;
    cursor: pointer;
    text-decoration: none;
    transition: background .1s, color .1s;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
  .db-nav-flyout-item:hover { background: rgba(255,255,255,0.07); color: #F9FAFB; }
  .db-nav-badge {
    font-size: 9px; font-weight: 700; padding: 1px 5px; border-radius: 4px;
    background: #10B981; color: #fff; margin-left: 4px; vertical-align: middle;
  }
  @media (max-width: 1200px) {
    .db-grid-4 { grid-template-columns: repeat(2, 1fr); }
    .db-grid-73, .db-grid-63 { grid-template-columns: 1fr; }
  }
`;

function NavItemEl({ item, activeNav, setActiveNav }: { item: NavItem; activeNav: string; setActiveNav: (s: string) => void }) {
  const [flyoutOpen, setFlyoutOpen] = useState(false);
  const [flyoutTop, setFlyoutTop] = useState(0);
  const ref = useRef<HTMLDivElement>(null);
  const isActive = activeNav === item.label;
  const hasChildren = item.children && item.children.length > 0;

  const handleMouseEnter = () => {
    if (hasChildren && ref.current) {
      const rect = ref.current.getBoundingClientRect();
      setFlyoutTop(rect.top);
      setFlyoutOpen(true);
    }
  };
  const handleMouseLeave = () => setFlyoutOpen(false);

  return (
    <div ref={ref} className="db-nav-group" onMouseEnter={handleMouseEnter} onMouseLeave={handleMouseLeave}>
      {hasChildren ? (
        <div
          className={`db-nav-item ${isActive ? 'active' : ''}`}
          onClick={() => setActiveNav(item.label)}
          style={{ justifyContent: 'space-between' }}
        >
          <span style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <item.icon size={15} />
            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', maxWidth: 120 }}>{item.label}</span>
          </span>
          <ChevronRight size={12} style={{ flexShrink: 0, opacity: .5 }} />
        </div>
      ) : (
        <a
          href={item.href}
          className={`db-nav-item ${isActive ? 'active' : ''}`}
          onClick={() => setActiveNav(item.label)}
        >
          <item.icon size={15} />
          <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1 }}>{item.label}</span>
          {item.badge && <span className="db-nav-badge">{item.badge}</span>}
        </a>
      )}
      {flyoutOpen && hasChildren && (
        <div className="db-nav-flyout" style={{ top: flyoutTop }}>
          {item.children!.map(child => (
            <a key={child.href} href={child.href} className="db-nav-flyout-item">
              {child.label}
              {child.badge && <span className="db-nav-badge">{child.badge}</span>}
            </a>
          ))}
        </div>
      )}
    </div>
  );
}

export default function DashboardPage() {
  const [activeNav, setActiveNav] = useState('Dashboard');
  const [tab, setTab] = useState('visao-geral');
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    return () => setMounted(false);
  }, []);

  return (
    <>
      <style>{CSS}</style>
      <div className="db-overlay">
        {/* ── Sidebar ── */}
        <nav className="db-sidebar">
          <div className="db-logo">
            <div className="db-logo-icon">D</div>
            <div>
              <div className="db-logo-text">DIAX CRM</div>
              <div className="db-logo-sub">v5 · Pro</div>
            </div>
          </div>

          <div className="db-nav">
            {navGroups.map(group => (
              <div key={group.section}>
                <div className="db-nav-section">{group.section}</div>
                {group.items.map(item => (
                  <NavItemEl key={item.label} item={item} activeNav={activeNav} setActiveNav={setActiveNav} />
                ))}
              </div>
            ))}
          </div>

          {/* User block */}
          <div style={{ padding: '12px 16px', borderTop: '1px solid rgba(255,255,255,0.06)', display: 'flex', alignItems: 'center', gap: 10 }}>
            <div className="db-avatar" style={{ width: 34, height: 34 }}>AQ</div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: '#F9FAFB', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>Alexandre Q.</div>
              <div style={{ fontSize: 10, color: '#6B7280' }}>Admin</div>
            </div>
            <div className="db-live" style={{ width: 7, height: 7, borderRadius: '50%', background: '#10B981', flexShrink: 0 }} />
          </div>
        </nav>

        {/* ── Main ── */}
        <div className="db-main">
          {/* Topbar */}
          <div className="db-topbar">
            <div className="db-search">
              <Search size={14} />
              <span>Buscar leads, contatos, negócios…</span>
              <span style={{ marginLeft: 'auto', fontSize: 11, opacity: .5 }}>⌘K</span>
            </div>
            <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 12 }}>
              <button className="db-btn db-btn-primary" style={{ padding: '7px 14px' }}>
                <Plus size={14} /> Novo Lead
              </button>
              <div style={{ position: 'relative', cursor: 'pointer' }}>
                <Bell size={17} color="#9CA3AF" />
                <div style={{ position: 'absolute', top: -3, right: -3, width: 7, height: 7, borderRadius: '50%', background: '#EF4444' }} />
              </div>
              <HelpCircle size={17} color="#9CA3AF" style={{ cursor: 'pointer' }} />
            </div>
          </div>

          {/* Content */}
          <div className="db-content">

            {/* Page header */}
            <div className="db-page-header">
              <div>
                <div className="db-page-title">Dashboard Executivo</div>
                <div className="db-page-sub">Maio 2026 · Atualizado agora há pouco</div>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                {['visao-geral', 'pipeline', 'financeiro', 'marketing'].map(t => (
                  <button key={t} onClick={() => setTab(t)} style={{ padding: '6px 14px', borderRadius: 8, border: 'none', fontSize: 12, fontWeight: 600, cursor: 'pointer', background: tab === t ? '#10B981' : 'rgba(255,255,255,0.06)', color: tab === t ? '#fff' : '#9CA3AF', transition: 'all .15s' }}>
                    {t === 'visao-geral' ? 'Visão Geral' : t === 'pipeline' ? 'Pipeline' : t === 'financeiro' ? 'Financeiro' : 'Marketing'}
                  </button>
                ))}
              </div>
            </div>

            {/* KPI row */}
            {mounted && (
              <div className="db-grid-4">
                {kpis.map((kpi, i) => <KpiCard key={kpi.label} kpi={kpi} idx={i} />)}
              </div>
            )}

            {/* Hero chart + AI insights */}
            <div className="db-grid-73">
              <Rv>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Desempenho Geral</div>
                      <div className="db-card-sub">Receita · Leads · Meta — 12 meses</div>
                    </div>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <span className="db-badge db-badge-green">Acima da meta</span>
                      <button className="db-btn db-btn-ghost" style={{ padding: '4px 10px', fontSize: 11 }}>Ver relatório</button>
                    </div>
                  </div>
                  {mounted && <ApexChart {...heroCfg} />}
                </div>
              </Rv>

              <Rv delay={120}>
                <div className="db-card" style={{ display: 'flex', flexDirection: 'column' }}>
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">IA · Insights</div>
                      <div className="db-card-sub">Gerado agora</div>
                    </div>
                    <span style={{ fontSize: 18 }}>🧠</span>
                  </div>
                  <div style={{ flex: 1 }}>
                    {aiInsights.map((ins, i) => (
                      <Rv key={i} delay={i * 60}>
                        <div className="db-insight-item">
                          <span style={{ fontSize: 16, flexShrink: 0 }}>{ins.icon}</span>
                          <span>{ins.text}</span>
                        </div>
                      </Rv>
                    ))}
                  </div>
                </div>
              </Rv>
            </div>

            {/* Pipeline + Donut */}
            <div className="db-grid-73">
              <Rv>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Pipeline de Vendas</div>
                      <div className="db-card-sub">334 oportunidades · R$ 668k</div>
                    </div>
                    <span className="db-badge db-badge-blue">Funil ativo</span>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                    {pipelineStages.map((stage, i) => (
                      <Rv key={stage.label} delay={i * 60}>
                        <div>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6 }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                              <div style={{ width: 8, height: 8, borderRadius: '50%', background: stage.color }} />
                              <span style={{ fontSize: 13, color: '#D1D5DB' }}>{stage.label}</span>
                            </div>
                            <div style={{ display: 'flex', gap: 16 }}>
                              <span style={{ fontSize: 12, color: '#9CA3AF' }}>{stage.count} leads</span>
                              <span style={{ fontSize: 12, fontWeight: 600, color: '#F9FAFB', fontFamily: 'var(--font-mono)' }}>{stage.value}</span>
                            </div>
                          </div>
                          <AnimBar pct={stage.pct} color={stage.color} />
                        </div>
                      </Rv>
                    ))}
                  </div>
                </div>
              </Rv>

              <Rv delay={120}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Distribuição CRM</div>
                      <div className="db-card-sub">847 registros ativos</div>
                    </div>
                  </div>
                  {mounted && <ApexChart {...donutCfg} />}
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
                    {['Clientes', 'Leads Quentes', 'Em Negociação', 'Perdidos'].map((l, i) => {
                      const cs = ['#10B981', '#6366F1', '#F59E0B', '#EF4444'];
                      const vs = [42, 28, 18, 12];
                      return (
                        <div key={l} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: 12 }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
                            <div style={{ width: 8, height: 8, borderRadius: '50%', background: cs[i] }} />
                            <span style={{ color: '#9CA3AF' }}>{l}</span>
                          </div>
                          <span style={{ fontWeight: 600, color: '#F9FAFB' }}>{vs[i]}%</span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </Rv>
            </div>

            {/* Deals table + Agenda */}
            <div className="db-grid-73">
              <Rv>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Top Oportunidades</div>
                      <div className="db-card-sub">Ordenado por probabilidade</div>
                    </div>
                    <button className="db-btn db-btn-ghost" style={{ padding: '5px 12px', fontSize: 11 }}>Ver todas</button>
                  </div>
                  <table className="db-table">
                    <thead>
                      <tr>
                        <th>Contato</th>
                        <th>Empresa</th>
                        <th>Valor</th>
                        <th>Estágio</th>
                        <th>Prob.</th>
                      </tr>
                    </thead>
                    <tbody>
                      {deals.map(deal => (
                        <tr key={deal.name}>
                          <td>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                              <div className="db-avatar">{deal.avatar}</div>
                              {deal.name}
                            </div>
                          </td>
                          <td style={{ color: '#9CA3AF' }}>{deal.company}</td>
                          <td style={{ fontFamily: 'var(--font-mono)', fontWeight: 600, color: '#10B981' }}>{deal.value}</td>
                          <td>
                            <span style={{ fontSize: 11, fontWeight: 600, padding: '3px 8px', borderRadius: 999, background: `${stageColors[deal.stage] ?? '#6B7280'}22`, color: stageColors[deal.stage] ?? '#9CA3AF' }}>
                              {deal.stage}
                            </span>
                          </td>
                          <td>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                              <div style={{ flex: 1, height: 4, background: 'rgba(255,255,255,0.08)', borderRadius: 999, overflow: 'hidden' }}>
                                <div style={{ height: '100%', width: `${deal.prob}%`, background: deal.prob >= 80 ? '#10B981' : deal.prob >= 60 ? '#F59E0B' : '#6366F1', borderRadius: 999 }} />
                              </div>
                              <span style={{ fontSize: 11, color: '#9CA3AF', width: 28, textAlign: 'right' }}>{deal.prob}%</span>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Rv>

              <Rv delay={120}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Agenda de Hoje</div>
                      <div className="db-card-sub">26 de Maio, 2026</div>
                    </div>
                    <button className="db-btn db-btn-ghost" style={{ padding: '5px 10px', fontSize: 11 }}><Plus size={12} /></button>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                    {agenda.map(ev => (
                      <div key={ev.title} style={{ display: 'flex', gap: 12, padding: '10px 12px', borderRadius: 10, background: 'rgba(255,255,255,0.03)', border: `1px solid ${ev.color}22` }}>
                        <div style={{ width: 3, borderRadius: 999, background: ev.color, flexShrink: 0 }} />
                        <div>
                          <div style={{ fontSize: 12, color: '#9CA3AF', marginBottom: 2 }}>{ev.time}</div>
                          <div style={{ fontSize: 13, fontWeight: 600, color: '#F9FAFB' }}>{ev.title}</div>
                          <div style={{ fontSize: 11, color: ev.color, marginTop: 2 }}>{ev.type === 'call' ? '📞 Ligação' : ev.type === 'meet' ? '🤝 Reunião' : '🏠 Interno'}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </Rv>
            </div>

            {/* Bars + Heatmap + Hot Leads */}
            <div className="db-grid-3">
              <Rv>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Conversão por Canal</div>
                      <div className="db-card-sub">% de leads convertidos</div>
                    </div>
                  </div>
                  {mounted && <ApexChart {...barsCfg} />}
                </div>
              </Rv>

              <Rv delay={80}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Mapa de Atividade</div>
                      <div className="db-card-sub">Contatos por hora da semana</div>
                    </div>
                  </div>
                  {mounted && <ApexChart {...heatCfg} />}
                </div>
              </Rv>

              <Rv delay={160}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Leads Quentes 🔥</div>
                      <div className="db-card-sub">Score IA em tempo real</div>
                    </div>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
                    {hotLeads.map(lead => (
                      <div key={lead.name} style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                        <div className="db-avatar">{lead.avatar}</div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ fontSize: 13, fontWeight: 600, color: '#F9FAFB', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{lead.name}</div>
                          <div style={{ fontSize: 11, color: '#9CA3AF' }}>{lead.company} · {lead.src}</div>
                          <div style={{ marginTop: 4 }}>
                            <AnimBar pct={lead.score} color={lead.score >= 90 ? '#10B981' : '#F59E0B'} />
                          </div>
                        </div>
                        <div style={{ fontSize: 13, fontWeight: 700, color: lead.score >= 90 ? '#10B981' : '#F59E0B', fontFamily: 'var(--font-mono)' }}>{lead.score}</div>
                      </div>
                    ))}
                  </div>
                </div>
              </Rv>
            </div>

            {/* Radial + Sources + Activity */}
            <div className="db-grid-3">
              <Rv>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Métricas de Saúde</div>
                      <div className="db-card-sub">Conv. · Retenção · Sat. · LTV</div>
                    </div>
                  </div>
                  {mounted && <ApexChart {...radialCfg} />}
                </div>
              </Rv>

              <Rv delay={80}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Feed de Atividade</div>
                      <div className="db-card-sub">Últimas 4 horas</div>
                    </div>
                    <div className="db-live" style={{ width: 7, height: 7, borderRadius: '50%', background: '#10B981' }} />
                  </div>
                  <div>
                    {activities.map((act, i) => (
                      <div key={i} className="db-activity-item">
                        <span style={{ fontSize: 15, flexShrink: 0 }}>{act.icon}</span>
                        <span style={{ flex: 1 }}>{act.text}</span>
                        <span style={{ color: '#4B5563', fontSize: 11, flexShrink: 0 }}>{act.time}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </Rv>

              <Rv delay={160}>
                <div className="db-card">
                  <div className="db-card-header">
                    <div>
                      <div className="db-card-title">Tarefas do Dia</div>
                      <div className="db-card-sub">3 pendentes · 2 concluídas</div>
                    </div>
                    <span className="db-badge db-badge-yellow">3 pendentes</span>
                  </div>
                  <div>
                    {tasks.map((task, i) => (
                      <div key={i} className="db-task-item">
                        <div style={{ width: 16, height: 16, borderRadius: 4, border: task.done ? 'none' : '1.5px solid #4B5563', background: task.done ? '#10B981' : 'transparent', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                          {task.done && <span style={{ fontSize: 10, color: '#fff' }}>✓</span>}
                        </div>
                        <span style={{ flex: 1, color: task.done ? '#4B5563' : '#D1D5DB', textDecoration: task.done ? 'line-through' : 'none' }}>{task.text}</span>
                        <span style={{ fontSize: 10, fontWeight: 600, padding: '2px 6px', borderRadius: 999, background: task.pri === 'high' ? 'rgba(239,68,68,0.15)' : task.pri === 'med' ? 'rgba(245,158,11,0.15)' : 'rgba(107,114,128,0.15)', color: task.pri === 'high' ? '#F87171' : task.pri === 'med' ? '#FBBF24' : '#9CA3AF' }}>
                          {task.pri === 'high' ? 'Alta' : task.pri === 'med' ? 'Média' : 'Baixa'}
                        </span>
                      </div>
                    ))}
                  </div>
                  <button className="db-btn db-btn-ghost" style={{ width: '100%', justifyContent: 'center', marginTop: 14, fontSize: 12 }}>
                    <Plus size={13} /> Nova tarefa
                  </button>
                </div>
              </Rv>
            </div>

          </div>
        </div>
      </div>
    </>
  );
}
