'use client'

import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Checkbox } from '@/components/ui/checkbox'
import { RichTextEditor } from '@/components/editor/RichTextEditor'
import { SmartPreselectionTable } from './SmartPreselectionTable'
import {
  smartPreselect,
  queueWithAssignment,
  type SmartPreselectRequest,
  type PreselectedLeadDto,
} from '@/services/emailProviders'
import {
  createEmailCampaign,
  sendTestEmail,
  type CreateEmailCampaignRequest,
} from '@/services/emailMarketing'

// ── Step indicator ────────────────────────────────────────────────────────────

const STEPS = ['Audiência', 'Pré-seleção', 'Composer', 'Enviar']

function StepIndicator({ current }: { current: number }) {
  return (
    <div className="flex items-center gap-2 mb-6">
      {STEPS.map((label, i) => (
        <div key={i} className="flex items-center gap-2">
          <div
            className="flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold"
            style={i < current
              ? { background: '#10B981', color: '#fff' }
              : i === current
                ? { background: '#3b82f6', color: '#fff' }
                : { background: 'rgba(255,255,255,0.1)', color: '#6B7280' }}
          >
            {i < current ? '✓' : i + 1}
          </div>
          <span className="text-sm" style={{ color: i === current ? '#F9FAFB' : '#6B7280', fontWeight: i === current ? 600 : 400 }}>
            {label}
          </span>
          {i < STEPS.length - 1 && <div className="w-6 h-px" style={{ background: 'rgba(255,255,255,0.1)' }} />}
        </div>
      ))}
    </div>
  )
}

// ── Step 1: Audience ──────────────────────────────────────────────────────────

type AudiencePreset = 'hot' | 'hotWarm' | 'warm'

interface AudienceState {
  preset: AudiencePreset
  maxPerProvider: number
  cooldownDays: number
  minScore: number
}

const PRESET_SEGMENTS: Record<AudiencePreset, number[]> = {
  hot:     [2],
  hotWarm: [1, 2],
  warm:    [1],
}

