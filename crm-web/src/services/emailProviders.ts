import { apiFetch } from './api'

export interface ProviderHealthDto {
  provider: string
  sentToday: number
  dailyLimit: number
  dailyRemaining: number
  dailyUsagePercent: number
  sentThisHour: number
  hourlyLimit: number
  hourlyRemaining: number
  health: 'ok' | 'degraded' | 'down'
}

export interface SmartPreselectRequest {
  segments: number[]
  minScore: number
  maxPerProvider: number
  cooldownDays: number
}

export interface PreselectedLeadDto {
  customerId: string
  name: string
  firstName: string
  email: string
  assignedProvider: string
  segment: number
  segmentLabel: string
  score: number
  reasonForSelection: string
  lastEmailSentAt: string | null
}

export interface SmartPreselectResponse {
  leads: PreselectedLeadDto[]
  totalSelected: number
  brevoCount: number
  mailjetCount: number
  resendCount: number
  warnings: string[]
}

export interface AssignedLeadQueueDto {
  customerId: string
  assignedProvider: string
}

export interface QueueWithAssignmentRequest {
  campaignId: string
  leads: AssignedLeadQueueDto[]
}

export async function getProviderHealth(): Promise<ProviderHealthDto[]> {
  return apiFetch<ProviderHealthDto[]>('/api/v1/email-providers/health')
}

export async function smartPreselect(request: SmartPreselectRequest): Promise<SmartPreselectResponse> {
  return apiFetch<SmartPreselectResponse>('/api/v1/email-providers/smart-preselect', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
}

export async function queueWithAssignment(request: QueueWithAssignmentRequest): Promise<unknown> {
  return apiFetch('/api/v1/email-providers/queue-with-assignment', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
}
