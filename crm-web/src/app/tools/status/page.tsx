'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { getApiBaseUrl } from '@/services/api';
import {
  Activity,
  CheckCircle2,
  Clock,
  ExternalLink,
  RefreshCw,
  WifiOff,
  XCircle,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface ServiceStatus {
  name: string;
  url: string;
  domain: string;
  category: string;
  online: boolean;
  statusCode: number;
  responseTimeMs: number;
}

interface StatusResponse {
  checkedAt: string;
  services: ServiceStatus[];
}

const CATEGORY_ORDER = ['SaaS', 'Website'];

const REFRESH_INTERVAL = 60_000; // 60s

function ResponseBar({ ms, online }: { ms: number; online: boolean }) {
  if (!online) return (
    <div className="flex items-center gap-1.5">
      <div className="h-1.5 w-24 rounded-full bg-red-500/30" />
      <span className="text-xs text-red-400">—</span>
    </div>
  );

  const pct = Math.min((ms / 3000) * 100, 100);
  const color = ms < 500 ? '#22c55e' : ms < 1500 ? '#eab308' : '#f97316';
  return (
    <div className="flex items-center gap-2">
      <div className="relative h-1.5 w-24 rounded-full bg-white/10 overflow-hidden">
        <div
          className="absolute inset-y-0 left-0 rounded-full transition-all duration-700"
          style={{ width: `${pct}%`, backgroundColor: color }}
        />
      </div>
      <span className="text-xs tabular-nums" style={{ color }}>{ms}ms</span>
    </div>
  );
}

function StatusDot({ online }: { online: boolean }) {
  return (
    <span className="relative flex h-2.5 w-2.5 shrink-0">
      {online && (
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-60" />
      )}
      <span
        className={`relative inline-flex h-2.5 w-2.5 rounded-full ${online ? 'bg-green-400' : 'bg-red-500'}`}
      />
    </span>
  );
}

function ServiceCard({ svc }: { svc: ServiceStatus }) {
  return (
    <div
      className={`flex items-center justify-between gap-4 rounded-xl border px-4 py-3 transition-colors ${
        svc.online
          ? 'border-white/8 bg-white/[0.03] hover:bg-white/[0.05]'
          : 'border-red-500/20 bg-red-500/[0.04]'
      }`}
    >
      {/* Left: dot + name + domain */}
      <div className="flex items-center gap-3 min-w-0">
        <StatusDot online={svc.online} />
        <div className="min-w-0">
          <p className="text-sm font-medium text-foreground truncate">{svc.name}</p>
          <p className="text-xs text-muted-foreground truncate">{svc.domain}</p>
        </div>
      </div>

      {/* Right: bar + status + link */}
      <div className="flex items-center gap-4 shrink-0">
        <ResponseBar ms={svc.responseTimeMs} online={svc.online} />

        <Badge
          variant="outline"
          className={`text-xs px-2 py-0 h-5 ${
            svc.online ? 'border-green-500/30 text-green-400' : 'border-red-500/30 text-red-400'
          }`}
        >
          {svc.online
            ? svc.statusCode > 0 ? `${svc.statusCode}` : 'Online'
            : 'Offline'}
        </Badge>

        <a
          href={svc.url}
          target="_blank"
          rel="noopener noreferrer"
          className="text-muted-foreground hover:text-foreground transition-colors"
        >
          <ExternalLink className="h-3.5 w-3.5" />
        </a>
      </div>
    </div>
  );
}

export default function StatusPage() {
  const [data, setData] = useState<StatusResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [countdown, setCountdown] = useState(REFRESH_INTERVAL / 1000);

  const fetchStatus = useCallback(async (silent = false) => {
    if (!silent) setLoading(true);
    else setRefreshing(true);
    setError(null);

    try {
      const token = localStorage.getItem('accessToken') || sessionStorage.getItem('accessToken');
      const res = await fetch(`${getApiBaseUrl()}/status/services`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const json: StatusResponse = await res.json();
      setData(json);
      setCountdown(REFRESH_INTERVAL / 1000);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar status');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  // Initial load
  useEffect(() => { fetchStatus(); }, [fetchStatus]);

  // Auto-refresh
  useEffect(() => {
    const interval = setInterval(() => fetchStatus(true), REFRESH_INTERVAL);
    return () => clearInterval(interval);
  }, [fetchStatus]);

  // Countdown timer
  useEffect(() => {
    const tick = setInterval(() => setCountdown(c => Math.max(0, c - 1)), 1000);
    return () => clearInterval(tick);
  }, [data]);

  // Group services
  const grouped = CATEGORY_ORDER.reduce<Record<string, ServiceStatus[]>>((acc, cat) => {
    const items = data?.services.filter(s => s.category === cat) ?? [];
    if (items.length) acc[cat] = items;
    return acc;
  }, {});

  const total = data?.services.length ?? 0;
  const onlineCount = data?.services.filter(s => s.online).length ?? 0;
  const allOnline = total > 0 && onlineCount === total;
  const anyOffline = onlineCount < total;

  const checkedAt = data?.checkedAt ? new Date(data.checkedAt) : null;

  return (
    <div className="space-y-6 max-w-3xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Badge variant="section">Monitoramento</Badge>
          </div>
          <h1 className="text-2xl font-serif tracking-tight">Status das Aplicações</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Visibilidade em tempo real de todos os SaaS e sites ativos
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => fetchStatus(true)}
          disabled={refreshing || loading}
          className="gap-2"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${refreshing ? 'animate-spin' : ''}`} />
          Atualizar
        </Button>
      </div>

      {/* Summary bar */}
      {!loading && !error && data && (
        <div
          className={`rounded-xl border px-5 py-4 flex items-center justify-between ${
            allOnline
              ? 'border-green-500/20 bg-green-500/[0.04]'
              : anyOffline
              ? 'border-red-500/20 bg-red-500/[0.04]'
              : 'border-white/8 bg-white/[0.03]'
          }`}
        >
          <div className="flex items-center gap-3">
            {allOnline ? (
              <CheckCircle2 className="h-5 w-5 text-green-400" />
            ) : (
              <XCircle className="h-5 w-5 text-red-400" />
            )}
            <div>
              <p className={`font-medium text-sm ${allOnline ? 'text-green-400' : 'text-red-400'}`}>
                {allOnline
                  ? 'Todos os sistemas operacionais'
                  : `${total - onlineCount} sistema${total - onlineCount > 1 ? 's' : ''} fora do ar`}
              </p>
              <p className="text-xs text-muted-foreground">
                {onlineCount}/{total} online
              </p>
            </div>
          </div>
          <div className="flex items-center gap-4 text-xs text-muted-foreground">
            {checkedAt && (
              <span className="flex items-center gap-1.5">
                <Clock className="h-3 w-3" />
                {checkedAt.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
              </span>
            )}
            <span className="flex items-center gap-1.5">
              <Activity className="h-3 w-3" />
              Próxima em {countdown}s
            </span>
          </div>
        </div>
      )}

      {/* Loading */}
      {loading && (
        <div className="space-y-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-14 rounded-xl bg-white/[0.03] animate-pulse border border-white/8" />
          ))}
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="rounded-xl border border-red-500/20 bg-red-500/[0.04] px-5 py-4 flex items-center gap-3">
          <WifiOff className="h-5 w-5 text-red-400 shrink-0" />
          <p className="text-sm text-red-400">{error}</p>
        </div>
      )}

      {/* Services grouped */}
      {!loading && !error && data && (
        <div className="space-y-6">
          {Object.entries(grouped).map(([category, services]) => (
            <div key={category} className="space-y-2">
              <div className="flex items-center gap-2">
                <h2 className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">
                  {category}
                </h2>
                <div className="flex-1 h-px bg-white/8" />
                <span className="text-xs text-muted-foreground">
                  {services.filter(s => s.online).length}/{services.length}
                </span>
              </div>
              <div className="space-y-1.5">
                {services.map(svc => <ServiceCard key={svc.name} svc={svc} />)}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
