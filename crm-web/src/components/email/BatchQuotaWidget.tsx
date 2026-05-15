'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { AlertTriangle, CheckCircle2, Clock, Loader2, RefreshCw, XCircle } from 'lucide-react'
import { getProviderHealth, type ProviderHealthDto } from '@/services/emailProviders'

const PROVIDER_ACCENT: Record<string, { bar: string; text: string; bg: string }> = {
  Brevo:        { bar: 'bg-blue-500',   text: 'text-blue-700',   bg: 'bg-blue-50' },
  Mailjet:      { bar: 'bg-orange-500', text: 'text-orange-700', bg: 'bg-orange-50' },
  Resend:       { bar: 'bg-violet-500', text: 'text-violet-700', bg: 'bg-violet-50' },
  SendGrid:     { bar: 'bg-teal-500',   text: 'text-teal-700',   bg: 'bg-teal-50' },
  MailerSend:   { bar: 'bg-green-500',  text: 'text-green-700',  bg: 'bg-green-50' },
  ElasticEmail: { bar: 'bg-rose-500',   text: 'text-rose-700',   bg: 'bg-rose-50' },
}

function barColor(pct: number) {
  if (pct >= 100) return 'bg-red-500'
  if (pct >= 80)  return 'bg-amber-400'
  return 'bg-emerald-500'
}

function useCountdown() {
  const [label, setLabel] = useState('')
  useEffect(() => {
    function tick() {
      const now = new Date()
      const midnight = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1))
      const diff = midnight.getTime() - now.getTime()
      const h = Math.floor(diff / 3_600_000)
      const m = Math.floor((diff % 3_600_000) / 60_000)
      const s = Math.floor((diff % 60_000) / 1_000)
      setLabel(`${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}:${String(s).padStart(2,'0')}`)
    }
    tick()
    const id = setInterval(tick, 1_000)
    return () => clearInterval(id)
  }, [])
  return label
}

