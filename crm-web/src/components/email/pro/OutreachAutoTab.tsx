'use client'

import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { getOutreachDashboard, sendOutreachCampaign, type OutreachDashboardResponse } from '@/services/outreach'

export function OutreachAutoTab() {
  const [dashboard, setDashboard] = useState<OutreachDashboardResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [running, setRunning] = useState(false)

  useEffect(() => {
    getOutreachDashboard()
      .then(setDashboard)
      .catch(() => toast.error('Erro ao carregar dashboard de outreach.'))
      .finally(() => setLoading(false))
  }, [])

  const handleRun = async () => {
    setRunning(true)
    try {
      await sendOutreachCampaign()
      toast.success('Outreach automático executado com sucesso!')
      const fresh = await getOutreachDashboard()
      setDashboard(fresh)
    } catch {
      toast.error('Erro ao executar outreach.')
    } finally {
      setRunning(false)
    }
  }

  if (loading) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-24 rounded-lg" />)}
      </div>
    )
  }

  if (!dashboard) return null

  const stats = [
    { label: 'Emails hoje',    value: dashboard.emailsSentToday ?? 0 },
    { label: 'Emails semana',  value: dashboard.emailsSentThisWeek ?? 0 },
    { label: 'Na fila',        value: dashboard.pendingInQueue ?? 0 },
    { label: 'Leads Hot',      value: dashboard.hotLeads ?? 0 },
  ]

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {stats.map(s => (
          <Card key={s.label}>
            <CardHeader className="pb-1">
              <CardTitle className="text-xs text-slate-500 font-medium">{s.label}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{s.value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="flex items-center gap-4">
        <Button onClick={handleRun} disabled={running}>
          {running ? 'Executando...' : 'Executar Outreach Agora'}
        </Button>
        <p className="text-sm text-slate-500">
          O outreach automático segmenta leads e envia emails com cooldown de 7 dias.
          Para configuração completa, acesse{' '}
          <a href="/outreach" className="text-blue-600 underline">
            /outreach
          </a>.
        </p>
      </div>
    </div>
  )
}
