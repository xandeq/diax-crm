import { apiFetch } from './api';

export interface AiInsightsResponse {
    text: string;
}

export const getDailyInsights = async (): Promise<AiInsightsResponse> => {
    return apiFetch<AiInsightsResponse>('/admin/ai-insights/daily');
};
