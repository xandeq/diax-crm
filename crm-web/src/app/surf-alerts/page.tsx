'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { SurfAlertResponse, surfAlertsService } from '@/services/surfAlerts';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, Waves } from 'lucide-react';

function formatDate(iso: string): string {
  try {
    return new Date(iso + (iso.length === 10 ? 'T00:00:00' : '')).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
    });
  } catch {
    return iso;
  }
}

function AlertCard({ alert }: { alert: SurfAlertResponse }) {
  const bw = alert.bestWindow;

  return (
    <Card className="border-white/10 bg-zinc-900/40">
      <CardHeader className="flex flex-row items-start justify-between gap-4 pb-3">
        <div>
          <CardTitle className="text-base text-zinc-100">
            Fim de semana {formatDate(alert.weekendSaturday)}–{formatDate(alert.weekendSunday)}
          </CardTitle>
          <p className="text-xs text-zinc-500 mt-1">
            {alert.source} · recebido em {new Date(alert.createdAt).toLocaleString('pt-BR')}
          </p>
        </div>
        <Badge
          className={
            alert.hasGoodWindow
              ? 'bg-emerald-500/15 text-emerald-400 border-emerald-500/20'
              : 'bg-zinc-500/15 text-zinc-400 border-zinc-500/20'
          }
        >
          {alert.hasGoodWindow ? 'Janela boa' : 'Sem janela boa'}
        </Badge>
      </CardHeader>
      <CardContent className="space-y-3">
        <p className="text-sm text-zinc-300">{alert.summary}</p>

        {bw && (
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs text-zinc-400 bg-black/20 rounded-lg p-3">
            <div>
              <div className="text-zinc-500">Spot</div>
              <div className="text-zinc-200">{bw.spot ?? '—'}</div>
            </div>
            <div>
              <div className="text-zinc-500">Horário</div>
              <div className="text-zinc-200">{bw.start ?? '?'}–{bw.end ?? '?'}</div>
            </div>
            <div>
              <div className="text-zinc-500">Ondas</div>
              <div className="text-zinc-200">
                {bw.wave_height_m?.min ?? '?'}–{bw.wave_height_m?.max ?? '?'} m
              </div>
            </div>
            <div>
              <div className="text-zinc-500">Vento</div>
              <div className="text-zinc-200">
                {bw.wind?.speed_kmh ?? '?'} km/h {bw.wind?.direction ?? ''}
              </div>
            </div>
            {bw.tide && (
              <div className="col-span-2 sm:col-span-4">
                <div className="text-zinc-500">Maré</div>
                <div className="text-zinc-200">{bw.tide}</div>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

export default function SurfAlertsPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['surf-alerts'],
    queryFn: () => surfAlertsService.getRecent(20),
  });

  const errorMessage = isError ? (error instanceof Error ? error.message : 'Erro ao carregar alertas') : null;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-4">
        <div className="flex items-center gap-3">
          <Waves className="h-6 w-6 text-cyan-400" />
          <div>
            <h1 className="text-2xl font-bold text-zinc-100">Alertas de Surf</h1>
            <p className="text-zinc-400 mt-1 text-sm">
              Previsão semanal (ondas + agenda) recebida da automação de surf
            </p>
          </div>
        </div>
        <Button
          onClick={() => refetch()}
          variant="outline"
          disabled={isFetching}
          className="border-white/10 hover:bg-white/5 text-zinc-300"
        >
          <RefreshCw className={`h-4 w-4 mr-2 ${isFetching ? 'animate-spin' : ''}`} />
          Atualizar
        </Button>
      </div>

      {errorMessage && (
        <div className="text-sm text-rose-400 bg-rose-500/10 border border-rose-500/20 rounded-lg p-3">
          {errorMessage}
        </div>
      )}

      {isLoading && (
        <div className="space-y-3">
          <Skeleton className="h-28 w-full" />
          <Skeleton className="h-28 w-full" />
        </div>
      )}

      {!isLoading && data && data.length === 0 && (
        <p className="text-sm text-zinc-500">Nenhum alerta recebido ainda.</p>
      )}

      {!isLoading && data && data.length > 0 && (
        <div className="space-y-3">
          {data.map((alert) => (
            <AlertCard key={alert.id} alert={alert} />
          ))}
        </div>
      )}
    </div>
  );
}
