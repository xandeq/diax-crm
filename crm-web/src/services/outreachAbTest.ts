import { apiFetch } from './api';

export interface OutreachVariationDto {
  label: string;
  tone: string;
  subject: string;
  body: string;
  estimatedResponseRate: number;
  rationale: string;
}

export interface GenerateAbVariationsRequest {
  provider: string;
  model?: string;
  baseMessage: string;
  targetAudience?: string;
  industry?: string;
  goal?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface GenerateAbVariationsResponse {
  variations: OutreachVariationDto[];
  providerUsed: string;
  modelUsed: string;
  generatedAt: string;
  requestId: string;
}

export async function generateAbVariations(
  data: GenerateAbVariationsRequest
): Promise<GenerateAbVariationsResponse> {
  return apiFetch<GenerateAbVariationsResponse>('/ai/outreach-ab-test', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
