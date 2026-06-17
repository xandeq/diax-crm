import { N8nWorkflowStatus, SystemHealthMetrics } from '../types/dashboard.types';

export const mockN8nWorkflows: N8nWorkflowStatus[] = [
  {
    id: 'email-marketing-v2',
    name: 'Email Marketing Queue Processor',
    isActive: true,
    lastExecutedAt: new Date(Date.now() - 3 * 60 * 1000).toISOString(),
    status: 'active',
  },
  {
    id: 'whatsapp-marketing-v2',
    name: 'WhatsApp Campaigns Dispatcher',
    isActive: true,
    lastExecutedAt: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
    status: 'active',
  },
  {
    id: 'whatsapp-routing',
    name: 'Evolution API Webhook Router',
    isActive: true,
    lastExecutedAt: new Date(Date.now() - 5 * 1000).toISOString(),
    status: 'active',
  },
  {
    id: 'n8n-workflow',
    name: 'Meta Ads Webhook Ingestion',
    isActive: false,
    lastExecutedAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    status: 'inactive',
  }
];

export function getMockSystemHealth(): SystemHealthMetrics {
  const isHealthy = Math.random() > 0.05;
  return {
    cpuUsage: Math.floor(Math.random() * 20) + 10, // 10% - 30%
    memoryUsage: 256 + Math.floor(Math.random() * 64), // MBs
    memoryLimit: 1024,
    responseTimeMs: Math.floor(Math.random() * 40) + 20, // 20ms - 60ms
    apiStatus: isHealthy ? 'healthy' : 'warning',
    dbStatus: 'healthy',
    uptimeSeconds: Math.floor(Date.now() / 1000) % 1000000 + 500000,
  };
}
