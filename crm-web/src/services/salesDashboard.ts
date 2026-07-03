import { apiFetch } from './api';

// ── Dashboard comercial ──────────────────────────────────────────────────────

export interface FunnelStage {
  stage: string;
  label: string;
  count: number;
  conversionToNext?: number | null; // 0-1
}

export interface MonthlyRevenue {
  month: string; // yyyy-MM
  total: number;
  count: number;
}

export interface ProposalSummary {
  status: string; // Draft | Sent | Accepted | Paid | Cancelled
  count: number;
  total: number;
}

export interface SalesDashboard {
  funnel: FunnelStage[];
  monthlyRevenue: MonthlyRevenue[];
  proposals: ProposalSummary[];
  proposalAcceptanceRate: number;
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  upcomingMeetings: number;
  openFollowUps: number;
  weightedForecast: number;
  wonLast30DaysValue: number;
  wonLast30DaysCount: number;
}

export async function getSalesDashboard(): Promise<SalesDashboard> {
  return apiFetch<SalesDashboard>('/sales-dashboard', { method: 'GET' });
}
