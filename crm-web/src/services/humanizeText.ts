import { apiFetch } from './api';

/**
 * Provider de IA para humanização de texto.
 * Os providers disponíveis são carregados dinamicamente do banco de dados via API.
 * Use getAiCatalog() de aiCatalog.ts para obter a lista atualizada.
 */
export type HumanizeProvider = string;

export type HumanizeTone = 'humanize_text_light' | 'humanize_text_professional' | 'humanize_text_marketing' | 'humanize_text_documentation';

export interface HumanizeToneOption {
  value: HumanizeTone;
  label: string;
  description: string;
}

export interface HumanizeTextRequest {
  provider: HumanizeProvider;
  model?: string;
  model?: string;
  tone: HumanizeTone;
  inputText: string;
  language?: string;
}

export interface HumanizeTextResponse {
  outputText: string;
  providerUsed: string;
  toneUsed: string;
  requestId: string;
}

export const humanizeToneOptions: HumanizeToneOption[] = [
  {
    value: 'humanize_text_light',
    label: 'Leve / Casual',
    description: 'Tom descontraído e amigável, ideal para conversas informais.'
  },
  {
    value: 'humanize_text_professional',
    label: 'Profissional / Corporativo',
    description: 'Tom polido e direto, ideal para e-mails e comunicações de trabalho.'
  },
  {
    value: 'humanize_text_marketing',
    label: 'Marketing / Persuasivo',
    description: 'Tom engajador e convincente, ideal para redes sociais e vendas.'
  },
  {
    value: 'humanize_text_documentation',
    label: 'Técnico / Documental',
    description: 'Tom preciso e organizado, ideal para manuais e documentações.'
  }
];

export async function humanizeText(data: HumanizeTextRequest): Promise<HumanizeTextResponse> {
  return apiFetch<HumanizeTextResponse>('/ai/humanize-text', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
