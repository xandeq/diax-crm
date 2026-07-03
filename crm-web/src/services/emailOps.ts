import { apiFetch } from './api'

// ─── Saúde operacional do módulo de email (GET /email-providers/ops-summary) ───

export interface EmailOpsProvider {
  provider: string
  key: string
  enabled: boolean
  sentToday: number
  queued: number
  dailyLimit: number | null
  breakerOpen: boolean
  breakerHalfOpen: boolean
  breakerReason: string | null
}

export interface EmailOpsSummary {
  generatedAtUtc: string
  queue: {
    queued: number
    processing: number
    failed: number
    deadLettered: number
    sentToday: number
    sentLastHour: number
  }
  limits: { daily: number; hourly: number }
  pilot: { isOpen: boolean; reason: string | null }
  providers: EmailOpsProvider[]
  ops: {
    telegramConfigured: boolean
    opsAlertsEnabled: boolean
    inCycleFallbackEnabled: boolean
    maxFallbackProvidersPerItem: number
    sandboxRedirectTo: string | null
  }
}

export interface DeadLetterItem {
  id: string
  recipientEmail: string
  recipientName: string
  subject: string
  assignedProvider: string
  attemptCount: number
  lastError: string | null
  campaignId: string | null
  deadLetteredAtUtc: string | null
  createdAtUtc: string
}

export interface DeadLetterPage {
  totalCount: number
  page: number
  pageSize: number
  items: DeadLetterItem[]
}

const basePath = '/email-providers'

export const emailOpsService = {
  getOpsSummary: async (): Promise<EmailOpsSummary> => {
    return apiFetch<EmailOpsSummary>(`${basePath}/ops-summary`)
  },

  getDeadLetter: async (page = 1, pageSize = 20): Promise<DeadLetterPage> => {
    return apiFetch<DeadLetterPage>(`${basePath}/dead-letter?page=${page}&pageSize=${pageSize}`)
  },

  requeueDeadLetter: async (id: string): Promise<{ requeued: string; provider: string }> => {
    return apiFetch<{ requeued: string; provider: string }>(`${basePath}/dead-letter/${id}/requeue`, {
      method: 'POST',
    })
  },
}
