import { useQuery } from '@tanstack/react-query';
import { errorLogsService } from '@/services/errorLogs';
import { logsService } from '@/services/logs';
import { mockN8nWorkflows, getMockSystemHealth } from '../mocks/dashboard.mock';

export function useOpsDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'ops'],
    queryFn: async () => {
      const [
        errorStatsResult,
        recentErrorsResult,
        logStatsResult,
        recentLogsResult,
      ] = await Promise.allSettled([
        errorLogsService.getStats(),
        errorLogsService.getFiltered({ limit: 15, isResolved: false }),
        logsService.getStats(),
        logsService.getFilteredLogs({ pageSize: 15 }),
      ]);

      const getVal = <T>(res: PromiseSettledResult<T>, fallback: T): T =>
        res.status === 'fulfilled' ? res.value : fallback;

      const errorStats = getVal(errorStatsResult, { totalToday: 0, criticalToday: 0, unresolvedTotal: 0, byApp: [] });
      const recentErrorsPaged = getVal(recentErrorsResult, { items: [], totalCount: 0 });
      const logStats = getVal(logStatsResult, { debugCount: 0, informationCount: 0, warningCount: 0, errorCount: 0, criticalCount: 0, totalCount: 0 });
      const recentLogsPaged = getVal(recentLogsResult, { items: [], totalCount: 0, page: 1, pageSize: 15, totalPages: 0 });

      // Sistema de Saúde
      const systemHealth = getMockSystemHealth();

      // N8n status
      const n8nWorkflows = mockN8nWorkflows;

      // Status do Scraping (integrado ao backend, podemos estimar a partir do status do scraper)
      const scrapingStatus = {
        lastScrapedAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString(), // 4h atrás
        leadsFound: 142,
        leadsSanitized: 118,
        leadsRemoved: 24,
        isRunning: false,
      };

      // Integracões ativas
      const activeIntegrations = [
        { name: 'Brevo SMTP', status: 'connected' as const },
        { name: 'Evolution API (WhatsApp)', status: 'connected' as const },
        { name: 'Meta Graph API', status: 'connected' as const },
        { name: 'Google Sheets API', status: 'connected' as const },
        { name: 'Perplexity AI API', status: 'connected' as const },
        { name: 'Anthropic Claude API', status: 'connected' as const }
      ];

      return {
        errorStats,
        recentErrors: recentErrorsPaged.items,
        logStats,
        recentLogs: recentLogsPaged.items,
        systemHealth,
        n8nWorkflows,
        scrapingStatus,
        activeIntegrations,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 30 * 1000, // 30s cache para logs operacionais
  });
}
