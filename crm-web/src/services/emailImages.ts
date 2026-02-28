import { apiFetch } from './api';

export interface UploadEmailImageRequest {
  fileName: string;
  base64Content: string;
  contentType: string;
}

export interface UploadEmailImageResponse {
  imageId: string;
  publicUrl: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
}

/**
 * Faz upload de uma imagem para o backend e retorna URL pública.
 * A URL retornada deve ser usada em <img src="URL"> no HTML do email.
 *
 * IMPORTANTE: Não usar base64 inline em emails!
 * Gmail e outros provedores bloqueiam data URIs.
 */
export async function uploadEmailImage(request: UploadEmailImageRequest): Promise<UploadEmailImageResponse> {
  return apiFetch<UploadEmailImageResponse>('/email-images/upload', {
    method: 'POST',
    body: JSON.stringify(request)
  });
}

/**
 * Remove uma imagem hospedada do servidor.
 */
export async function deleteEmailImage(imageId: string): Promise<void> {
  await apiFetch(`/email-images/${imageId}`, {
    method: 'DELETE'
  });
}
