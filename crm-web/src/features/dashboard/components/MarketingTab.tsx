'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import { useMarketingDashboard } from '../hooks/useMarketingDashboard';
import { LoadingSkeleton, LoadingGrid } from './LoadingSkeleton';
import { ErrorState } from './ErrorState';
import { MetricCard } from '@/components/dashboard/MetricCard';
import { ChartCard } from '@/components/dashboard/ChartCard';
import { HealthBadge } from './HealthBadge';
import { Button } from '@/components/ui/button';
import { 
  Mail, MessageSquare, Send, CheckCircle, AlertTriangle, 
  Clock, ArrowRight, ShieldCheck, Database, Server, Smartphone 
} from 'lucide-react';
import Link from 'next/link';

const C = {
  primary: '#00D4AA',
  success: '#22C55E',
  loss: '#EF4444',
  warn: '#F59E0B',
  info: '#818CF8',
  accent: '#F472B6',
};

const R = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 0, maximumFractionDigits: 0 });

export function MarketingTab() {
  const { data, isLoading, isError, error, refetch } = useMarketingDashboard();
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
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <LoadingSkeleton rows={2} />
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

  const { outreach, whatsapp, campaigns, queue, providerHealth, stats, nextSafeSendTime, channels } = data;

  const kpis = [
    { label: 'Emails Enviados', value: stats.totalSent, color: C.info, icon: <Mail size={14} /> },
    { label: 'Campanhas Realizadas', value: campaigns.length, color: C.primary, icon: <Send size={14} /> },
    { label: 'Abertura Média', value: stats.openRate, suffix: '%', color: stats.openRate >= 20 ? C.success : C.warn, icon: <CheckCircle size={14} /> },
    { label: 'Cliques Médios', value: stats.clickRate, suffix: '%', color: C.accent, icon: <CheckCircle size={14} /> },
  ];

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
        {kpis.map((k, idx) => (
          <MetricCard key={k.label} {...k} idx={idx} />
        ))}
      </div>

      {/* WhatsApp & Email Workers row */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* WhatsApp Integration Status */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-sm font-bold text-zinc-100">WhatsApp Integration</h3>
                <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Evolution API (Baileys v2)</p>
              </div>
              <HealthBadge status={whatsapp.status} label={whatsapp.connected ? 'Conectado' : 'Desconectado'} />
            </div>
            
            <div className="space-y-3.5 mt-2">
              <div className="flex items-center gap-3">
                <Smartphone className="h-5 w-5 text-teal-400 shrink-0" />
                <div>
                  <div className="text-xs font-bold text-zinc-300">Instância ativa</div>
                  <div className="text-[10px] text-zinc-500 font-mono mt-0.5">{whatsapp.instanceName}</div>
                </div>
              </div>
              
              <div className="grid grid-cols-2 gap-4 pt-1">
                <div>
                  <div className="text-[10px] font-bold text-zinc-500 uppercase tracking-wider">Disparados hoje</div>
                  <div className="text-lg font-bold text-zinc-200 font-mono mt-0.5">{whatsapp.sentToday}</div>
                </div>
                <div>
                  <div className="text-[10px] font-bold text-zinc-500 uppercase tracking-wider">Aguardando na fila</div>
                  <div className="text-lg font-bold text-zinc-200 font-mono mt-0.5">{whatsapp.queuedMessages}</div>
                </div>
              </div>
            </div>
          </div>
          
          <Button asChild size="sm" className="w-full mt-4 h-9 bg-zinc-900 border border-zinc-800 text-zinc-300 font-semibold hover:bg-zinc-800 border-none shadow-[0_4px_15px_rgba(0,0,0,0.5)]">
            <Link href="/outreach">Gerenciar WhatsApp</Link>
          </Button>
        </div>

        {/* Email Worker Safe Schedule */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100 mb-1">Email Queue Worker</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Mecanismo de controle e batching de envios</p>
            
            <div className="space-y-3 mt-2">
              <div className="flex items-center gap-3">
                <Clock className="h-5 w-5 text-blue-400 shrink-0" />
                <div>
                  <div className="text-xs font-bold text-zinc-300">Próximo envio seguro</div>
                  <div className="text-[10px] text-zinc-500 font-mono mt-0.5">
                    {new Date(nextSafeSendTime).toLocaleTimeString('pt-BR')}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <Database className="h-5 w-5 text-amber-400 shrink-0" />
                <div>
                  <div className="text-xs font-bold text-zinc-300">Fila de envio</div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">
                    {outreach?.pendingInQueue ?? 0} pendentes · {outreach?.failedInQueue ?? 0} com falha
                  </div>
                </div>
              </div>
            </div>
          </div>

          <Button asChild size="sm" className="w-full mt-4 h-9 bg-zinc-900 border border-zinc-800 text-zinc-300 font-semibold hover:bg-zinc-800 border-none shadow-[0_4px_15px_rgba(0,0,0,0.5)]">
            <Link href="/email-marketing">Compor E-mail</Link>
          </Button>
        </div>

        {/* SMTP Providers Health */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl flex flex-col justify-between">
          <div>
            <h3 className="text-sm font-bold text-zinc-100 mb-1">SMTP Providers Health</h3>
            <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Saúde e limites operacionais dos provedores</p>
            
            <div className="space-y-2 max-h-36 overflow-y-auto scrollbar-none pr-1">
              {providerHealth.length > 0 ? (
                providerHealth.map(prov => (
                  <div key={prov.provider} className="flex justify-between items-center text-xs">
                    <span className="font-bold text-zinc-300 truncate max-w-[50%]">{prov.provider}</span>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-[10px] text-zinc-500">
                        {prov.sentToday}/{prov.dailyLimit}
                      </span>
                      <HealthBadge status={prov.health === 'ok' ? 'healthy' : prov.health === 'degraded' ? 'warning' : 'critical'} label={prov.health === 'ok' ? 'OK' : prov.health === 'degraded' ? 'Alerta' : 'DOWN'} />
                    </div>
                  </div>
                ))
              ) : (
                <div className="flex flex-col items-center justify-center py-4 text-zinc-500">
                  <Server className="h-5 w-5 mb-1 stroke-[1.5]" />
                  <span className="text-[10px] font-semibold">Sem provedores SMTP ativos no banco</span>
                </div>
              )}
            </div>
          </div>

          <Button asChild size="sm" className="w-full mt-4 h-9 bg-zinc-900 border border-zinc-800 text-zinc-300 font-semibold hover:bg-zinc-800 border-none shadow-[0_4px_15px_rgba(0,0,0,0.5)]">
            <Link href="/email-marketing/pro">Configurar Provedores</Link>
          </Button>
        </div>
      </div>

      {/* Campaigns list & Queue */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <div className="lg:col-span-8 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <div className="flex justify-between items-center mb-4">
            <div>
              <h3 className="text-sm font-bold text-zinc-100">Desempenho por Canal</h3>
              <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mt-0.5">Retorno e atração estimados por via de outreach</p>
            </div>
          </div>
          <div className="space-y-4">
            {channels.map((chan, idx) => (
              <div key={idx} className="p-4 bg-zinc-900/30 border border-zinc-850 rounded-xl space-y-2">
                <div className="flex justify-between items-center text-xs">
                  <span className="font-bold text-zinc-200">{chan.channel}</span>
                  <span className="px-2 py-0.5 font-mono text-[10px] font-bold text-teal-400 bg-teal-500/10 rounded-full">
                    {chan.ctr.toFixed(1)}% CTR
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-2 text-xs">
                  <div>
                    <div className="text-[10px] text-zinc-500 uppercase font-semibold">Disparados / Contatos</div>
                    <div className="font-bold text-zinc-300 mt-0.5">{chan.leads}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-zinc-500 uppercase font-semibold">Investido</div>
                    <div className="font-bold text-zinc-300 mt-0.5">{R(chan.spend)}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-zinc-500 uppercase font-semibold">Valor Estimado</div>
                    <div className="font-bold text-zinc-200 mt-0.5">{R(chan.revenue)}</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Fila recente */}
        <div className="lg:col-span-4 p-5 bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl">
          <h3 className="text-sm font-bold text-zinc-100 mb-1">Envios Recentes</h3>
          <p className="text-[10px] text-zinc-500 font-semibold uppercase tracking-wider mb-4">Últimos itens processados na fila</p>
          
          <div className="space-y-2.5 max-h-80 overflow-y-auto scrollbar-none pr-1">
            {queue.length > 0 ? (
              queue.slice(0, 5).map(item => (
                <div key={item.id} className="p-2.5 bg-zinc-900/20 border border-zinc-850 rounded-lg flex items-center justify-between text-xs">
                  <div className="truncate pr-2 max-w-[70%]">
                    <div className="font-bold text-zinc-200 truncate">{item.recipientName || item.recipientEmail}</div>
                    <div className="text-[9px] text-zinc-500 truncate">{item.subject}</div>
                  </div>
                  <span className={`px-2 py-0.5 text-[9px] font-bold uppercase rounded-full shrink-0 ${
                    item.status === 2 
                      ? 'bg-emerald-500/15 text-emerald-400' 
                      : item.status === 3 
                        ? 'bg-red-500/15 text-red-400' 
                        : 'bg-zinc-800 text-zinc-400'
                  }`}>
                    {item.status === 2 ? 'Enviado' : item.status === 3 ? 'Erro' : 'Fila'}
                  </span>
                </div>
              ))
            ) : (
              <div className="flex flex-col items-center justify-center py-8 text-zinc-500">
                <span className="text-xs font-semibold">Fila de e-mails vazia no momento</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
