import { apiFetch } from './api';
import { BriefingCard, BriefingDetail } from '@/types/dailyBriefings';

export const dailyBriefingsService = {
  /** Cards dos briefings do dia corrente. */
  getToday: async (): Promise<BriefingCard[]> => {
    return apiFetch<BriefingCard[]>('/daily-briefings/today');
  },

  /** Conteúdo completo de um briefing. */
  getById: async (id: string): Promise<BriefingDetail> => {
    return apiFetch<BriefingDetail>(`/daily-briefings/${id}`);
  },
};
