'use client'

import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Bot, CheckCircle2, Loader2, Play, RefreshCw, ThumbsUp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  approveSuggestion,
  getNormalizationPreview,
  getNormalizationStats,
  normalizeWithAi,
  runBatchNormalization,
  type NormalizationPreviewItemDto,
  type NormalizationStatsDto,
} from '@/services/leadNormalization'

function ScorePill({ score }: { score: number | null }) {
  if (score == null) return <span className="text-slate-400 text-xs">—</span>
  const cls =
    score >= 80
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

const PAGE_SIZE = 20

export function LeadNormalizationTab() {
  const [stats, setStats]               = useState<NormalizationStatsDto | null>(null)
  const [preview, setPreview]           = useState<NormalizationPreviewItemDto[]>([])
  const [loadingStats, setLoadingStats] = useState(true)
  const [loadingPrev, setLoadingPrev]   = useState(true)
  const [running, setRunning]           = useState(false)
  const [page, setPage]                 = useState(1)

  // Per-row async state: customerId → 'approving' | 'ai' | null
  const [rowBusy, setRowBusy] = useState<Record<string, 'approving' | 'ai' | null>>({})

  // Local overrides applied after approve/AI without re-fetching entire list
  const [localOverrides, setLocalOverrides] = useState<
    Record<string, { name: string; score: number; source: string }>
  >({})

  const loadStats = useCallback(async () => {
    setLoadingStats(true)
    try { setStats(await getNormalizationStats()) }
    catch { toast.error('Erro ao carregar estatísticas.') }
    finally { setLoadingStats(false) }
  }, [])

  const loadPreview = useCallback(async (p: number) => {
    setLoadingPrev(true)
    try { setPreview(await getNormalizationPreview(p, PAGE_SIZE)) }
    catch { toast.error('Erro ao carregar preview.') }
    finally { setLoadingPrev(false) }
  }, [])

  useEffect(() => { loadStats() },        [loadStats])
  useEffect(() => { loadPreview(page) },  [loadPreview, page])

  const handleRun = async (force = false) => {
    setRunning(true)
    try {
      const r = await runBatchNormalization(force)
      toast.success(
        `Concluído: ${r.updated} atualizados, ${r.skipped} sem nome detectável (${r.processed} processados).`,
      )
      setLocalOverrides({})
      await Promise.all([loadStats(), loadPreview(page)])
    } catch { toast.error('Erro ao executar normalização.') }
    finally { setRunning(false) }
  }

  const handleApprove = async (item: NormalizationPreviewItemDto) => {
    const name = localOverrides[item.customerId]?.name ?? item.suggestedName
    if (!name) return
    setRowBusy(p => ({ ...p, [item.customerId]: 'approving' }))
    try {
      await approveSuggestion(item.customerId, name)
      setLocalOverrides(p => ({ ...p, [item.customerId]: { name, score: 100, source: 'Manual' } }))
      toast.success(`"${name}" aprovado.`)
      // Refresh stats silently
      getNormalizationStats().then(setStats).catch(() => {})
    } catch { toast.error('Erro ao aprovar.') }
    finally { setRowBusy(p => ({ ...p, [item.customerId]: null })) }
  }

  const handleAi = async (item: NormalizationPreviewItemDto) => {
    setRowBusy(p => ({ ...p, [item.customerId]: 'ai' }))
    try {
      const result = await normalizeWithAi(item.customerId)
      if (!result.normalizedName) {
        toast.warning('IA não conseguiu identificar um nome para este lead.')
        return
      }
      setLocalOverrides(p => ({
        ...p,
        [item.customerId]: { name: result.normalizedName, score: result.score, source: 'AiFallback' },
      }))
      toast.success(`IA sugeriu "${result.normalizedName}" (score ${result.score}).`)
      getNormalizationStats().then(setStats).catch(() => {})
    } catch { toast.error('Erro ao chamar IA.') }
    finally { setRowBusy(p => ({ ...p, [item.customerId]: null })) }
  }

  const resolvedName  = (item: NormalizationPreviewItemDto) =>
    localOverrides[item.customerId]?.name    ?? item.currentNormalizedName
  const resolvedScore = (item: NormalizationPreviewItemDto) =>
    localOverrides[item.customerId]?.score   ?? item.currentScore
  const resolvedSrc   = (item: NormalizationPreviewItemDto) =>
    localOverrides[item.customerId]?.source  ?? null
  const isApproved    = (item: NormalizationPreviewItemDto) =>
    (resolvedScore(item) ?? 0) === 100 && (resolvedSrc(item) === 'Manual' || localOverrides[item.customerId])

  return (
    <div className="space-y-6">
      {/* Stats row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total de Leads',      value: stats?.total,          color: 'text-slate-800' },
          { label: 'Normalizados',         value: stats?.normalized,     color: 'text-emerald-700',
            sub: `${stats?.coveragePercent ?? 0}% cobertura` },
          { label: 'Pendentes',            value: stats?.pending,        color: 'text-amber-600' },
          { label: 'Alta Confiança (≥80)', value: stats?.highConfidence, color: 'text-blue-700' },
        ].map(card => (
          <div key={card.label} className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
            <p className="text-xs text-slate-500 mb-1">{card.label}</p>
            {loadingStats
              ? <div className="h-7 w-16 animate-pulse rounded bg-slate-100" />
              : <p className={`text-2xl font-bold ${card.color}`}>{card.value}</p>}
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

      {/* Batch actions */}
      <div className="flex items-center gap-3">
        <Button onClick={() => handleRun(false)} disabled={running} size="sm" className="gap-1.5">
          {running ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Play className="h-3.5 w-3.5" />}
          Executar Normalização
        </Button>
        <Button variant="outline" onClick={() => handleRun(true)} disabled={running} size="sm" className="gap-1.5">
          <RefreshCw className="h-3.5 w-3.5" />
          Reprocessar Todos
        </Button>
      </div>

      {/* Preview table */}
      <div>
        <h3 className="text-sm font-semibold text-slate-700 mb-3">Preview de Normalização</h3>
        <div className="rounded-xl border border-slate-200 bg-white overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Nome Atual</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Sugestão</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Score</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Fonte</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Normalizado</th>
                <th className="px-4 py-2 text-left font-medium text-slate-600 text-xs">Ações</th>
              </tr>
            </thead>
            <tbody>
              {loadingPrev
                ? [...Array(5)].map((_, i) => (
                    <tr key={i} className="border-t border-slate-100">
                      {[...Array(6)].map((_, j) => (
                        <td key={j} className="px-4 py-2.5">
                          <div className="h-4 w-20 animate-pulse rounded bg-slate-100" />
                        </td>
                      ))}
                    </tr>
                  ))
                : preview.map((item, i) => {
                    const busy        = rowBusy[item.customerId]
                    const approved    = isApproved(item)
                    const curName     = resolvedName(item)
                    const curScore    = resolvedScore(item)
                    const canApprove  = !approved && (item.suggestedName || localOverrides[item.customerId]?.name)
                    const needsAi     = item.suggestedScore < 70 && !localOverrides[item.customerId]

                    return (
                      <tr
                        key={item.customerId}
                        className={`border-t border-slate-100 ${i % 2 === 0 ? 'bg-white' : 'bg-slate-50/40'}`}
                      >
                        {/* Nome bruto */}
                        <td className="px-4 py-2.5">
                          <span className="text-slate-700">{item.rawName}</span>
                          {item.email && (
                            <span className="block text-[11px] text-slate-400 truncate max-w-[180px]">
                              {item.email}
                            </span>
                          )}
                        </td>

                        {/* Sugestão determinística */}
                        <td className="px-4 py-2.5">
                          {item.suggestedName
                            ? <span className="font-medium text-slate-800">{item.suggestedName}</span>
                            : <span className="text-slate-300 italic text-xs">sem sugestão</span>}
                        </td>

                        {/* Score sugestão */}
                        <td className="px-4 py-2.5">
                          <ScorePill score={item.suggestedScore} />
                        </td>

                        {/* Fonte */}
                        <td className="px-4 py-2.5">
                          <span className="text-xs text-slate-500">{item.suggestedSource}</span>
                        </td>

                        {/* Nome normalizado atual */}
                        <td className="px-4 py-2.5">
                          {curName ? (
                            <span className="flex items-center gap-1 text-xs">
                              <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                              <span className="font-medium text-slate-800">{curName}</span>
                              {curScore != null && <ScorePill score={curScore} />}
                            </span>
                          ) : (
                            <span className="text-slate-300 text-xs">—</span>
                          )}
                        </td>

                        {/* Ações */}
                        <td className="px-4 py-2.5">
                          <div className="flex items-center gap-2">
                            {/* Approved badge */}
                            {approved && (
                              <span className="rounded-full bg-emerald-100 text-emerald-700 px-2 py-0.5 text-[11px] font-medium">
                                Aprovado
                              </span>
                            )}

                            {/* Approve button */}
                            {canApprove && !approved && (
                              <button
                                onClick={() => handleApprove(item)}
                                disabled={!!busy}
                                title="Aprovar sugestão"
                                className="flex items-center gap-1 rounded-md bg-emerald-50 border border-emerald-200 px-2 py-1 text-[11px] font-medium text-emerald-700 hover:bg-emerald-100 disabled:opacity-50"
                              >
                                {busy === 'approving'
                                  ? <Loader2 className="h-3 w-3 animate-spin" />
                                  : <ThumbsUp className="h-3 w-3" />}
                                Aprovar
                              </button>
                            )}

                            {/* AI button */}
                            {needsAi && (
                              <button
                                onClick={() => handleAi(item)}
                                disabled={!!busy}
                                title="Usar IA para extrair nome"
                                className="flex items-center gap-1 rounded-md bg-violet-50 border border-violet-200 px-2 py-1 text-[11px] font-medium text-violet-700 hover:bg-violet-100 disabled:opacity-50"
                              >
                                {busy === 'ai'
                                  ? <Loader2 className="h-3 w-3 animate-spin" />
                                  : <Bot className="h-3 w-3" />}
                                IA
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {!loadingPrev && preview.length > 0 && (
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
