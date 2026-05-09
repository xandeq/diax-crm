'use client'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import type { ProviderHealthDto } from '@/services/emailProviders'

const PROVIDER_COLORS: Record<string, string> = {
  Brevo: 'text-blue-600',
  Mailjet: 'text-orange-500',
  Resend: 'text-violet-600',
}

const HEALTH_BADGE: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' }> = {
  ok:       { label: 'Saudável', variant: 'default' },
  degraded: { label: 'Degradado', variant: 'secondary' },
  down:     { label: 'Limite atingido', variant: 'destructive' },
}

interface Props {
  data: ProviderHealthDto
}

export function ProviderHealthCard({ data }: Props) {
  const color = PROVIDER_COLORS[data.provider] ?? 'text-slate-600'
  const badge = HEALTH_BADGE[data.health] ?? HEALTH_BADGE.ok

  return (
    <Card>
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className={`text-base font-semibold ${color}`}>{data.provider}</CardTitle>
          <Badge variant={badge.variant} className="text-xs">{badge.label}</Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div>
          <div className="flex justify-between text-xs text-slate-500 mb-1">
            <span>Hoje</span>
            <span>{data.sentToday} / {data.dailyLimit}</span>
          </div>
          <Progress value={data.dailyUsagePercent} className="h-2" />
          <p className="text-xs text-slate-400 mt-1">{data.dailyRemaining} restantes hoje</p>
        </div>
        <div className="flex justify-between text-xs text-slate-500">
          <span>Esta hora</span>
          <span>{data.sentThisHour} / {data.hourlyLimit} ({data.hourlyRemaining} rest.)</span>
        </div>
      </CardContent>
    </Card>
  )
}
