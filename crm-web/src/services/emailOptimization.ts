import { apiFetch } from './api';

export interface SubjectLineDto {
  text: string;
  angle: string;
  estimatedOpenRate: number;
}

export interface GenerateSubjectLinesRequest {
  provider: string;
  model?: string;
  baseMessage: string;
  targetAudience?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface GenerateSubjectLinesResponse {
  subjects: SubjectLineDto[];
  requestId: string;
  providerUsed: string;
  modelUsed: string;
  completionTime: number;
}

export async function generateSubjectLines(
  data: GenerateSubjectLinesRequest
): Promise<GenerateSubjectLinesResponse> {
  return apiFetch<GenerateSubjectLinesResponse>('/ai/email-subject-optimizer', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
