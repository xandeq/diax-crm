import { CustomerStatus } from '@/services/leads';
import { ErrorLogLevel } from '@/services/errorLogs';
import { LogLevel, LogCategory } from '@/services/logs';

export interface FunnelCounts {
  lead: number;
  contacted: number;
  qualified: number;
  negotiating: number;
  customer: number;
}

export interface AgendaItem {
  time: string;
  title: string;
}

export interface EmailMarketingStats {
  totalCampaigns: number;
  totalEmailsSent: number;
  openRate: number;
  clickRate: number;
  bounceRate: number;
  spamRate: number;
  unsubscribeRate: number;
}

export interface CampaignSummary {
  id: string;
  name: string;
  subject: string;
  status: number;
  totalRecipients: number;
  sentCount: number;
  deliveredCount: number;
  openCount: number;
  clickCount: number;
  bounceCount: number;
  createdAt: string;
}

export interface ProviderHealth {
  name: string;
  status: 'healthy' | 'warning' | 'critical';
  sent: number;
  delivered: number;
  bounces: number;
  spam: number;
  score: number; // 0-100
}

export interface WhatsappStatus {
  connected: boolean;
  instanceName?: string;
  phoneNumber?: string;
  queuedMessages: number;
  sentToday: number;
  status: 'online' | 'offline' | 'connecting';
}

export interface ChannelPerformance {
  channel: 'Email' | 'WhatsApp' | 'Meta Ads';
  spend: number;
  revenue: number;
  leads: number;
  cpc: number;
  ctr: number;
  roas: number;
}

export interface ExpenseByCategory {
  name: string;
  total: number;
  pct: number;
}

export interface BriefingItem {
  id: string;
  title: string;
  source: string;
  content: string;
  createdAt: string;
  impact: 'high' | 'medium' | 'low';
}

export interface AppLog {
  id: string;
  timestampUtc: string;
  level: LogLevel;
  category: LogCategory;
  message: string;
  userId?: string;
  requestPath?: string;
  statusCode?: number;
}

export interface ErrorLog {
  id: string;
  appName: string;
  level: ErrorLogLevel;
  message: string;
  occurredAt: string;
  occurrenceCount: number;
  isResolved: boolean;
}

export interface ScrapingStatus {
  lastScrapedAt?: string;
  leadsFound: number;
  leadsSanitized: number;
  leadsRemoved: number;
  isRunning: boolean;
}

export interface N8nWorkflowStatus {
  id: string;
  name: string;
  isActive: boolean;
  lastExecutedAt?: string;
  status: 'active' | 'inactive' | 'error';
}

export interface SystemHealthMetrics {
  cpuUsage: number;
  memoryUsage: number;
  memoryLimit: number;
  responseTimeMs: number;
  apiStatus: 'healthy' | 'warning' | 'error';
  dbStatus: 'healthy' | 'error';
  uptimeSeconds: number;
}
