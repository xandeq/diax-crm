'use client'

import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import type { PreselectedLeadDto } from '@/services/emailProviders'

const PROVIDER_BADGE: Record<string, string> = {
  Brevo:   'bg-blue-100 text-blue-700',
  Mailjet: 'bg-orange-100 text-orange-700',
  Resend:  'bg-violet-100 text-violet-700',
}

const SEGMENT_BADGE: Record<string, string> = {
  Hot:  'bg-red-100 text-red-700',
  Warm: 'bg-yellow-100 text-yellow-700',
}

interface Props {
  leads: PreselectedLeadDto[]
  onSelectionChange: (selected: PreselectedLeadDto[]) => void
}

export function SmartPreselectionTable({ leads, onSelectionChange }: Props) {
  const [excluded, setExcluded] = useState<Set<string>>(new Set())

  const toggle = (id: string) => {
    const next = new Set(excluded)
    if (next.has(id)) next.delete(id)
    else next.add(id)
    setExcluded(next)
    onSelectionChange(leads.filter(l => !next.has(l.customerId)))
  }

  const providerGroups = ['Brevo', 'Mailjet', 'Resend']
  const counts = providerGroups.map(p => ({
    name: p,
    total: leads.filter(l => l.assignedProvider === p).length,
    selected: leads.filter(l => l.assignedProvider === p && !excluded.has(l.customerId)).length,
  }))

  return (
    <div className="space-y-3">
      <div className="flex gap-3 flex-wrap">
        {counts.map(c => (
          <div key={c.name} className={`rounded px-2 py-1 text-xs font-medium ${PROVIDER_BADGE[c.name] ?? 'bg-slate-100 text-slate-700'}`}>
            {c.name}: {c.selected}/{c.total}
          </div>
        ))}
        <div className="text-xs text-slate-500 self-center ml-auto">
          {leads.length - excluded.size} selecionado(s)
        </div>
      </div>

      <div className="border rounded-md overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600">
            <tr>
              <th className="w-8 px-3 py-2" />
              <th className="px-3 py-2 text-left font-medium">Nome / Email</th>
              <th className="px-3 py-2 text-left font-medium">Segmento</th>
              <th className="px-3 py-2 text-left font-medium">Provider</th>
              <th className="px-3 py-2 text-left font-medium">Motivo</th>
            </tr>
          </thead>
          <tbody>
            {leads.map((lead, i) => {
              const isExcluded = excluded.has(lead.customerId)
              return (
                <tr
                  key={lead.customerId}
                  className={`border-t transition-colors ${isExcluded ? 'bg-slate-50 opacity-50' : i % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'}`}
                >
                  <td className="px-3 py-2">
                    <Checkbox
                      checked={!isExcluded}
                      onCheckedChange={() => toggle(lead.customerId)}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <div className="font-medium">{lead.firstName}</div>
                    <div className="text-xs text-slate-400">{lead.email}</div>
                  </td>
                  <td className="px-3 py-2">
                    <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${SEGMENT_BADGE[lead.segmentLabel] ?? 'bg-slate-100 text-slate-600'}`}>
                      {lead.segmentLabel}
                    </span>
                  </td>
                  <td className="px-3 py-2">
                    <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${PROVIDER_BADGE[lead.assignedProvider] ?? 'bg-slate-100'}`}>
                      {lead.assignedProvider}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 max-w-[220px] truncate">
                    {lead.reasonForSelection}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
