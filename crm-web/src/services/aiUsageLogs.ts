import { apiFetch } from './api';

export interface ProviderUsageStats {
    providerId: string;
    providerName: string;
    totalRequests: number;
    totalTokens: number;
    totalCost: number;
}

export const getGroupedAiUsageStats = async (
    startDate?: string,
    endDate?: string
): Promise<ProviderUsageStats[]> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const queryString = params.toString();
    return apiFetch<ProviderUsageStats[]>(`/admin/ai/usage-logs/stats/grouped${queryString ? `?${queryString}` : ''}`);
};
