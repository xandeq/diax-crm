import { apiFetch } from './api';

export interface ExtractTextRequest {
  html: string;
}

export interface ExtractTextResponse {
  text: string;
}

export interface ExtractUrlsRequest {
  html: string;
}

export interface ExtractUrlsResponse {
  urls: string[];
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

export async function extractUrls(html: string): Promise<string[]> {
  const request: ExtractUrlsRequest = { html };
  const response = await apiFetch<ExtractUrlsResponse>(
    '/htmlextraction/extract-urls',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );
  return response.urls ?? [];
}
