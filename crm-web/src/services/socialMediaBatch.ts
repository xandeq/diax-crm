import { apiFetch } from './api';

export interface SocialPostDto {
  number: number;
  platform: string;
  contentType: string;
  caption: string;
  hashtags: string[];
  imagePrompt: string | null;
  imageDimension: string;
  bestTimeToPost: string;
  topic: string;
}

export interface GenerateSocialBatchRequest {
  provider: string;
  model?: string;
  topics: string[];
  platforms: string[];
  postCount?: number;
  month?: string;
  brandVoice?: string;
  targetAudience?: string;
  temperature?: number;
  maxTokens?: number;
}

export interface GenerateSocialBatchResponse {
  posts: SocialPostDto[];
  providerUsed: string;
  modelUsed: string;
  generatedAt: string;
  requestId: string;
}

export async function generateSocialBatch(
  data: GenerateSocialBatchRequest
): Promise<GenerateSocialBatchResponse> {
  return apiFetch<GenerateSocialBatchResponse>('/ai/social-media-batch', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
