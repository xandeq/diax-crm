'use client';

import { useEffect, useState } from 'react';
import dynamic from 'next/dynamic';
import {
  Activity,
  AlertCircle,
  BarChart3,
  Chrome,
  Clock,
  ExternalLink,
  Globe,
  Loader2,
  Monitor,
  RefreshCw,
  Smartphone,
  Tablet,
  TrendingDown,
  TrendingUp,
  Users,
} from 'lucide-react';
import { toast } from 'sonner';
import {
  getGa4Report,
  getGa4Status,
  type Ga4Report,
} from '@/services/googleAnalytics';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

const PERIOD_OPTIONS = [
  { label: '7 dias', value: 7 },
  { label: '30 dias', value: 30 },
  { label: '90 dias', value: 90 },
  { label: '6 meses', value: 180 },
  { label: '1 ano', value: 365 },
];

const CHANNEL_COLORS: Record<string, string> = {
  'Organic Search': '#10B981',
  'Direct': '#6EE7B7',
  'Social': '#3B82F6',
  'Referral': '#A78BFA',
  'Email': '#F59E0B',
  'Paid Search': '#F97316',
  'Display': '#EC4899',
  'Organic Social': '#06B6D4',
};

function channelColor(channel: string) {
  return CHANNEL_COLORS[channel] ?? '#6B7280';
}

function formatDuration(seconds: number) {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return m > 0 ? `${m}m ${s}s` : `${s}s`;
}

function deviceIcon(device: string) {
  switch (device.toLowerCase()) {
    case 'desktop': return <Monitor className="w-4 h-4" />;
    case 'mobile': return <Smartphone className="w-4 h-4" />;
    case 'tablet': return <Tablet className="w-4 h-4" />;
    default: return <Globe className="w-4 h-4" />;
  }
}

// ── KPI card ─────────────────────────────────────────────────────────────────

function KpiCard({
  icon: Icon,
  label,
  value,
  sub,
  color,
}: {
  icon: React.ElementType;
  label: string;
  value: string;
  sub?: string;
  color: string;
}) {
  return (
    <div
      className="rounded-xl p-4 flex flex-col gap-2"
      style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
    >
      <div className="flex items-center justify-between">
        <span className="text-xs text-zinc-400">{label}</span>
        <div
          className="w-7 h-7 rounded-lg flex items-center justify-center"
          style={{ background: `${color}20` }}
        >
          <Icon className="w-3.5 h-3.5" style={{ color }} />
        </div>
      </div>
      <p className="text-2xl font-semibold text-zinc-100 leading-none">{value}</p>
      {sub && <p className="text-xs text-zinc-500">{sub}</p>}
    </div>
  );
}

// ── Setup instructions (unconfigured state) ───────────────────────────────────

