import { apiFetch } from './api'

export interface SuppressionDto {
  id: string
  email: string | null
  domainPattern: string | null
  reason: string
  source: string
  suppressedAt: string
}

export function getSuppressions(): Promise<SuppressionDto[]> {
  return apiFetch<SuppressionDto[]>('/api/v1/email-suppressions')
}

export function addEmailSuppression(email: string): Promise<void> {
  return apiFetch<void>('/api/v1/email-suppressions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  })
}

export function addDomainSuppression(domain: string): Promise<void> {
  return apiFetch<void>('/api/v1/email-suppressions', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ domain }),
  })
}

export function removeSuppression(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/email-suppressions/${id}`, { method: 'DELETE' })
}
