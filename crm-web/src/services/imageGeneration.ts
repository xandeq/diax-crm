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
  isBase64: boolean;
  revisedPrompt?: string;
  seed?: string;
  width: number;
  height: number;
  durationMs: number;
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

export async function generateImage(data: ImageGenerationRequest): Promise<ImageGenerationResponse> {
  return apiFetch<ImageGenerationResponse>('/ai/generate-image', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
