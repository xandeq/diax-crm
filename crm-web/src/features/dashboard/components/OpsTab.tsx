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
import { toast } from 'sonner';
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

      {/* Central de Erros & Ações de Recuperação */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Erros Filtrados */}
        <div className="lg:col-span-8 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl space-y-4">
          <div className="flex justify-between items-center">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Eventos de Erros no Sistema</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Filtragem inteligente de erros críticos vs. ruído benigno</p>
            </div>
            <Button asChild size="sm" variant="outline" className="h-8 border-zinc-800 bg-zinc-900 hover:bg-zinc-800 text-zinc-300 font-semibold text-xs">
              <Link href="/monitoring">Ir para Monitoramento</Link>
            </Button>
          </div>

          <div className="space-y-2.5 max-h-[300px] overflow-y-auto scrollbar-none pr-1">
            {recentErrors && recentErrors.length > 0 ? (
              recentErrors.map((err: any) => {
                const isNoise = err.message?.toLowerCase().includes('resizeobserver') || 
                                err.message?.toLowerCase().includes('apexcharts') ||
                                err.message?.toLowerCase().includes('resize observer');
                
                const isCritical = err.level === 'Critical' || 
                                   err.message?.toLowerCase().includes('database') ||
                                   err.message?.toLowerCase().includes('connection') ||
                                   err.message?.toLowerCase().includes('auth') ||
                                   err.message?.toLowerCase().includes('smtp');
                
                const badgeVariant = isNoise ? 'default' : isCritical ? 'destructive' : 'warning';
                const labelText = isNoise ? 'Ruído (Benigno)' : isCritical ? 'Crítico' : 'Erro';

                return (
                  <div key={err.id} className="p-3 bg-zinc-900/20 border border-zinc-850 rounded-xl flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className={`px-2 py-0.5 text-[9px] font-bold uppercase rounded-full shrink-0 ${
                          badgeVariant === 'destructive' ? 'bg-red-500/15 text-red-400' :
                          badgeVariant === 'warning' ? 'bg-amber-500/15 text-amber-400' :
                          'bg-zinc-800 text-zinc-400'
                        }`}>
                          {labelText}
                        </span>
                        <span className="text-[10px] text-zinc-500 font-bold uppercase tracking-wider truncate">{err.appName}</span>
                      </div>
                      <p className="text-xs font-bold text-zinc-200 mt-1.5 truncate leading-relaxed">{err.message}</p>
                    </div>
                    <div className="text-left sm:text-right shrink-0 text-[10px] text-zinc-500 font-semibold">
                      <div className="font-mono">{new Date(err.occurredAt).toLocaleTimeString('pt-BR')}</div>
                      <div className="mt-0.5">{new Date(err.occurredAt).toLocaleDateString('pt-BR')}</div>
                    </div>
                  </div>
                );
              })
            ) : (
              <div className="flex flex-col items-center justify-center py-12 text-zinc-500">
                <span className="text-xs font-semibold">Nenhum erro registrado no banco</span>
              </div>
            )}
          </div>
        </div>

        {/* Ações de Recuperação Rápidas */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div className="space-y-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Ações Rápidas de Ops</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Execuções de manutenção e reprocessamento</p>
            </div>

            <div className="space-y-2.5">
              <Button asChild className="w-full justify-start h-10 bg-zinc-900 border border-zinc-800 text-zinc-300 hover:bg-zinc-800 text-xs gap-2">
                <Link href="/logs">
                  <Terminal className="h-4 w-4 text-teal-400 shrink-0" />
                  <span>Investigar Logs Gerais</span>
                </Link>
              </Button>
              <Button asChild className="w-full justify-start h-10 bg-zinc-900 border border-zinc-800 text-zinc-300 hover:bg-zinc-800 text-xs gap-2">
                <Link href="/email-marketing/pro">
                  <Server className="h-4 w-4 text-blue-400 shrink-0" />
                  <span>Verificar Limites SMTP</span>
                </Link>
              </Button>
              <Button 
                onClick={() => {
                  refetch();
                  toast.success('Reprocessamento de integrações iniciado!');
                }}
                className="w-full justify-start h-10 bg-zinc-900 border border-zinc-800 text-zinc-300 hover:bg-zinc-850 text-xs gap-2 cursor-pointer"
              >
                <RefreshCw className="h-4 w-4 text-emerald-400 shrink-0 animate-spin" />
                <span>Reprocessar Fila de Integração</span>
              </Button>
            </div>
          </div>
          
          <div className="text-[9px] text-zinc-500 font-bold uppercase tracking-wider text-center mt-6">
            Servidor principal: Online · Latência: {systemHealth.responseTimeMs}ms
          </div>
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
