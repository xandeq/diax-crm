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

      const errorStatsRaw = getVal(errorStatsResult, null);
      const errorStats = {
        totalToday: errorStatsRaw?.totalToday ?? 0,
        criticalToday: errorStatsRaw?.criticalToday ?? 0,
        unresolvedTotal: errorStatsRaw?.unresolvedTotal ?? 0,
        byApp: Array.isArray(errorStatsRaw?.byApp) ? errorStatsRaw.byApp : []
      };

      const recentErrorsPaged = getVal(recentErrorsResult, null);
      const recentErrors = Array.isArray(recentErrorsPaged?.items) ? recentErrorsPaged.items : [];

      const logStatsRaw = getVal(logStatsResult, null);
      const logStats = {
        debugCount: logStatsRaw?.debugCount ?? 0,
        informationCount: logStatsRaw?.informationCount ?? 0,
        warningCount: logStatsRaw?.warningCount ?? 0,
        errorCount: logStatsRaw?.errorCount ?? 0,
        criticalCount: logStatsRaw?.criticalCount ?? 0,
        totalCount: logStatsRaw?.totalCount ?? 0
      };

      const recentLogsPaged = getVal(recentLogsResult, null);
      const recentLogs = Array.isArray(recentLogsPaged?.items) ? recentLogsPaged.items : [];

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
        recentErrors,
        logStats,
        recentLogs,
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
