import { apiFetch } from './api';

export interface WaveHeight {
  min?: number;
  max?: number;
}

export interface Wind {
  speed_kmh?: number;
  gust_kmh?: number;
  direction?: string;
  type?: string;
}

export interface Swell {
  direction?: string;
  period_s?: number;
}

export interface BestWindow {
  date: string;
  start?: string;
  end?: string;
  spot?: string;
  wave_height_m?: WaveHeight;
  swell?: Swell;
  wind?: Wind;
  tide?: string;
  weather?: string;
  rating?: number;
  calendar_free?: boolean;
}

export interface Alternative {
  date: string;
  start?: string;
  end?: string;
  spot?: string;
  wave_height_m?: WaveHeight;
  wind?: Wind;
  rating?: number;
  calendar_free?: boolean;
}

export interface SurfAlertResponse {
  id: string;
  source: string;
  type: string;
  generatedAt: string;
  weekendSaturday: string;
  weekendSunday: string;
  hasGoodWindow: boolean;
  summary: string;
  bestWindow?: BestWindow | null;
  alternatives?: Alternative[] | null;
  sources?: string[] | null;
  createdAt: string;
}

class SurfAlertsService {
  async getRecent(limit = 10): Promise<SurfAlertResponse[]> {
    return apiFetch<SurfAlertResponse[]>(`/surf-alerts?limit=${limit}`);
  }
}

export const surfAlertsService = new SurfAlertsService();