function AudienceStep({
  state,
  onChange,
  onNext,
}: {
  state: AudienceState
  onChange: (s: AudienceState) => void
  onNext: () => void
}) {
  return (
    <div className="space-y-5">
      <div>
        <Label className="mb-2 block text-sm font-medium">Segmento</Label>
        <div className="flex gap-2 flex-wrap">
          {([
            ['hot',     'Somente Hot'],
            ['hotWarm', 'Hot + Warm'],
            ['warm',    'Somente Warm'],
          ] as [AudiencePreset, string][]).map(([val, label]) => (
            <button
              key={val}
              type="button"
              onClick={() => onChange({ ...state, preset: val })}
              className="px-4 py-1.5 rounded-full text-sm border transition-colors"
              style={state.preset === val
                ? { background: '#3b82f6', color: '#fff', borderColor: '#3b82f6' }
                : { background: 'rgba(255,255,255,0.05)', color: '#9CA3AF', borderColor: 'rgba(255,255,255,0.15)' }}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label htmlFor="maxPP" className="text-sm">Máx. por provider</Label>
          <Input
            id="maxPP"
            type="number"
            min={1}
            max={50}
            value={state.maxPerProvider}
            onChange={e => onChange({ ...state, maxPerProvider: Number(e.target.value) })}
            className="mt-1"
          />
          <p className="text-xs text-slate-400 mt-1">Total: {state.maxPerProvider * 3} emails</p>
        </div>
        <div>
          <Label htmlFor="cooldown" className="text-sm">Cooldown (dias)</Label>
          <Input
            id="cooldown"
            type="number"
            min={0}
            max={365}
            value={state.cooldownDays}
            onChange={e => onChange({ ...state, cooldownDays: Number(e.target.value) })}
            className="mt-1"
          />
          <p className="text-xs text-slate-400 mt-1">Ignora leads contatados há menos de {state.cooldownDays}d</p>
        </div>
      </div>

      <div>
        <Label htmlFor="minScore" className="text-sm">Score mínimo</Label>
        <Input
          id="minScore"
          type="number"
          min={0}
          value={state.minScore}
          onChange={e => onChange({ ...state, minScore: Number(e.target.value) })}
          className="mt-1 w-32"
        />
      </div>

      <div className="flex justify-end pt-2">
        <Button onClick={onNext}>Próximo →</Button>
      </div>
    </div>
  )
}

// ── Step 2: Smart Preselect ───────────────────────────────────────────────────

function PreselectStep({
  audience,
  selectedLeads,
  onLeadsChange,
  onBack,
  onNext,
}: {
  audience: AudienceState
  selectedLeads: PreselectedLeadDto[]
  onLeadsChange: (leads: PreselectedLeadDto[]) => void
  onBack: () => void
  onNext: () => void
}) {
  const [allLeads, setAllLeads] = useState<PreselectedLeadDto[]>([])
  const [loading, setLoading] = useState(false)
  const [fetched, setFetched] = useState(false)

  const handlePreselect = async () => {
    setLoading(true)
    try {
      const req: SmartPreselectRequest = {
        segments: PRESET_SEGMENTS[audience.preset],
        minScore: audience.minScore,
        maxPerProvider: audience.maxPerProvider,
        cooldownDays: audience.cooldownDays,
      }
      const res = await smartPreselect(req)
      setAllLeads(res.leads)
      onLeadsChange(res.leads)
      setFetched(true)
      if (res.warnings.length > 0) {
        res.warnings.forEach(w => toast.warning(w))
      }
    } catch {
      toast.error('Erro ao pré-selecionar leads.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Button onClick={handlePreselect} disabled={loading} variant="outline">
          {loading ? 'Buscando...' : `Pré-selecionar ${audience.maxPerProvider * 3} leads`}
        </Button>
        {fetched && (
          <span className="text-sm text-slate-500">
            {allLeads.length} encontrado(s) — {selectedLeads.length} selecionado(s)
          </span>
        )}
      </div>

      {fetched && allLeads.length > 0 && (
        <SmartPreselectionTable leads={allLeads} onSelectionChange={onLeadsChange} />
      )}

      {fetched && allLeads.length === 0 && (
        <p className="text-slate-500 text-sm py-4 text-center">
          Nenhum lead elegível encontrado com os critérios informados.
        </p>
      )}

      <div className="flex justify-between pt-2">
        <Button variant="ghost" onClick={onBack}>← Voltar</Button>
        <Button onClick={onNext} disabled={selectedLeads.length === 0}>
          Próximo ({selectedLeads.length} leads) →
        </Button>
      </div>
    </div>
  )
}

// ── Step 3: Composer ──────────────────────────────────────────────────────────

interface ComposerState {
  campaignName: string
  subject: string
  htmlBody: string
}

const DEFAULT_BODY = `<p>Olá <strong>{{firstName}}</strong>,</p>
<p>Escreva o corpo do seu email aqui...</p>
<p>Atenciosamente,<br><strong>Alexandre Queiroz</strong></p>`

function ComposerStep({
  state,
  onChange,
  onBack,
  onNext,
}: {
  state: ComposerState
  onChange: (s: ComposerState) => void
  onBack: () => void
  onNext: (campaignId: string) => void
}) {
  const [saving, setSaving] = useState(false)
  const [sendingTest, setSendingTest] = useState(false)
  const [savedCampaignId, setSavedCampaignId] = useState<string | null>(null)

  const saveCampaign = async (): Promise<string | null> => {
    if (savedCampaignId) return savedCampaignId
    if (!state.campaignName.trim() || !state.subject.trim()) {
      toast.error('Preencha o nome da campanha e o assunto.')
      return null
    }
    try {
      const req: CreateEmailCampaignRequest = {
        name: state.campaignName.trim(),
        subject: state.subject.trim(),
        bodyHtml: state.htmlBody,
      }
      const campaign = await createEmailCampaign(req)
      setSavedCampaignId(campaign.id)
      return campaign.id
    } catch {
      toast.error('Erro ao salvar campanha.')
      return null
    }
  }

  const handleSendTest = async () => {
    setSendingTest(true)
    try {
      const id = await saveCampaign()
      if (!id) return
      await sendTestEmail(id, {
        subjectOverride: state.subject,
        bodyHtmlOverride: state.htmlBody,
      })
      toast.success('Email de teste enviado para sua caixa de entrada!')
    } catch {
      toast.error('Erro ao enviar email de teste.')
    } finally {
      setSendingTest(false)
    }
  }

  const handleNext = async () => {
    setSaving(true)
    try {
      const id = await saveCampaign()
      if (id) onNext(id)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-4">
      <div>
        <Label htmlFor="campName" className="text-sm">Nome da Campanha</Label>
        <Input
          id="campName"
          value={state.campaignName}
          onChange={e => onChange({ ...state, campaignName: e.target.value })}
          placeholder="Ex: Apps – Batch 3 – Maio 2026"
          className="mt-1"
        />
      </div>

      <div>
        <Label htmlFor="subject" className="text-sm">Assunto</Label>
        <Input
          id="subject"
          value={state.subject}
          onChange={e => onChange({ ...state, subject: e.target.value })}
          placeholder="Ex: {{firstName}}, seu negócio merece um app próprio"
          className="mt-1"
        />
        <p className="text-xs text-slate-400 mt-1">
          Variáveis disponíveis: {'{{firstName}}'} {'{{company}}'} {'{{email}}'}
        </p>
      </div>

      <div>
        <Label className="text-sm mb-1 block">Corpo do Email (HTML)</Label>
        <div className="border rounded-md">
          <RichTextEditor
            value={state.htmlBody || DEFAULT_BODY}
            onChange={v => onChange({ ...state, htmlBody: v })}
          />
        </div>
      </div>

      <div className="flex justify-between items-center pt-2">
        <div className="flex gap-2">
          <Button variant="ghost" onClick={onBack}>← Voltar</Button>
          <Button variant="outline" onClick={handleSendTest} disabled={sendingTest}>
            {sendingTest ? 'Enviando...' : 'Enviar teste para mim'}
          </Button>
        </div>
        <Button onClick={handleNext} disabled={saving}>
          {saving ? 'Salvando...' : 'Próximo →'}
        </Button>
      </div>
    </div>
  )
}

// ── Step 4: Send ──────────────────────────────────────────────────────────────

function SendStep({
  campaignId,
  leads,
  onBack,
  onDone,
}: {
  campaignId: string
  leads: PreselectedLeadDto[]
  onBack: () => void
  onDone: () => void
}) {
  const [sending, setSending] = useState(false)

  const providerCounts = ['Brevo', 'Mailjet', 'Resend'].map(p => ({
    name: p,
    count: leads.filter(l => l.assignedProvider === p).length,
  }))

  const handleSend = async () => {
    setSending(true)
    try {
      await queueWithAssignment({
        campaignId,
        leads: leads.map(l => ({
          customerId: l.customerId,
          assignedProvider: l.assignedProvider,
        })),
      })
      toast.success(`${leads.length} emails enfileirados com sucesso! O worker envia em até 5 min.`)
      onDone()
    } catch {
      toast.error('Erro ao enfileirar emails.')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="space-y-5">
      <div className="rounded-lg p-4 space-y-3" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
        <h3 className="font-semibold" style={{ color: '#D1D5DB' }}>Resumo do Envio</h3>
        <div className="grid grid-cols-3 gap-3">
          {providerCounts.map(({ name, count }) => (
            <div key={name} className="text-center rounded p-3" style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.09)' }}>
              <div className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>{count}</div>
              <div className="text-xs" style={{ color: '#9CA3AF' }}>{name}</div>
            </div>
          ))}
        </div>
        <div className="text-sm" style={{ color: '#9CA3AF' }}>
          <span className="font-semibold" style={{ color: '#D1D5DB' }}>{leads.length} emails</span> serão enfileirados e enviados pelo worker em até 5 minutos.
        </div>
      </div>

      <div className="flex justify-between pt-2">
        <Button variant="ghost" onClick={onBack}>← Voltar</Button>
        <Button
          onClick={handleSend}
          disabled={sending || leads.length === 0}
          className="bg-green-600 hover:bg-green-700 text-white"
        >
          {sending ? 'Enfileirando...' : `Enviar ${leads.length} emails agora`}
        </Button>
      </div>
    </div>
  )
}

// ── Main Wizard ───────────────────────────────────────────────────────────────

interface Props {
  onDone?: () => void
}

export function CampaignWizard({ onDone }: Props) {
  const [step, setStep] = useState(0)
  const [audience, setAudience] = useState<AudienceState>({
    preset: 'hotWarm',
    maxPerProvider: 20,
    cooldownDays: 30,
    minScore: 0,
  })
  const [selectedLeads, setSelectedLeads] = useState<PreselectedLeadDto[]>([])
  const [composer, setComposer] = useState<ComposerState>({
    campaignName: '',
    subject: '',
    htmlBody: DEFAULT_BODY,
  })
  const [campaignId, setCampaignId] = useState<string | null>(null)

  const handleDone = () => {
    // Reset wizard for next use
    setStep(0)
    setSelectedLeads([])
    setComposer({ campaignName: '', subject: '', htmlBody: DEFAULT_BODY })
    setCampaignId(null)
    onDone?.()
  }

  return (
    <div className="max-w-3xl">
      <StepIndicator current={step} />

      {step === 0 && (
        <AudienceStep
          state={audience}
          onChange={setAudience}
          onNext={() => setStep(1)}
        />
      )}
      {step === 1 && (
        <PreselectStep
          audience={audience}
          selectedLeads={selectedLeads}
          onLeadsChange={setSelectedLeads}
          onBack={() => setStep(0)}
          onNext={() => setStep(2)}
        />
      )}
      {step === 2 && (
        <ComposerStep
          state={composer}
          onChange={setComposer}
          onBack={() => setStep(1)}
          onNext={id => { setCampaignId(id); setStep(3) }}
        />
      )}
      {step === 3 && campaignId && (
        <SendStep
          campaignId={campaignId}
          leads={selectedLeads}
          onBack={() => setStep(2)}
          onDone={handleDone}
        />
      )}
    </div>
  )
}
