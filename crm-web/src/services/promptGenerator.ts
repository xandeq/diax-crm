import { apiFetch } from './api';

export type PromptProvider = 'chatgpt' | 'perplexity' | 'deepseek';

export type PromptType = 'professional' | 'pas' | 'aida' | 'fab' | 'pear' | 'goat' | 'care';

export interface GeneratePromptRequest {
  rawPrompt: string;
  provider: PromptProvider;
  promptType: PromptType;
}

export interface GeneratePromptResponse {
  finalPrompt: string;
}

export const promptTypeOptions: { value: PromptType; label: string; description: string }[] = [
  {
    value: 'professional',
    label: 'Profissional (Padrão)',
    description: 'Prompt estruturado com contexto, objetivo, público-alvo e instruções.'
  },
  {
    value: 'pas',
    label: 'P.A.S. - Problema, Agitar, Solução',
    description: 'Ideal para conteúdo persuasivo, vendas e marketing emocional.'
  },
  {
    value: 'aida',
    label: 'A.I.D.A. - Atenção, Interesse, Desejo, Ação',
    description: 'Perfeito para anúncios, campanhas e jornada de decisão.'
  },
  {
    value: 'fab',
    label: 'F.A.B. - Características, Vantagens, Benefícios',
    description: 'Ótimo para descrições de produtos e traduzir specs em valor.'
  },
  {
    value: 'pear',
    label: 'P.E.A.R. - Pesquisa, Extrair, Aplicar, Entregar',
    description: 'Ideal para análises, pesquisas e síntese de informações.'
  },
  {
    value: 'goat',
    label: 'G.O.A.T. - Objetivo, Obstáculo, Ação, Transformação',
    description: 'Perfeito para storytelling, estudos de caso e narrativas.'
  },
  {
    value: 'care',
    label: 'C.A.R.E. - Conteúdo, Ação, Resultado, Emoção',
    description: 'Ideal para depoimentos e histórias de sucesso com dados.'
  },
];

export async function generatePrompt(rawPrompt: string, provider: PromptProvider, promptType: PromptType): Promise<string> {
  const request: GeneratePromptRequest = { rawPrompt, provider, promptType };

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
