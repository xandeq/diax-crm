import { apiFetch } from './api';

export type PromptProvider = 'chatgpt' | 'perplexity' | 'deepseek';

export interface GeneratePromptRequest {
  rawPrompt: string;
  provider: PromptProvider;
}

export interface GeneratePromptResponse {
  finalPrompt: string;
}

export async function generatePrompt(rawPrompt: string, provider: PromptProvider): Promise<string> {
  const request: GeneratePromptRequest = { rawPrompt, provider };

  const response = await apiFetch<GeneratePromptResponse>(
    '/prompt-generator/generate',
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    }
  );

  return response.finalPrompt;
}
