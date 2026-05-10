import { apiFetch } from './api'

export interface NormalizationStatsDto {
  total: number
  normalized: number
  pending: number
  coveragePercent: number
  highConfidence: number
  lowConfidence: number
}

export interface NormalizationPreviewItemDto {
  customerId: string
  rawName: string
  email: string | null
  suggestedName: string | null
  suggestedScore: number
  suggestedSource: string
  currentNormalizedName: string | null
  currentScore: number | null
}

export interface RunNormalizationResultDto {
  processed: number
  updated: number
  skipped: number
}

export function getNormalizationStats(): Promise<NormalizationStatsDto> {
  return apiFetch<NormalizationStatsDto>('/leads/normalization/stats')
}

export function getNormalizationPreview(
  page = 1,
  pageSize = 20,
): Promise<NormalizationPreviewItemDto[]> {
  return apiFetch<NormalizationPreviewItemDto[]>(
    `/leads/normalization/preview?page=${page}&pageSize=${pageSize}`,
  )
}

export function runBatchNormalization(
  forceReprocess = false,
): Promise<RunNormalizationResultDto> {
  return apiFetch<RunNormalizationResultDto>(
    `/leads/normalization/run?forceReprocess=${forceReprocess}`,
    { method: 'POST' },
  )
}
