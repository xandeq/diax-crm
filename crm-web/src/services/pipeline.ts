import { apiFetch } from './api';

// ── Pipeline de vendas (Kanban) ──────────────────────────────────────────────

export type PipelineStage = 'Lead' | 'Contacted' | 'Qualified' | 'Negotiating' | 'Customer' | 'Inactive';

export interface PipelineCard {
  id: string;
  name: string;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
  whatsApp?: string | null;
  estimatedValue?: number | null;
  expectedCloseDate?: string | null;
  leadScore?: number | null;
  segment?: string | null;
  lastContactAt?: string | null;
  tags?: string | null;
}

export interface PipelineColumn {
  status: PipelineStage;
  label: string;
  probability: number;      // 0..1 — peso na previsão ponderada
  count: number;
  totalValue: number;
  cards: PipelineCard[];
}

export interface PipelineBoard {
  columns: PipelineColumn[];
  totalOpenDeals: number;
  totalOpenValue: number;
  /** Previsão de receita = soma(valor × probabilidade do estágio) dos negócios abertos */
  weightedForecast: number;
  /** Total fechado (convertido) nos últimos 30 dias */
  wonLast30DaysValue: number;
  wonLast30DaysCount: number;
}

export async function getPipelineBoard(): Promise<PipelineBoard> {
  return apiFetch<PipelineBoard>('/pipeline/board', { method: 'GET' });
}

export async function movePipelineStage(customerId: string, status: PipelineStage): Promise<void> {
  await apiFetch(`/pipeline/leads/${customerId}/stage`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  });
}

export async function updatePipelineDeal(
  customerId: string,
  estimatedValue: number | null,
  expectedCloseDate: string | null,
): Promise<void> {
  await apiFetch(`/pipeline/leads/${customerId}/deal`, {
    method: 'PATCH',
    body: JSON.stringify({ estimatedValue, expectedCloseDate }),
  });
}
