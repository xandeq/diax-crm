import { apiFetch, apiFetchRaw } from './api';

export type TaxDocumentType =
  | 0
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8;

export const TAX_DOCUMENT_TYPE_LABELS: Record<number, string> = {
  0: 'Banco',
  1: 'Corretora',
  2: 'Empresa',
  3: 'Plataforma',
  4: 'Investimento',
  5: 'Previdencia',
  6: 'Criptoativos',
  7: 'Pagamentos',
  8: 'Outros',
};

export interface TaxDocumentDto {
  id: string;
  fiscalYear: number;
  institutionName: string;
  institutionType: TaxDocumentType;
  institutionTypeName: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  notes?: string;
  uploadedAt: string;
}

export interface TaxDocumentListParams {
  fiscalYear?: number;
  institutionType?: number;
  search?: string;
}

export interface UploadTaxDocumentRequest {
  fiscalYear: number;
  institutionName: string;
  institutionType: number;
  notes?: string;
}

export interface UpdateTaxDocumentRequest {
  institutionName: string;
  institutionType: number;
  fiscalYear: number;
  notes?: string;
}

export async function getTaxDocuments(params?: TaxDocumentListParams): Promise<TaxDocumentDto[]> {
  const query = new URLSearchParams();
  if (params?.fiscalYear) query.set('fiscalYear', String(params.fiscalYear));
  if (params?.institutionType !== undefined) query.set('institutionType', String(params.institutionType));
  if (params?.search) query.set('search', params.search);
  const qs = query.toString();
  return apiFetch<TaxDocumentDto[]>(`/tax-documents${qs ? `?${qs}` : ''}`);
}

export async function getTaxDocumentFiscalYears(): Promise<number[]> {
  return apiFetch<number[]>('/tax-documents/fiscal-years');
}

export async function uploadTaxDocument(formData: FormData): Promise<TaxDocumentDto> {
  return apiFetch<TaxDocumentDto>('/tax-documents/upload', {
    method: 'POST',
    body: formData,
  });
}

export async function updateTaxDocument(
  id: string,
  request: UpdateTaxDocumentRequest,
): Promise<TaxDocumentDto> {
  return apiFetch<TaxDocumentDto>(`/tax-documents/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function downloadTaxDocument(id: string, fileName: string): Promise<void> {
  const response = await apiFetchRaw(`/tax-documents/${id}/download`, {
    method: 'GET',
  });

  if (!response.ok) {
    throw new Error(`Falha ao baixar documento (${response.status}).`);
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.URL.revokeObjectURL(url);
}

export async function deleteTaxDocument(id: string): Promise<void> {
  await apiFetch<void>(`/tax-documents/${id}`, { method: 'DELETE' });
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
