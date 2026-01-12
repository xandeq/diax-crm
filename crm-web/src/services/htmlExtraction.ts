import { apiFetch } from './api';

export interface ExtractTextRequest {
  html: string;
}

export interface ExtractTextResponse {
  text: string;
}

export async function extractText(html: string): Promise<string> {
  const request: ExtractTextRequest = { html };
  const response = await apiFetch<ExtractTextResponse>(
    '/htmlextraction/extract-text',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );
  return response.text;
}
