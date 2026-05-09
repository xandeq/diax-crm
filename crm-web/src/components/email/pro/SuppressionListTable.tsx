'use client'

import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Trash2, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  getSuppressions,
  addEmailSuppression,
  addDomainSuppression,
  removeSuppression,
  type SuppressionDto,
} from '@/services/emailSuppression'

const REASON_BADGE: Record<string, string> = {
  HardBounce:    'bg-red-100 text-red-700',
  SpamComplaint: 'bg-orange-100 text-orange-700',
  ManualOptOut:  'bg-slate-100 text-slate-600',
  InvalidDomain: 'bg-yellow-100 text-yellow-700',
  UserListImport:'bg-blue-100 text-blue-700',
}

export function SuppressionListTable() {
  const [suppressions, setSuppressions] = useState<SuppressionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [input, setInput] = useState('')
  const [adding, setAdding] = useState(false)

  const load = () =>
    getSuppressions()
      .then(setSuppressions)
      .catch(() => toast.error('Erro ao carregar lista de supressao.'))
      .finally(() => setLoading(false))

  useEffect(() => { load() }, [])

  const handleAdd = async () => {
    const value = input.trim()
    if (!value) return
    setAdding(true)
    try {
      if (value.startsWith('@') || !value.includes('@')) {
        const domain = value.startsWith('@') ? value.slice(1) : value
        await addDomainSuppression(domain)
        toast.success(`Dominio @${domain} suprimido.`)
      } else {
        await addEmailSuppression(value)
        toast.success(`Email ${value} suprimido.`)
      }
      setInput('')
      await load()
    } catch {
      toast.error('Erro ao adicionar supressao.')
    } finally {
      setAdding(false)
    }
  }

  const handleRemove = async (id: string) => {
    try {
      await removeSuppression(id)
      setSuppressions(s => s.filter(x => x.id !== id))
      toast.success('Supressao removida.')
    } catch {
      toast.error('Erro ao remover supressao.')
    }
  }

  if (loading) {
    return (
      <div className="space-y-2">
        {[...Array(4)].map((_, i) => <Skeleton key={i} className="h-10 rounded" />)}
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Add form */}
      <div className="flex gap-2">
        <Input
          value={input}
          onChange={e => setInput(e.target.value)}
          placeholder="email@exemplo.com ou @dominio.com"
          className="max-w-sm"
          onKeyDown={e => e.key === 'Enter' && handleAdd()}
        />
        <Button onClick={handleAdd} disabled={adding || !input.trim()} size="sm">
          <Plus className="h-4 w-4 mr-1" />
          {adding ? 'Adicionando...' : 'Suprimir'}
        </Button>
      </div>

      <p className="text-xs text-slate-400">
        Emails nesta lista nao recebem campanhas. Use @dominio.com para suprimir um dominio inteiro.
        {' '}Total: <strong>{suppressions.length}</strong>
      </p>

      {suppressions.length === 0 ? (
        <p className="text-slate-400 text-sm py-4 text-center">Nenhuma supressao configurada.</p>
      ) : (
        <div className="border rounded-md overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="px-4 py-2 text-left font-medium">Email / Dominio</th>
                <th className="px-4 py-2 text-left font-medium">Motivo</th>
                <th className="px-4 py-2 text-left font-medium">Fonte</th>
                <th className="px-4 py-2 text-left font-medium">Data</th>
                <th className="px-4 py-2 w-10" />
              </tr>
            </thead>
            <tbody>
              {suppressions.map((s, i) => (
                <tr
                  key={s.id}
                  className={`border-t ${i % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'}`}
                >
                  <td className="px-4 py-2 font-mono text-xs">
                    {s.email ?? `@${s.domainPattern}`}
                  </td>
                  <td className="px-4 py-2">
                    <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${REASON_BADGE[s.reason] ?? 'bg-slate-100 text-slate-600'}`}>
                      {s.reason}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-slate-500 text-xs">{s.source}</td>
                  <td className="px-4 py-2 text-slate-400 text-xs">
                    {new Date(s.suppressedAt).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-2">
                    <button
                      onClick={() => handleRemove(s.id)}
                      className="text-slate-400 hover:text-red-500 transition-colors"
                      title="Remover supressao"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
