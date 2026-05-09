'use client'

import { useEffect, useState } from 'react'
import { toast } from 'sonner'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Skeleton } from '@/components/ui/skeleton'
import { ProviderHealthCard } from '@/components/email/pro/ProviderHealthCard'
import { CampaignWizard } from '@/components/email/pro/CampaignWizard'
import { OutreachAutoTab } from '@/components/email/pro/OutreachAutoTab'
import { getProviderHealth, type ProviderHealthDto } from '@/services/emailProviders'
import { getEmailCampaigns, type EmailCampaignResponse } from '@/services/emailMarketing'
import { SuppressionListTable } from '@/components/email/pro/SuppressionListTable'

function RecentCampaigns({ campaigns }: { campaigns: EmailCampaignResponse[] }) {
  if (campaigns.length === 0) {
    return <p className="text-slate-400 text-sm py-2">Nenhuma campanha recente.</p>
  }
  return (
    <div className="border rounded-md overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-slate-50 text-slate-600">
          <tr>
            <th className="px-4 py-2 text-left font-medium">Nome</th>
            <th className="px-4 py-2 text-left font-medium">Status</th>
            <th className="px-4 py-2 text-right font-medium">Enviados</th>
            <th className="px-4 py-2 text-right font-medium">Abertos</th>
          </tr>
        </thead>
        <tbody>
          {campaigns.map((c, i) => (
            <tr key={c.id} className={`border-t ${i % 2 === 0 ? 'bg-white' : 'bg-slate-50/50'}`}>
              <td className="px-4 py-2 font-medium text-slate-800">{c.name}</td>
              <td className="px-4 py-2 text-slate-500 capitalize">{c.status}</td>
              <td className="px-4 py-2 text-right">{c.sentCount ?? 0}</td>
              <td className="px-4 py-2 text-right">{c.openCount ?? 0}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function DashboardTab() {
  const [health, setHealth] = useState<ProviderHealthDto[]>([])
  const [campaigns, setCampaigns] = useState<EmailCampaignResponse[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([getProviderHealth(), getEmailCampaigns(1, 5)])
      .then(([h, c]) => {
        setHealth(h)
        setCampaigns(c.items ?? [])
      })
      .catch(() => toast.error('Erro ao carregar dashboard PRO.'))
      .finally(() => setLoading(false))
  }, [])

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="grid grid-cols-3 gap-4">
          {[...Array(3)].map((_, i) => <Skeleton key={i} className="h-36 rounded-lg" />)}
        </div>
        <Skeleton className="h-48 rounded-lg" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide mb-3">
          Saúde dos Providers
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {health.map(h => <ProviderHealthCard key={h.provider} data={h} />)}
        </div>
      </div>

      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold text-slate-500 uppercase tracking-wide">
            Campanhas Recentes
          </h2>
          <a href="/campanhas" className="text-xs text-blue-600 hover:underline">
            Ver todas →
          </a>
        </div>
        <RecentCampaigns campaigns={campaigns} />
      </div>
    </div>
  )
}

export default function EmailMarketingProPage() {
  const [refreshKey, setRefreshKey] = useState(0)

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="max-w-6xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-slate-900">Email Marketing PRO</h1>
            <span className="rounded bg-blue-600 px-2 py-0.5 text-xs font-bold text-white">PRO</span>
          </div>
          <p className="text-slate-500 text-sm mt-1">
            Smart preselection, provider health e envio com tracking completo.
          </p>
        </div>

        <Tabs defaultValue="dashboard">
          <TabsList className="mb-6">
            <TabsTrigger value="dashboard">Dashboard</TabsTrigger>
            <TabsTrigger value="campanha">Nova Campanha</TabsTrigger>
            <TabsTrigger value="outreach">Outreach Auto</TabsTrigger>
            <TabsTrigger value="supressao">Supressao</TabsTrigger>
            <TabsTrigger value="historico">Historico</TabsTrigger>
          </TabsList>

          <TabsContent value="dashboard">
            <DashboardTab key={refreshKey} />
          </TabsContent>

          <TabsContent value="campanha">
            <div className="bg-white rounded-lg border p-6">
              <h2 className="text-base font-semibold text-slate-800 mb-5">Wizard de Campanha</h2>
              <CampaignWizard onDone={() => setRefreshKey(k => k + 1)} />
            </div>
          </TabsContent>

          <TabsContent value="outreach">
            <div className="bg-white rounded-lg border p-6">
              <h2 className="text-base font-semibold text-slate-800 mb-5">Outreach Automático</h2>
              <OutreachAutoTab />
            </div>
          </TabsContent>

          <TabsContent value="supressao">
            <div className="bg-white rounded-lg border p-6">
              <h2 className="text-base font-semibold text-slate-800 mb-1">Lista de Supressao</h2>
              <p className="text-sm text-slate-500 mb-5">
                Emails e dominios nesta lista nunca recebem campanhas. Alimentada automaticamente por hard bounces, spam complaints e descadastros.
              </p>
              <SuppressionListTable />
            </div>
          </TabsContent>

          <TabsContent value="historico">
            <div className="bg-white rounded-lg border p-6 text-center">
              <p className="text-slate-500 mb-4">
                Veja o historico completo de campanhas enviadas, com metricas de entrega e abertura.
              </p>
              <a
                href="/campanhas"
                className="inline-block bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-blue-700"
              >
                Ver todas as campanhas →
              </a>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
