import { apiFetch } from './api';
import type { QuotaStatusDto } from '@/components/QuotaStatusCard';

export interface ImageGenerationRequest {
  provider: string;
  model: string;
  prompt: string;
  negativePrompt?: string;
  width?: number;
  height?: number;
  numberOfImages?: number;
  style?: string;
  quality?: string;
  seed?: string;
  projectId?: string;
  referenceImageBase64?: string;
  /** Permite troca automática de provider em erro transitório (default: true) */
  allowFallback?: boolean;
}

export interface GeneratedImageDto {
  imageUrl: string;
  revisedPrompt?: string;
  seed?: string;
  width: number;
  height: number;
}

export interface ImageGenerationResponse {
  projectId: string;
  providerUsed: string;
  modelUsed: string;
  requestId: string;
  durationMs: number;
  images: GeneratedImageDto[];
  /** True quando o provider solicitado falhou e outro assumiu automaticamente */
  fallbackOccurred?: boolean;
  requestedProvider?: string;
  attemptedProviders?: string[];
  /** Custo estimado em USD (0 = grátis; null/undefined = desconhecido) */
  estimatedCostUsd?: number | null;
  quotaStatus?: QuotaStatusDto;
}

export const imageSizeOptions = [
  { value: '1024x1024', label: '1024 x 1024 (Quadrado)', width: 1024, height: 1024 },
  { value: '1792x1024', label: '1792 x 1024 (Paisagem)', width: 1792, height: 1024 },
  { value: '1024x1792', label: '1024 x 1792 (Retrato)', width: 1024, height: 1792 },
  { value: '512x512', label: '512 x 512 (Pequeno)', width: 512, height: 512 },
];

export const imageStyleOptions = [
  { value: 'vivid', label: 'Vívido' },
  { value: 'natural', label: 'Natural' },
];

export const imageQualityOptions = [
  { value: 'standard', label: 'Padrão' },
  { value: 'hd', label: 'HD' },
];

function normalizeImageUrl(url: string): string {
  if (!url || url.startsWith('http') || url.startsWith('data:')) return url;
  return `data:image/png;base64,${url}`;
}

export async function generateImage(data: ImageGenerationRequest): Promise<ImageGenerationResponse> {
  const response = await apiFetch<ImageGenerationResponse>('/ai/generate-image', {
    method: 'POST',
    body: JSON.stringify(data),
  });
  response.images = response.images.map(img => ({
    ...img,
    imageUrl: normalizeImageUrl(img.imageUrl),
  }));
  return response;
}

export interface VideoGenerationRequest {
  provider: string;
  model: string;
  prompt?: string;
  negativePrompt?: string;
  durationSeconds?: number;
  width?: number;
  height?: number;
  aspectRatio?: string;
  seed?: string;
  referenceImageBase64?: string;
  /** Permite troca automática de provider em erro transitório (default: true) */
  allowFallback?: boolean;
}

export interface VideoGenerationResponse {
  providerUsed: string;
  modelUsed: string;
  requestId: string;
  durationMs: number;
  videoUrl: string;
  thumbnailUrl?: string;
  quotaStatus?: QuotaStatusDto;
  /** True quando o provider solicitado falhou e outro assumiu automaticamente */
  fallbackOccurred?: boolean;
  requestedProvider?: string;
  attemptedProviders?: string[];
  /** Custo estimado em USD (0 = grátis; null/undefined = desconhecido) */
  estimatedCostUsd?: number | null;
}

export const videoAspectRatioOptions = [
  { value: '16:9', label: '16:9 (Paisagem HD)' },
  { value: '9:16', label: '9:16 (Vertical / Reels)' },
  { value: '1:1', label: '1:1 (Quadrado)' },
  { value: '4:3', label: '4:3 (Clássico)' },
  { value: '3:4', label: '3:4 (Retrato)' },
];

export const videoDurationOptions = [
  { value: 3, label: '3 segundos' },
  { value: 5, label: '5 segundos' },
  { value: 8, label: '8 segundos' },
  { value: 10, label: '10 segundos' },
];

export async function generateVideo(data: VideoGenerationRequest): Promise<VideoGenerationResponse> {
  return apiFetch<VideoGenerationResponse>('/ai/generate-video', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// ── Video jobs (geração assíncrona) ─────────────────────────────────────────
// Vídeos podem levar minutos; o fluxo é: enfileirar → poll do status → resultado.

export type VideoJobStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export interface VideoJobDto {
  id: string;
  status: VideoJobStatus;
  provider: string;
  model: string;
  prompt?: string | null;
  providerUsed?: string | null;
  modelUsed?: string | null;
  videoUrl?: string | null;
  thumbnailUrl?: string | null;
  errorMessage?: string | null;
  errorCategory?: string | null;
  fallbackOccurred: boolean;
  attemptedProviders?: string[] | null;
  queuePosition?: number | null;
  durationMs?: number | null;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  /** Custo estimado em USD (0 = grátis; null/undefined = desconhecido) */
  estimatedCostUsd?: number | null;
}

export async function createVideoJob(data: VideoGenerationRequest): Promise<VideoJobDto> {
  return apiFetch<VideoJobDto>('/ai/video-jobs', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function getVideoJob(jobId: string): Promise<VideoJobDto> {
  return apiFetch<VideoJobDto>(`/ai/video-jobs/${jobId}`, { method: 'GET' });
}

export async function listVideoJobs(take = 20): Promise<VideoJobDto[]> {
  return apiFetch<VideoJobDto[]>(`/ai/video-jobs?take=${take}`, { method: 'GET' });
}
