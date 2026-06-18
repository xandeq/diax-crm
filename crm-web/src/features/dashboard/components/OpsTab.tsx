'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useOpsDashboard } from '../hooks/useOpsDashboard';
import { LoadingSkeleton, LoadingGrid } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { HealthBadge } from './HealthBadge';
import { RecentActivityTimeline } from './RecentActivityTimeline';
import { MetricCard } from '@/components/dashboard/MetricCard';
import { Button } from '@/components/ui/button';
import { 
  Cpu, HardDrive, Server, RefreshCw, AlertTriangle, 
  Terminal, ShieldCheck, Database, Zap, Activity 
} from 'lucide-react';
import Link from 'next/link';

const C = {
  primary: '#00D4AA',
  success: '#22C55E',
  loss: '#EF4444',
  warn: '#F59E0B',
  info: '#818CF8',
};

export function OpsTab() {
  const { data, isLoading, isError, error, refetch } = useOpsDashboard();
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
        <LoadingGrid cards={3} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <LoadingSkeleton rows={3} />
          <LoadingSkeleton rows={3} />
        </div>
      </div>
    );
  }

  if (isError) {
    return <ErrorState message={error instanceof Error ? error.message : undefined} onRetry={refetch} />;
  }

  if (!data) return null;

  const { errorStats, recentErrors, logStats, recentLogs, systemHealth, n8nWorkflows, scrapingStatus, activeIntegrations } = data;

  const uptimeDays = Math.floor(systemHealth.uptimeSeconds / (24 * 3600));
  const uptimeHours = Math.floor((systemHealth.uptimeSeconds % (24 * 3600)) / 3600);

  const kpis = [
    { label: 'Erros Hoje', value: errorStats.totalToday, color: errorStats.totalToday > 0 ? C.loss : C.success, icon: <AlertTriangle size={14} /> },
    { label: 'Críticos Hoje', value: errorStats.criticalToday, color: errorStats.criticalToday > 0 ? C.loss : C.success, icon: <Activity size={14} /> },
    { label: 'Erros Pendentes', value: errorStats.unresolvedTotal, color: errorStats.unresolvedTotal > 0 ? C.loss : C.success, icon: <Database size={14} /> },
  ];

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {kpis.map((k, idx) => (
          <MetricCard key={k.label} {...k} idx={idx} />
        ))}
      </div>

      {/* Hardware / Health row */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* System Health Performance */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100 mb-1">Status de Hardware</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Métricas estimadas de performance do servidor</p>
            
            <div className="space-y-3 mt-2">
              <div className="space-y-1">
                <div className="flex justify-between text-xs font-semibold text-zinc-300">
                  <span className="flex items-center gap-1.5"><Cpu className="h-3.5 w-3.5" /> CPU</span>
                  <span className="font-mono">{systemHealth.cpuUsage}%</span>
                </div>
                <div className="w-full bg-zinc-900 h-2 rounded-full overflow-hidden">
                  <div className="bg-teal-400 h-full rounded-full" style={{ width: `${systemHealth.cpuUsage}%` }} />
                </div>
              </div>

              <div className="space-y-1">
                <div className="flex justify-between text-xs font-semibold text-zinc-300">
                  <span className="flex items-center gap-1.5"><HardDrive className="h-3.5 w-3.5" /> Memória</span>
                  <span className="font-mono">{systemHealth.memoryUsage}MB / {systemHealth.memoryLimit}MB</span>
                </div>
                <div className="w-full bg-zinc-900 h-2 rounded-full overflow-hidden">
                  <div className="bg-teal-400 h-full rounded-full" style={{ width: `${(systemHealth.memoryUsage / systemHealth.memoryLimit) * 100}%` }} />
                </div>
              </div>

              <div className="pt-2 flex justify-between text-[10px] text-zinc-500 font-bold uppercase tracking-wider">
                <span>Latency: {systemHealth.responseTimeMs}ms</span>
                <span>Uptime: {uptimeDays}d {uptimeHours}h</span>
              </div>
            </div>
          </div>
          
          <Button asChild size="sm" className="w-full mt-4 h-9 bg-zinc-900 border border-zinc-800 text-zinc-300 font-semibold hover:bg-zinc-800 border-none shadow-[0_4px_15px_rgba(0,0,0,0.5)]">
            <Link href="/monitoring">Central de Erros</Link>
          </Button>
        </div>

        {/* n8n Status workflows */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100 mb-1">Automações n8n</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Status de workflows de automação e webhooks</p>
            
            <div className="space-y-2.5 max-h-36 overflow-y-auto scrollbar-none pr-1">
              {n8nWorkflows.map(wf => (
                <div key={wf.id} className="flex justify-between items-center text-xs">
                  <span className="font-bold text-zinc-300 truncate max-w-[60%]">{wf.name}</span>
                  <HealthBadge status={wf.status === 'active' ? 'healthy' : 'error'} label={wf.status === 'active' ? 'Ativo' : 'Erro'} />
                </div>
              ))}
            </div>
          </div>

          <div className="text-[10px] text-zinc-500 font-semibold text-center mt-2">
            Status dos fluxos exportados e processados localmente.
          </div>
        </div>

        {/* Google Scraping status */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100 mb-1">Google Scraping status</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Informações da última captação de leads por robô</p>
            
            <div className="space-y-3 mt-2">
              <div className="flex items-center justify-between text-xs font-semibold">
                <span className="text-zinc-400">Status do Scraper</span>
                <HealthBadge status={scrapingStatus.isRunning ? 'warning' : 'healthy'} label={scrapingStatus.isRunning ? 'Rodando' : 'Idle'} />
              </div>
              <div className="flex items-center justify-between text-xs font-semibold">
                <span className="text-zinc-400">Leads capturados</span>
                <span className="font-mono text-zinc-150 font-bold">{scrapingStatus.leadsFound}</span>
              </div>
              <div className="flex items-center justify-between text-xs font-semibold">
                <span className="text-zinc-400">Leads higienizados</span>
                <span className="font-mono text-emerald-400 font-bold">{scrapingStatus.leadsSanitized}</span>
              </div>
            </div>
          </div>

          <Button asChild size="sm" className="w-full mt-4 h-9 bg-zinc-900 border border-zinc-800 text-zinc-300 font-semibold hover:bg-zinc-800 border-none shadow-[0_4px_15px_rgba(0,0,0,0.5)]">
            <Link href="/leads/import">Painel de Importação</Link>
          </Button>
        </div>
      </div>

      {/* Audit logs & Application logs */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Logs de aplicação */}
        <div className="lg:col-span-8 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Atividades de Sistema</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Últimos eventos gerais e de rede gravados em logs</p>
            </div>
            <Button asChild size="sm" variant="outline" className="h-8 border-zinc-800 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 font-semibold text-xs">
              <Link href="/logs">Ver Logs Completos</Link>
            </Button>
          </div>
          
          <RecentActivityTimeline logs={recentLogs} />
        </div>

        {/* Integrações ativas */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Integrações Conectadas</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Saúde dos SDKs e APIs de terceiros</p>
          
          <div className="space-y-3">
            {activeIntegrations.map(integ => (
              <div key={integ.name} className="flex justify-between items-center text-xs p-2.5 bg-zinc-900/20 border border-zinc-850 rounded-lg">
                <span className="font-bold text-zinc-200">{integ.name}</span>
                <HealthBadge status="healthy" label="OK" />
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
