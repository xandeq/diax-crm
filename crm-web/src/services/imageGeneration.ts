import { apiFetch } from './api';

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
}

export interface VideoGenerationResponse {
  providerUsed: string;
  modelUsed: string;
  requestId: string;
  durationMs: number;
  videoUrl: string;
  thumbnailUrl?: string;
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
