'use client'

import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'
import { CheckCircle2, Loader2, Play, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  getNormalizationStats,
  getNormalizationPreview,
  runBatchNormalization,
  type NormalizationStatsDto,
  type NormalizationPreviewItemDto,
} from '@/services/leadNormalization'

function scoreColor(score: number | null): string {
  if (score == null) return 'text-slate-400'
  if (score >= 80) return 'text-emerald-600'
  if (score >= 60) return 'text-amber-600'
  return 'text-red-500'
}

function ScorePill({ score }: { score: number | null }) {
  if (score == null) return <span className="text-slate-400 text-xs">—</span>
  const cls = score >= 80
    ? 'bg-emerald-100 text-emerald-700'
    : score >= 60
    ? 'bg-amber-100 text-amber-700'
    : 'bg-red-100 text-red-600'
  return (
    <span className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${cls}`}>
      {score}%
    </span>
  )
}

export function LeadNormalizationTab() {
  const [stats, setStats] = useState<NormalizationStatsDto | null>(null)
  const [preview, setPreview] = useState<NormalizationPreviewItemDto[]>([])
  const [loadingStats, setLoadingStats] = useState(true)
  const [loadingPreview, setLoadingPreview] = useState(true)
  const [running, setRunning] = useState(false)
  const [page, setPage] = useState(1)
  const PAGE_SIZE = 20

  const loadStats = useCallback(async () => {
    setLoadingStats(true)
    try {
      setStats(await getNormalizationStats())
    } catch {
      toast.error('Erro ao carregar estatísticas de normalização.')
    } finally {
      setLoadingStats(false)
    }
  }, [])

  const loadPreview = useCallback(async (p: number) => {
    setLoadingPreview(true)
    try {
      setPreview(await getNormalizationPreview(p, PAGE_SIZE))
    } catch {
      toast.error('Erro ao carregar preview de normalização.')
    } finally {
      setLoadingPreview(false)
    }
  }, [])

  useEffect(() => { loadStats() }, [loadStats])
  useEffect(() => { loadPreview(page) }, [loadPreview, page])

  const handleRun = async (force = false) => {
    setRunning(true)
    try {
      const result = await runBatchNormalization(force)
      toast.success(
        `Normalização concluída: ${result.updated} atualizados, ${result.skipped} ignorados (de ${result.processed} processados).`
      )
      await Promise.all([loadStats(), loadPreview(page)])
    } catch {
      toast.error('Erro ao executar normalização em lote.')
    } finally {
      setRunning(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Stats row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          {
            label: 'Total de Leads',
            value: loadingStats ? null : stats?.total,
            sub: '',
            color: 'text-slate-800',
          },
          {
            label: 'Normalizados',
            value: loadingStats ? null : stats?.normalized,
            sub: loadingStats ? '' : `${stats?.coveragePercent ?? 0}% cobertura`,
            color: 'text-emerald-700',
          },
          {
            label: 'Pendentes',
            value: loadingStats ? null : stats?.pending,
            sub: '',
            color: 'text-amber-600',
          },
          {
            label: 'Alta Confiança (≥80)',
            value: loadingStats ? null : stats?.highConfidence,
            sub: '',
            color: 'text-blue-700',
          },
        ].map(card => (
          <div
            key={card.label}
            className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm"
          >
            <p className="text-xs text-slate-500 mb-1">{card.label}</p>
            {loadingStats ? (
              <div className="h-7 w-16 animate-pulse rounded bg-slate-100" />
            ) : (
              <p className={`text-2xl font-bold ${card.color}`}>{card.value}</p>
            )}
            {card.sub && !loadingStats && (
              <p className="text-[11px] text-slate-400 mt-0.5">{card.sub}</p>
            )}
          </div>
        ))}
      </div>

      {/* Coverage bar */}
      {!loadingStats && stats && (
        <div>
          <div className="flex justify-between text-xs text-slate-500 mb-1">
            <span>Cobertura de normalização</span>
            <span>{stats.coveragePercent}%</span>
          </div>
          <div className="h-2 w-full rounded-full bg-slate-100 overflow-hidden">
            <div
              className="h-full rounded-full bg-emerald-500 transition-all duration-700"
              style={{ width: `${Math.min(stats.coveragePercent, 100)}%` }}
            />
          </div>
        </div>
      )}

      {/* Action buttons */}
      <div className="flex items-center gap-3">
        <Button
          onClick={() => handleRun(false)}
          disabled={running}
          size="sm"
          className="gap-1.5"
        >
          {running ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Play className="h-3.5 w-3.5" />}
          Executar Normalização
        </Button>
        <Button
          variant="outline"
          onClick={() => handleRun(true)}
          disabled={running}
          size="sm"
          className="gap-1.5"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          Reprocessar Todos
        </Button>
      </div>

      {/* Preview table */}
      <div>
        <h3 className="text-sm font-semibold text-slate-700 mb-3">
          Preview de Normalização
        </h3>
        <div className="rounded-xl border border-slate-200 bg-white overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Nome Atual</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Sugestão</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Score</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Fonte</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Normalizado</th>
              </tr>
            </thead>
            <tbody>
              {loadingPreview
                ? [...Array(5)].map((_, i) => (
                    <tr key={i} className="border-t border-slate-100">
                      {[...Array(5)].map((_, j) => (
                        <td key={j} className="px-4 py-2.5">
                          <div className="h-4 w-24 animate-pulse rounded bg-slate-100" />
                        </td>
                      ))}
                    </tr>
                  ))
                : preview.map((item, i) => (
                    <tr
                      key={item.customerId}
                      className={`border-t border-slate-100 ${i % 2 === 0 ? 'bg-white' : 'bg-slate-50/40'}`}
                    >
                      <td className="px-4 py-2.5">
                        <span className="text-slate-700">{item.rawName}</span>
                        {item.email && (
                          <span className="block text-[11px] text-slate-400 truncate max-w-[200px]">
                            {item.email}
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-2.5">
                        {item.suggestedName ? (
                          <span className="font-medium text-slate-800">{item.suggestedName}</span>
                        ) : (
                          <span className="text-slate-300 italic text-xs">sem sugestão</span>
                        )}
                      </td>
                      <td className="px-4 py-2.5">
                        <ScorePill score={item.suggestedScore} />
                      </td>
                      <td className="px-4 py-2.5">
                        <span className="text-xs text-slate-500">{item.suggestedSource}</span>
                      </td>
                      <td className="px-4 py-2.5">
                        {item.currentNormalizedName ? (
                          <span className="flex items-center gap-1 text-emerald-600 text-xs">
                            <CheckCircle2 className="h-3.5 w-3.5" />
                            {item.currentNormalizedName}
                          </span>
                        ) : (
                          <span className="text-slate-300 text-xs">—</span>
                        )}
                      </td>
                    </tr>
                  ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {!loadingPreview && preview.length > 0 && (
          <div className="flex items-center gap-3 mt-3 text-sm text-slate-500">
            <button
              disabled={page <= 1}
              onClick={() => setPage(p => p - 1)}
              className="px-2 py-1 rounded border border-slate-200 disabled:opacity-40 hover:bg-slate-50"
            >
              ← Anterior
            </button>
            <span>Página {page}</span>
            <button
              disabled={preview.length < PAGE_SIZE}
              onClick={() => setPage(p => p + 1)}
              className="px-2 py-1 rounded border border-slate-200 disabled:opacity-40 hover:bg-slate-50"
            >
              Próxima →
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
