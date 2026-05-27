import { apiFetch } from './api';

export interface Ga4Status {
  isConfigured: boolean;
  propertyId: string | null;
}

export interface Ga4OverviewMetrics {
  sessions: number;
  users: number;
  newUsers: number;
  pageViews: number;
  bounceRate: number;
  avgSessionDuration: number;
}

export interface Ga4TrafficSourceItem {
  channel: string;
  sessions: number;
  percentage: number;
}

export interface Ga4TopPageItem {
  page: string;
  pageViews: number;
  users: number;
}

export interface Ga4DeviceItem {
  device: string;
  sessions: number;
  percentage: number;
}

export interface Ga4TimeSeriesPoint {
  date: string;
  sessions: number;
  users: number;
}

export interface Ga4Report {
  overview: Ga4OverviewMetrics;
  trafficSources: Ga4TrafficSourceItem[];
  topPages: Ga4TopPageItem[];
  devices: Ga4DeviceItem[];
  timeSeries: Ga4TimeSeriesPoint[];
  startDate: string;
  endDate: string;
}

export async function getGa4Status(): Promise<Ga4Status> {
  return apiFetch('/google-analytics/status');
}

export async function getGa4Report(days: number = 30): Promise<Ga4Report> {
  return apiFetch(`/google-analytics/report?days=${days}`);
}