export function BatchQuotaWidget() {
  const [data, setData] = useState<ProviderHealthDto[]>([])
  const [loading, setLoading] = useState(true)
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const countdown = useCountdown()

  const load = useCallback(async () => {
    try {
      const result = await getProviderHealth()
      setData(result)
      setLastRefresh(new Date())
    } catch {
      // silently keep stale data on refresh failures
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    load()
    timerRef.current = setInterval(load, 60_000)
    return () => { if (timerRef.current) clearInterval(timerRef.current) }
  }, [load])

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-slate-400 text-sm py-3">
        <Loader2 className="h-4 w-4 animate-spin" />
        Carregando quota de envio...
      </div>
    )
  }

  if (data.length === 0) return null

  const totalSent      = data.reduce((s, p) => s + p.sentToday, 0)
  const totalLimit     = data.reduce((s, p) => s + p.dailyLimit, 0)
  const totalRemaining = data.reduce((s, p) => s + p.dailyRemaining, 0)
  const totalQueued    = data.reduce((s, p) => s + p.queuedCount, 0)
  const totalFailed    = data.reduce((s, p) => s + p.failedToday, 0)
  const totalPct       = totalLimit > 0 ? Math.round(totalSent * 100 / totalLimit) : 0
  const anyDown        = data.some(p => p.health === 'down')
  const anyDegraded    = data.some(p => p.health === 'degraded')

  return (
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
      {/* Header bar */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-100 bg-slate-50">
        <div className="flex items-center gap-2">
          {anyDown
            ? <XCircle className="h-4 w-4 text-red-500" />
            : anyDegraded
              ? <AlertTriangle className="h-4 w-4 text-amber-500" />
              : <CheckCircle2 className="h-4 w-4 text-emerald-500" />}
          <span className="text-sm font-semibold text-slate-700">Quota de Envio Diária</span>
        </div>
        <div className="flex items-center gap-3 text-xs text-slate-400">
          <span className="flex items-center gap-1">
            <Clock className="h-3.5 w-3.5" />
            Reset em <span className="font-mono font-medium text-slate-600 ml-1">{countdown}</span>
          </span>
          <button
            onClick={load}
            title="Atualizar agora"
            className="hover:text-slate-600 transition-colors"
          >
            <RefreshCw className="h-3.5 w-3.5" />
          </button>
          {lastRefresh && (
            <span title={lastRefresh.toLocaleTimeString('pt-BR')}>
              atualizado {lastRefresh.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
            </span>
          )}
        </div>
      </div>

      <div className="px-4 py-3 space-y-4">
        {/* Global total bar */}
        <div>
          <div className="flex items-end justify-between mb-1.5">
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-bold text-slate-800">{totalSent}</span>
              <span className="text-sm text-slate-400">/ {totalLimit} enviados hoje</span>
            </div>
            <div className="flex items-center gap-3 text-xs">
              {totalQueued > 0 && (
                <span className="flex items-center gap-1 text-blue-600 font-medium">
                  <span className="inline-block w-2 h-2 rounded-full bg-blue-400" />
                  {totalQueued} na fila
                </span>
              )}
              {totalFailed > 0 && (
                <span className="flex items-center gap-1 text-red-500 font-medium">
                  <span className="inline-block w-2 h-2 rounded-full bg-red-400" />
                  {totalFailed} com erro
                </span>
              )}
              <span className="text-slate-500 font-medium">{totalRemaining} restantes</span>
            </div>
          </div>
          {/* Total progress bar */}
          <div className="h-3 w-full rounded-full bg-slate-100 overflow-hidden">
            <div
              className={`h-full rounded-full transition-all duration-500 ${barColor(totalPct)}`}
              style={{ width: `${Math.min(totalPct, 100)}%` }}
            />
          </div>
          <div className="flex justify-between mt-1 text-[11px] text-slate-400">
            <span>{totalPct}% usado</span>
            <span className="font-mono">{totalSent}/{totalLimit}</span>
          </div>
        </div>

        {/* Divider */}
        <div className="border-t border-slate-100" />

        {/* Per-provider rows */}
        <div className="space-y-3">
          {data.map(p => {
            const accent = PROVIDER_ACCENT[p.provider] ?? { bar: 'bg-slate-400', text: 'text-slate-600', bg: 'bg-slate-50' }
            const pct = Math.min(p.dailyUsagePercent, 100)

            return (
              <div key={p.provider} className="space-y-1">
                <div className="flex items-center justify-between text-xs">
                  <div className="flex items-center gap-2">
                    <span className={`font-semibold ${accent.text}`}>{p.provider}</span>
                    {p.health === 'down' && (
                      <span className="rounded-full bg-red-100 text-red-600 px-1.5 py-0.5 text-[10px] font-medium">Limite atingido</span>
                    )}
                    {p.health === 'degraded' && (
                      <span className="rounded-full bg-amber-100 text-amber-600 px-1.5 py-0.5 text-[10px] font-medium">Quase no limite</span>
                    )}
                  </div>
                  <div className="flex items-center gap-3 text-slate-500">
                    {p.queuedCount > 0 && (
                      <span className="text-blue-500">{p.queuedCount} na fila</span>
                    )}
                    {p.failedToday > 0 && (
                      <span className="text-red-400">{p.failedToday} erro{p.failedToday > 1 ? 's' : ''}</span>
                    )}
                    <span>
                      <span className="font-medium text-slate-700">{p.sentToday}</span>
                      <span className="text-slate-400">/{p.dailyLimit}</span>
                    </span>
                    <span className="text-slate-400">·</span>
                    <span className="text-slate-500">{p.dailyRemaining} rest.</span>
                    <span className="text-slate-300">|</span>
                    <span title="Enviados esta hora" className="text-slate-400">{p.sentThisHour}/{p.hourlyLimit} /h</span>
                  </div>
                </div>
                <div className="h-1.5 w-full rounded-full bg-slate-100 overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all duration-500 ${accent.bar}`}
                    style={{ width: `${pct}%` }}
                  />
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
