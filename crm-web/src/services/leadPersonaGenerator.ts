import { apiFetch, ApiError } from './api';

export interface PersonaDTO {
  id: number;
  name: string;
  title: string;
  companyType: string;
  industry: string;
  painPoints: string[];
  goals: string[];
  budgetRange: string;
  decisionProcess: string;
  effectiveChannels: string[];
  outreachMessages: string[];
  leadExamples: string[];
  percentageOfLeads: number;
}

export interface GeneratePersonasRequest {
  provider: string;
  model?: string;
  count?: number;
  focusSegment?: string;
  includeOutreachTips?: boolean;
  temperature?: number;
  maxTokens?: number;
}

export interface GeneratePersonasResponse {
  personas: PersonaDTO[];
  requestId: string;
  providerUsed?: string;
  modelUsed?: string;
  completionTime: number;
  leadsAnalyzed: number;
}

export async function generatePersonas(
  data: GeneratePersonasRequest,
): Promise<GeneratePersonasResponse> {
  try {
    return await apiFetch<GeneratePersonasResponse>('/ai/lead-personas', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  } catch (error) {
    if (error instanceof ApiError) {
      throw new Error(error.message);
    }
    throw error;
  }
}
