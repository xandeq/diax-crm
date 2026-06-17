'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useCrmDashboard } from '../hooks/useCrmDashboard';
import { LoadingSkeleton, LoadingGrid } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { MetricCard } from '@/components/dashboard/MetricCard';
import { ChartCard } from '@/components/dashboard/ChartCard';
import { StatusBadge } from '@/components/dashboard/StatusBadge';
import { HealthBadge } from './HealthBadge';
import { Button } from '@/components/ui/button';
import {
  Users, Target, AlertTriangle, UserCheck, Clock,
  ArrowRight, Search, Globe, Laptop, Database, Edit, Trash
} from 'lucide-react';
import Link from 'next/link';
import dynamic from 'next/dynamic';

const ApexChart = dynamic(() => import('react-apexcharts'), { ssr: false });

const C = {
  primary: '#00D4AA',
  success: '#22C55E',
  loss: '#EF4444',
  warn: '#F59E0B',
  info: '#818CF8',
  accent: '#F472B6',
  funnel: ['#6366F1', '#8B5CF6', '#A78BFA', '#EC4899', '#00D4AA'],
  sources: ['#10B981', '#3B82F6', '#8B5CF6', '#F59E0B'],
};

export function CrmTab() {
  const { data, isLoading, isError, error, refetch } = useCrmDashboard();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const handleSync = () => refetch();
    window.addEventListener('dashboard-sync', handleSync);
    return () => window.removeEventListener('dashboard-sync', handleSync);
  }, [refetch]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <LoadingGrid cards={4} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <LoadingSkeleton rows={2} />
          <LoadingSkeleton rows={2} />
        </div>
      </div>
    );
  }

  if (isError) {
    return <ErrorState message={error instanceof Error ? error.message : undefined} onRetry={refetch} />;
  }

  if (!data) return null;

  const {
    funnel,
    segments,
    sources,
    avgLeadScore,
    stageConversions,
    bottlenecks,
    recentLeads,
    topOpportunities,
    staleLeads,
    cityAllocation,
  } = data;

  const totalActiveLeads = funnel.lead + funnel.contacted + funnel.qualified + funnel.negotiating;

  const kpis = [
    { label: 'Total Leads Ativos', value: totalActiveLeads, color: C.info, icon: <Users size={14} /> },
    { label: 'Clientes Ganhos', value: funnel.customer, color: C.primary, icon: <UserCheck size={14} /> },
    { label: 'Lead Score Médio', value: avgLeadScore, suffix: '/100', color: avgLeadScore >= 60 ? C.success : C.warn, icon: <Target size={14} /> },
    { label: 'Gargalo no Funil', value: bottlenecks[0] ? bottlenecks[0].drop : 0, suffix: '% drop', color: bottlenecks[0]?.drop > 50 ? C.loss : C.warn, icon: <AlertTriangle size={14} /> },
  ];

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
        {kpis.map((k, idx) => (
          <MetricCard key={k.label} {...k} idx={idx} />
        ))}
      </div>

      {/* Pipeline & Origin row */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Conversão de Estágios</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Porcentagem de conversão entre etapas do pipeline</p>
          <div className="space-y-3">
            {stageConversions.map((conv, idx) => (
              <div key={idx} className="p-3 bg-zinc-900/30 border border-zinc-850 rounded-xl flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-zinc-200">{conv.from}</span>
                  <span className="text-zinc-500 mx-2">→</span>
                  <span className="text-xs font-bold text-zinc-300">{conv.to}</span>
                </div>
                <div className="flex items-center gap-3">
                  <div className="w-24 bg-zinc-800 h-2 rounded-full overflow-hidden shrink-0">
                    <div className="bg-teal-400 h-full rounded-full" style={{ width: `${conv.pct}%` }} />
                  </div>
                  <span className="font-mono text-xs font-bold text-teal-400 w-10 text-right">{conv.pct.toFixed(0)}%</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Origem dos Leads</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Canais de captação mais expressivos</p>
          {mounted && (
            <ApexChart
              type="donut"
              height={220}
              series={[sources.manual, sources.scraping, sources.import, sources.googleMaps]}
              options={{
                chart: { background: 'transparent' },
                labels: ['Manual', 'Scraping', 'Importação', 'Google Maps'],
                colors: [...C.sources],
                stroke: { width: 0 },
                legend: { position: 'bottom', labels: { colors: C.primary } },
                dataLabels: { enabled: false },
                plotOptions: { pie: { donut: { size: '60%' } } },
                tooltip: { theme: 'dark' },
              }}
            />
          )}
        </div>
      </div>

      {/* Allocation by Segment and City */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Segmentação de Temperatura</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Distribuição de leads por interesse</p>
          <div className="space-y-4">
            {[
              { label: 'Hot (Alto interesse)', count: segments.hot, pct: totalActiveLeads > 0 ? (segments.hot / totalActiveLeads) * 100 : 0, color: '#EF4444' },
              { label: 'Warm (Interesse médio)', count: segments.warm, pct: totalActiveLeads > 0 ? (segments.warm / totalActiveLeads) * 100 : 0, color: '#F59E0B' },
              { label: 'Cold (Baixo interesse)', count: segments.cold, pct: totalActiveLeads > 0 ? (segments.cold / totalActiveLeads) * 100 : 0, color: '#3B82F6' },
            ].map(seg => (
              <div key={seg.label} className="space-y-1.5">
                <div className="flex justify-between text-xs">
                  <span className="font-semibold text-zinc-300">{seg.label}</span>
                  <span className="font-mono font-bold text-zinc-100">{seg.count} leads</span>
                </div>
                <div className="w-full bg-zinc-900 h-2.5 rounded-full overflow-hidden">
                  <div className="h-full rounded-full" style={{ width: `${seg.pct}%`, backgroundColor: seg.color }} />
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Geografia de Prospecção</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Maiores pólos de captação (estimado)</p>
          {cityAllocation.length > 0 ? (
            <div className="space-y-3">
              {cityAllocation.map((city, idx) => (
                <div key={city.name} className="flex justify-between items-center text-xs">
                  <span className="font-semibold text-zinc-300">{idx + 1}. {city.name}</span>
                  <span className="px-2 py-0.5 font-mono font-bold text-teal-400 bg-teal-500/10 rounded-full border border-teal-500/20">
                    {city.count} leads
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-8 text-zinc-500">
              <span className="text-xs font-semibold">Sem dados geográficos processados nas notas dos leads</span>
            </div>
          )}
        </div>

        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Leads Ociosos</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Leads ativos sem contato há mais de 7 dias</p>
          {staleLeads.length > 0 ? (
            <div className="space-y-2.5">
              {staleLeads.slice(0, 4).map(lead => (
                <div key={lead.id} className="flex items-center justify-between text-xs p-2 rounded-lg bg-zinc-900/20 border border-zinc-850">
                  <div className="truncate pr-2 max-w-[70%]">
                    <div className="font-bold text-zinc-200 truncate">{lead.name}</div>
                    <div className="text-[10px] text-zinc-500 truncate">{lead.companyName || 'Sem empresa'}</div>
                  </div>
                  <span className="text-[10px] font-bold text-amber-400 flex items-center gap-1 shrink-0">
                    <Clock className="h-3 w-3" /> Sem contato
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-8 text-zinc-500">
              <span className="text-xs font-semibold">Tudo em ordem: sem leads pendentes há muito tempo</span>
            </div>
          )}
        </div>
      </div>

      {/* Top Opportunities & Action table */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="md:col-span-2 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Visão Geral Comercial</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Leads e clientes cadastrados recentemente</p>
            </div>
            <Button asChild size="sm" variant="outline" className="h-8 border-zinc-800 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 font-semibold text-xs">
              <Link href="/leads">Ver Base Completa</Link>
            </Button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b border-zinc-800/80 text-zinc-500 font-bold uppercase tracking-wider">
                  <th className="py-2.5">Nome</th>
                  <th className="py-2.5">Status</th>
                  <th className="py-2.5">Empresa</th>
                  <th className="py-2.5">Score</th>
                  <th className="py-2.5 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-900">
                {recentLeads.slice(0, 6).map(lead => (
                  <tr key={lead.id} className="hover:bg-zinc-900/10">
                    <td className="py-2.5 font-bold text-zinc-200">{lead.name}</td>
                    <td className="py-2.5">
                      <span className="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full bg-zinc-800 text-zinc-400">
                        {lead.status === 4 ? 'Cliente' : lead.status === 0 ? 'Lead' : 'Em Contato'}
                      </span>
                    </td>
                    <td className="py-2.5 text-zinc-400">{lead.companyName || '—'}</td>
                    <td className="py-2.5 font-mono font-bold text-zinc-300">{lead.leadScore ?? '—'}</td>
                    <td className="py-2.5 text-right">
                      <Button asChild size="icon" variant="ghost" className="h-7 w-7 text-zinc-400 hover:text-zinc-100">
                        <Link href={`/leads`}>
                          <ArrowRight className="h-3.5 w-3.5" />
                        </Link>
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Maiores Oportunidades</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Leads com alta intenção comercial</p>
          {topOpportunities.length > 0 ? (
            <div className="space-y-3">
              {topOpportunities.slice(0, 5).map(lead => (
                <div key={lead.id} className="p-3 bg-zinc-900/20 border border-zinc-850 rounded-xl flex items-center justify-between">
                  <div className="max-w-[70%]">
                    <div className="font-bold text-zinc-100 truncate">{lead.name}</div>
                    <div className="text-[10px] text-zinc-500 truncate">{lead.email}</div>
                  </div>
                  <div className="text-right shrink-0">
                    <span className="text-xs font-mono font-bold text-emerald-400">
                      Score: {lead.leadScore}
                    </span>
                    <div className="text-[9px] font-bold text-zinc-500 uppercase tracking-wider mt-0.5">Hot</div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-8 text-zinc-500">
              <span className="text-xs font-semibold">Sem oportunidades de alta conversão no radar</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