function SetupPanel() {
  const saEmail = 'blog-generator@alexandre-queiroz.iam.gserviceaccount.com';

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] px-6 max-w-2xl mx-auto text-center gap-6">
      <div
        className="w-16 h-16 rounded-2xl flex items-center justify-center"
        style={{ background: 'rgba(234,179,8,0.12)', border: '1px solid rgba(234,179,8,0.3)' }}
      >
        <BarChart3 className="w-7 h-7 text-yellow-400" />
      </div>

      <div>
        <h2 className="text-xl font-semibold text-zinc-100">Google Analytics não configurado</h2>
        <p className="text-sm text-zinc-400 mt-2 max-w-md mx-auto">
          Para exibir os dados do Google Analytics 4, siga os passos abaixo uma única vez.
        </p>
      </div>

      <div className="w-full text-left space-y-4">
        {[
          {
            step: '1',
            title: 'Crie uma propriedade GA4',
            desc: 'Acesse analytics.google.com → Admin → Create Property. Anote o Property ID (ex: 123456789).',
            link: 'https://analytics.google.com',
          },
          {
            step: '2',
            title: 'Adicione a conta de serviço como visualizador',
            desc: `Em Admin → Property Access Management → Add Users, adicione o e-mail abaixo com função "Viewer":`,
            badge: saEmail,
          },
          {
            step: '3',
            title: 'Configure o Property ID no servidor',
            desc: 'Adicione o segredo GA4__PropertyId no GitHub (Settings → Secrets → Actions) com o valor: properties/SEU_PROPERTY_ID',
            code: 'Name: DIAX_GA4__PropertyId\nValue: properties/123456789',
          },
          {
            step: '4',
            title: 'Adicione o tracking ao seu site',
            desc: 'No GA4, vá em Admin → Data Streams → Web → adicione seu site. Copie o Measurement ID (G-XXXXXXXX) e adicione o gtag.js ao seu site.',
          },
        ].map(({ step, title, desc, link, badge, code }) => (
          <div
            key={step}
            className="rounded-xl p-4 flex gap-4"
            style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
          >
            <div
              className="flex-shrink-0 w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold"
              style={{ background: 'rgba(16,185,129,0.15)', color: '#10B981' }}
            >
              {step}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-zinc-200">{title}</p>
              <p className="text-xs text-zinc-400 mt-1">{desc}</p>
              {badge && (
                <code className="block mt-2 text-xs bg-zinc-900 text-emerald-400 px-3 py-1.5 rounded-lg break-all">
                  {badge}
                </code>
              )}
              {code && (
                <pre className="mt-2 text-xs bg-zinc-900 text-zinc-300 px-3 py-2 rounded-lg overflow-x-auto">
                  {code}
                </pre>
              )}
              {link && (
                <a
                  href={link}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 mt-2 text-xs text-emerald-400 hover:text-emerald-300"
                >
                  Abrir GA4 <ExternalLink className="w-3 h-3" />
                </a>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function GoogleAnalyticsPage() {
  const [configured, setConfigured] = useState<boolean | null>(null);
  const [days, setDays] = useState(30);
  const [report, setReport] = useState<Ga4Report | null>(null);
  const [loading, setLoading] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => { setMounted(true); }, []);

  useEffect(() => {
    getGa4Status()
      .then(s => setConfigured(s.isConfigured))
      .catch(() => setConfigured(false));
  }, []);

  useEffect(() => {
    if (!configured) return;
    setLoading(true);
    setReport(null);
    getGa4Report(days)
      .then(setReport)
      .catch(e => {
        toast.error(e?.message ?? 'Erro ao buscar relatório GA4');
      })
      .finally(() => setLoading(false));
  }, [configured, days]);

  // ── Configurations unknown yet ─────────────────────────────────────────
  if (configured === null) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader2 className="w-6 h-6 animate-spin text-zinc-500" />
      </div>
    );
  }

  if (!configured) {
    return (
      <div className="min-h-screen" style={{ background: '#0F1A14' }}>
        <SetupPanel />
      </div>
    );
  }

  // ── Chart configs ──────────────────────────────────────────────────────
  const timeSeriesConfig = report ? {
    type: 'area' as const,
    height: 220,
    series: [
      { name: 'Sessões', data: report.timeSeries.map(p => p.sessions) },
      { name: 'Usuários', data: report.timeSeries.map(p => p.users) },
    ],
    options: {
      chart: { background: 'transparent', toolbar: { show: false }, sparkline: { enabled: false } },
      colors: ['#10B981', '#6EE7B7'],
      fill: { type: 'gradient', gradient: { opacityFrom: 0.25, opacityTo: 0.0 } },
      stroke: { curve: 'smooth' as const, width: 2 },
      xaxis: {
        categories: report.timeSeries.map(p => p.date.slice(5)),
        labels: { style: { colors: '#71717a', fontSize: '10px' } },
        axisBorder: { show: false },
        axisTicks: { show: false },
        tickAmount: Math.min(7, report.timeSeries.length),
      },
      yaxis: { labels: { style: { colors: '#71717a', fontSize: '10px' } } },
      grid: { borderColor: 'rgba(255,255,255,0.06)', strokeDashArray: 4 },
      legend: { labels: { colors: '#a1a1aa' } },
      tooltip: { theme: 'dark' as const },
    },
  } : null;

  const trafficDonutConfig = report ? {
    type: 'donut' as const,
    height: 200,
    series: report.trafficSources.map(t => t.sessions),
    options: {
      labels: report.trafficSources.map(t => t.channel),
      colors: report.trafficSources.map(t => channelColor(t.channel)),
      chart: { background: 'transparent' },
      legend: { position: 'bottom' as const, labels: { colors: '#a1a1aa' }, fontSize: '11px' },
      dataLabels: { enabled: false },
      tooltip: { theme: 'dark' as const },
      plotOptions: { pie: { donut: { size: '65%' } } },
    },
  } : null;

  const deviceBarConfig = report ? {
    type: 'bar' as const,
    height: 180,
    series: [{ name: 'Sessões', data: report.devices.map(d => d.sessions) }],
    options: {
      chart: { background: 'transparent', toolbar: { show: false } },
      colors: ['#10B981'],
      plotOptions: { bar: { horizontal: true, borderRadius: 4, barHeight: '50%' } },
      xaxis: { categories: report.devices.map(d => d.device), labels: { style: { colors: '#71717a', fontSize: '11px' } } },
      yaxis: { labels: { style: { colors: '#a1a1aa', fontSize: '11px' } } },
      grid: { borderColor: 'rgba(255,255,255,0.06)', xaxis: { lines: { show: true } }, yaxis: { lines: { show: false } } },
      tooltip: { theme: 'dark' as const },
      dataLabels: { enabled: false },
    },
  } : null;

  // ── Rendered page ──────────────────────────────────────────────────────
  return (
    <div className="min-h-screen p-6 max-w-7xl mx-auto" style={{ color: '#E5E7EB' }}>
      {/* Header */}
      <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
        <div>
          <h1 className="text-xl font-semibold text-zinc-100 flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-emerald-400" />
            Google Analytics
          </h1>
          {report && (
            <p className="text-xs text-zinc-500 mt-0.5">
              {report.startDate} → {report.endDate}
            </p>
          )}
        </div>

        <div className="flex items-center gap-2">
          {/* Period selector */}
          <div
            className="flex rounded-lg overflow-hidden"
            style={{ border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.04)' }}
          >
            {PERIOD_OPTIONS.map(o => (
              <button
                key={o.value}
                type="button"
                onClick={() => setDays(o.value)}
                className={`px-3 py-1.5 text-xs transition-colors ${
                  days === o.value
                    ? 'bg-emerald-600 text-white font-medium'
                    : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/5'
                }`}
              >
                {o.label}
              </button>
            ))}
          </div>

          {/* Refresh */}
          <button
            type="button"
            onClick={() => {
              setLoading(true);
              setReport(null);
              getGa4Report(days)
                .then(setReport)
                .catch(e => toast.error(e?.message ?? 'Erro'))
                .finally(() => setLoading(false));
            }}
            disabled={loading}
            className="p-2 rounded-lg text-zinc-400 hover:text-zinc-100 hover:bg-white/5 transition-colors disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* Loading skeleton */}
      {loading && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="rounded-xl h-24 animate-pulse" style={{ background: 'rgba(255,255,255,0.05)' }} />
            ))}
          </div>
          <div className="rounded-xl h-64 animate-pulse" style={{ background: 'rgba(255,255,255,0.04)' }} />
        </div>
      )}

      {/* Data */}
      {!loading && report && (
        <div className="space-y-4">
          {/* KPI row */}
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
            <KpiCard icon={Users} label="Sessões" value={report.overview.sessions.toLocaleString('pt-BR')} color="#10B981" />
            <KpiCard icon={Users} label="Usuários" value={report.overview.users.toLocaleString('pt-BR')} color="#6EE7B7" />
            <KpiCard icon={Activity} label="Novos usuários" value={report.overview.newUsers.toLocaleString('pt-BR')} color="#3B82F6" />
            <KpiCard icon={Globe} label="Pageviews" value={report.overview.pageViews.toLocaleString('pt-BR')} color="#A78BFA" />
            <KpiCard
              icon={report.overview.bounceRate > 0.6 ? TrendingDown : TrendingUp}
              label="Bounce rate"
              value={`${(report.overview.bounceRate * 100).toFixed(1)}%`}
              color={report.overview.bounceRate > 0.6 ? '#F87171' : '#10B981'}
            />
            <KpiCard
              icon={Clock}
              label="Duração média"
              value={formatDuration(report.overview.avgSessionDuration)}
              color="#F59E0B"
            />
          </div>

          {/* Sessions chart + Traffic sources */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {/* Time series */}
            <div
              className="lg:col-span-2 rounded-xl p-4"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
            >
              <h3 className="text-sm font-medium text-zinc-300 mb-3 flex items-center gap-2">
                <Activity className="w-4 h-4 text-emerald-400" />
                Sessões ao longo do tempo
              </h3>
              {mounted && timeSeriesConfig && (
                <ApexChart
                  type={timeSeriesConfig.type}
                  height={timeSeriesConfig.height}
                  series={timeSeriesConfig.series}
                  options={timeSeriesConfig.options}
                />
              )}
              {report.timeSeries.length === 0 && (
                <p className="text-xs text-zinc-500 text-center py-16">
                  Sem dados neste período. O tracking pode não estar configurado no site.
                </p>
              )}
            </div>

            {/* Traffic sources */}
            <div
              className="rounded-xl p-4"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
            >
              <h3 className="text-sm font-medium text-zinc-300 mb-3 flex items-center gap-2">
                <Chrome className="w-4 h-4 text-blue-400" />
                Fontes de tráfego
              </h3>
              {report.trafficSources.length > 0 ? (
                <>
                  {mounted && trafficDonutConfig && (
                    <ApexChart
                      type={trafficDonutConfig.type}
                      height={trafficDonutConfig.height}
                      series={trafficDonutConfig.series}
                      options={trafficDonutConfig.options}
                    />
                  )}
                  <div className="space-y-1.5 mt-2">
                    {report.trafficSources.map(t => (
                      <div key={t.channel} className="flex items-center justify-between text-xs">
                        <span className="flex items-center gap-1.5">
                          <span
                            className="w-2 h-2 rounded-full flex-shrink-0"
                            style={{ background: channelColor(t.channel) }}
                          />
                          <span className="text-zinc-400 truncate">{t.channel}</span>
                        </span>
                        <span className="text-zinc-300 font-medium">
                          {t.sessions.toLocaleString('pt-BR')} ({t.percentage}%)
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <p className="text-xs text-zinc-500 text-center py-8">Sem dados de tráfego</p>
              )}
            </div>
          </div>

          {/* Devices + Top Pages */}
          <div className="grid grid-cols-1 lg:grid-cols-5 gap-4">
            {/* Devices */}
            <div
              className="lg:col-span-2 rounded-xl p-4"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
            >
              <h3 className="text-sm font-medium text-zinc-300 mb-3 flex items-center gap-2">
                <Monitor className="w-4 h-4 text-purple-400" />
                Dispositivos
              </h3>
              {report.devices.length > 0 ? (
                <>
                  {mounted && deviceBarConfig && (
                    <ApexChart
                      type={deviceBarConfig.type}
                      height={deviceBarConfig.height}
                      series={deviceBarConfig.series}
                      options={deviceBarConfig.options}
                    />
                  )}
                  <div className="space-y-2 mt-2">
                    {report.devices.map(d => (
                      <div key={d.device} className="flex items-center justify-between text-xs">
                        <span className="flex items-center gap-1.5 text-zinc-400">
                          {deviceIcon(d.device)}
                          <span className="capitalize">{d.device}</span>
                        </span>
                        <span className="text-zinc-300 font-medium">
                          {d.percentage}%
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <p className="text-xs text-zinc-500 text-center py-8">Sem dados de dispositivos</p>
              )}
            </div>

            {/* Top Pages */}
            <div
              className="lg:col-span-3 rounded-xl p-4"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
            >
              <h3 className="text-sm font-medium text-zinc-300 mb-3 flex items-center gap-2">
                <Globe className="w-4 h-4 text-emerald-400" />
                Páginas mais acessadas
              </h3>
              {report.topPages.length > 0 ? (
                <div className="space-y-0.5">
                  {/* Header */}
                  <div className="grid grid-cols-12 text-[10px] text-zinc-500 uppercase tracking-wider pb-2 border-b border-zinc-800">
                    <span className="col-span-7">Página</span>
                    <span className="col-span-3 text-right">Pageviews</span>
                    <span className="col-span-2 text-right">Usuários</span>
                  </div>

                  {report.topPages.map((p, i) => (
                    <div
                      key={i}
                      className="grid grid-cols-12 py-2 text-xs border-b border-zinc-800/50 hover:bg-white/3 transition-colors rounded"
                    >
                      <div className="col-span-7 flex items-center gap-2 min-w-0">
                        <span className="text-zinc-600 font-mono text-[10px] w-4 flex-shrink-0">{i + 1}</span>
                        <span className="text-zinc-300 truncate font-mono text-[10px]">{p.page}</span>
                      </div>
                      <span className="col-span-3 text-right text-zinc-300">
                        {p.pageViews.toLocaleString('pt-BR')}
                      </span>
                      <span className="col-span-2 text-right text-zinc-400">
                        {p.users.toLocaleString('pt-BR')}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="flex flex-col items-center gap-2 py-8">
                  <AlertCircle className="w-5 h-5 text-zinc-600" />
                  <p className="text-xs text-zinc-500 text-center">
                    Sem dados de páginas neste período.<br />
                    Certifique-se de que o tracking GA4 está instalado no site.
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Empty state (configured but no data yet) */}
      {!loading && !report && configured && (
        <div className="flex flex-col items-center gap-3 py-24 text-center">
          <AlertCircle className="w-8 h-8 text-zinc-600" />
          <p className="text-sm text-zinc-400">Não foi possível carregar os dados. Tente novamente.</p>
          <button
            type="button"
            onClick={() => {
              setLoading(true);
              getGa4Report(days)
                .then(setReport)
                .catch(e => toast.error(e?.message ?? 'Erro'))
                .finally(() => setLoading(false));
            }}
            className="text-xs text-emerald-400 hover:text-emerald-300 underline underline-offset-2"
          >
            Tentar novamente
          </button>
        </div>
      )}
    </div>
  );
}
