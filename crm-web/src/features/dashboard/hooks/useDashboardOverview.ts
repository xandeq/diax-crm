import { useQuery } from '@tanstack/react-query';
import { financeService } from '@/services/finance';
import { getLeads, CustomerStatus as LeadStatus } from '@/services/leads';
import { agendaService } from '@/services/agenda';
import { plannerService } from '@/services/plannerService';
import { investiqService } from '@/services/personalControlService';
import { getAdAccountSummary, getInsights } from '@/services/ads';
import { apiFetch } from '@/services/api';
import { FunnelCounts, AgendaItem, EmailMarketingStats } from '../types/dashboard.types';
import { tasksService } from '@/services/tasks';
import { checklistService } from '@/services/checklistService';

export function useDashboardOverview() {
  return useQuery({
    queryKey: ['dashboard', 'overview'],
    queryFn: async () => {
      const now = new Date();
      const yr = now.getFullYear();
      const mo = now.getMonth() + 1;

      // Calcular mês anterior
      let py = yr;
      let pm = mo - 1;
      if (pm === 0) {
        pm = 12;
        py = yr - 1;
      }

      // 6 meses anteriores para histórico
      const months6: [number, number][] = [];
      for (let i = 5; i >= 0; i--) {
        let y6 = yr;
        let m6 = mo - i;
        while (m6 <= 0) {
          m6 += 12;
          y6 -= 1;
        }
        months6.push([y6, m6]);
      }

      // Chamadas concorrentes e tolerantes a falhas
      const [
        currMonthResult,
        prevMonthResult,
        fLResult,
        fCResult,
        fQResult,
        fNResult,
        fCustResult,
        emailResult,
        agendaResult,
        goalsResult,
        simulationResult,
        investiqResult,
        adsSummaryResult,
        adsInsightsResult,
        tasksResult,
        checklistsResult,
        recentLeadsResult,
      ] = await Promise.allSettled([
        financeService.getPersonalFinanceMonth(yr, mo),
        financeService.getPersonalFinanceMonth(py, pm),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Lead }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Contacted }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Qualified }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Negotiating }),
        getLeads({ page: 1, pageSize: 1, status: LeadStatus.Customer }),
        apiFetch<{ overallStats: EmailMarketingStats }>('/email-campaigns/analytics?days=30'),
        agendaService.getByDateRange(
          new Date(yr, mo - 1, now.getDate()).toISOString(),
          new Date(yr, mo - 1, now.getDate(), 23, 59).toISOString()
        ),
        plannerService.getActiveFinancialGoals(),
        plannerService.getOrGenerateSimulation(yr, mo),
        investiqService.getPortfolioSummary(),
        getAdAccountSummary(),
        getInsights({ datePreset: 'last_30d', level: 'campaign' }).catch(() => []),
        tasksService.getAll({ status: 'Todo' }),
        checklistService.getItems({ status: 0, pageSize: 10 }),
        getLeads({ page: 1, pageSize: 50, sortBy: 'CreatedAt', sortDescending: true }),
      ]);

      // Helper para extrair valor das settled promises
      const getVal = <T>(res: PromiseSettledResult<T>, fallback: T): T =>
        res.status === 'fulfilled' ? res.value : fallback;

      const currMonth = getVal(currMonthResult, null);
      const prevMonth = getVal(prevMonthResult, null);

      const fc = (res: PromiseSettledResult<{ totalCount?: number }>) =>
        res.status === 'fulfilled' ? res.value?.totalCount ?? 0 : 0;

      const funnel: FunnelCounts = {
        lead: fc(fLResult),
        contacted: fc(fCResult),
        qualified: fc(fQResult),
        negotiating: fc(fNResult),
        customer: fc(fCustResult),
      };

      // Histórico de 6 meses
      const historicalPromises = await Promise.allSettled(
        months6.map(([y, m]) => financeService.getPersonalFinanceMonth(y, m))
      );
      const MONTH_NAMES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];
      const trend = historicalPromises.map((res, idx) => {
        const val = res.status === 'fulfilled' ? res.value : null;
        return {
          label: MONTH_NAMES[months6[idx][1] - 1],
          income: val?.summary?.totalIncome ?? 0,
          expense: val?.summary?.totalExpenses ?? 0,
        };
      });

      // Agrupar despesas por categoria
      const expMap: Record<string, number> = {};
      (currMonth?.expenses ?? []).forEach((t) => {
        const cat = t.categoryName ?? 'Outros';
        expMap[cat] = (expMap[cat] ?? 0) + Math.abs(t.amount);
      });
      const expTotal = Object.values(expMap).reduce((a, b) => a + b, 0);
      const expenses = Object.entries(expMap)
        .sort(([, a], [, b]) => b - a)
        .slice(0, 5)
        .map(([name, total]) => ({
          name,
          total,
          pct: expTotal > 0 ? (total / expTotal) * 100 : 0,
        }));

      const email = getVal(emailResult, null);
      const agendaRaw = getVal(agendaResult, null);
      const agendaList = Array.isArray(agendaRaw) ? agendaRaw : [];
      const agenda: AgendaItem[] = agendaList.map((a: any) => ({
        time: a?.startTime
          ? new Date(a.startTime).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
          : '--:--',
        title: a?.title ?? 'Compromisso',
      }));

      const goalsRaw = getVal(goalsResult, null);
      const goals = Array.isArray(goalsRaw) ? goalsRaw : [];
      
      const simulation = getVal(simulationResult, null);
      const investiq = getVal(investiqResult, null);
      const adsSummary = getVal(adsSummaryResult, null);
      
      const adsInsightsRaw = getVal(adsInsightsResult, null);
      const adsInsights = Array.isArray(adsInsightsRaw) ? adsInsightsRaw : [];
      
      const tasksRaw = getVal(tasksResult, null);
      const tasks = Array.isArray(tasksRaw) ? tasksRaw : [];
      
      const checklistsPaged = getVal(checklistsResult, null);
      const checklists = Array.isArray(checklistsPaged?.items) ? checklistsPaged.items : [];
      
      const recentLeadsPaged = getVal(recentLeadsResult, null);
      const recentLeads = Array.isArray(recentLeadsPaged?.items) ? recentLeadsPaged.items : [];

      return {
        funnel,
        curr: currMonth,
        prev: prevMonth,
        trend,
        expenses,
        email: email?.overallStats ?? null,
        agenda,
        goals,
        simulation,
        investiq,
        ads: adsSummary ? { summary: adsSummary, insights: adsInsights, period: 'últimos 30 dias' } : null,
        tasks,
        checklists,
        recentLeads,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 60 * 1000, // 1 minuto de cache
  });
}
