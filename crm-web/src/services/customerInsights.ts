import { apiFetch } from './api';

export interface InsightPatternDto {
  title: string;
  description: string;
  impact: string;
  category: string;
}

export interface InsightsSummaryDto {
  totalLeads: number;
  totalCustomers: number;
  newLeadsInPeriod: number;
  convertedInPeriod: number;
  conversionRate: number;
  topSource: string;
  topSegment: string;
}

export interface GenerateInsightsRequest {
  provider: string;
  model?: string;
  dateRange?: string;
  focusArea?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface GenerateInsightsResponse {
  title: string;
  summary: InsightsSummaryDto;
  patterns: InsightPatternDto[];
  recommendations: string[];
  providerUsed: string;
  modelUsed: string;
  generatedAt: string;
  requestId: string;
}

export async function generateCustomerInsights(
  data: GenerateInsightsRequest
): Promise<GenerateInsightsResponse> {
  return apiFetch<GenerateInsightsResponse>('/ai/customer-insights', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
