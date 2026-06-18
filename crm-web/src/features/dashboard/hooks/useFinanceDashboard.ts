import { useQuery } from '@tanstack/react-query';
import { financeService } from '@/services/finance';
import { plannerService } from '@/services/plannerService';

export function useFinanceDashboard() {
  return useQuery({
    queryKey: ['dashboard', 'finance'],
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

      const [
        currMonthResult,
        prevMonthResult,
        creditCardsResult,
        accountsResult,
        goalsResult,
        simulationResult,
      ] = await Promise.allSettled([
        financeService.getPersonalFinanceMonth(yr, mo),
        financeService.getPersonalFinanceMonth(py, pm),
        financeService.getCreditCards(),
        financeService.getFinancialAccounts(),
        plannerService.getActiveFinancialGoals(),
        plannerService.getOrGenerateSimulation(yr, mo),
      ]);

      const getVal = <T>(res: PromiseSettledResult<T>, fallback: T): T =>
        res.status === 'fulfilled' ? res.value : fallback;

      const currMonth = getVal(currMonthResult, null);
      const prevMonth = getVal(prevMonthResult, null);
      
      const creditCardsRaw = getVal(creditCardsResult, null);
      const creditCards = Array.isArray(creditCardsRaw) ? creditCardsRaw : [];
      
      const accountsRaw = getVal(accountsResult, null);
      const accounts = Array.isArray(accountsRaw) ? accountsRaw : [];
      
      const goalsRaw = getVal(goalsResult, null);
      const goals = Array.isArray(goalsRaw) ? goalsRaw : [];
      
      const simulation = getVal(simulationResult, null);

      const cs = currMonth?.summary;
      const ps = prevMonth?.summary;

      const income = cs?.totalIncome ?? 0;
      const expenses = cs?.totalExpenses ?? 0;
      const cashFlow = cs?.remainingBalance ?? 0;

      const revMoM = cs && ps && ps.totalIncome > 0 ? ((income - ps.totalIncome) / ps.totalIncome) * 100 : 0;
      const expMoM = cs && ps && ps.totalExpenses > 0 ? ((expenses - ps.totalExpenses) / ps.totalExpenses) * 100 : 0;

      // Agrupar despesas por categoria
      const expMap: Record<string, number> = {};
      (currMonth?.expenses ?? []).forEach((t) => {
        const cat = t.categoryName ?? 'Outros';
        expMap[cat] = (expMap[cat] ?? 0) + Math.abs(t.amount);
      });
      const expTotal = Object.values(expMap).reduce((a, b) => a + b, 0);
      const categoryExpenses = Object.entries(expMap)
        .sort(([, a], [, b]) => b - a)
        .map(([name, total]) => ({
          name,
          total,
          pct: expTotal > 0 ? (total / expTotal) * 100 : 0,
        }));

      // Cartões de crédito próximos do fechamento
      const currentDay = now.getDate();
      const nearClosingCards = creditCards.map(card => {
        const daysToClosing = card.closingDay >= currentDay 
          ? card.closingDay - currentDay 
          : (card.closingDay + 30) - currentDay; // estimativa simples
        return {
          id: card.id,
          name: card.name,
          limit: card.limit,
          closingDay: card.closingDay,
          dueDay: card.dueDay,
          daysToClosing,
        };
      }).sort((a, b) => a.daysToClosing - b.daysToClosing);

      // Despesas recentes
      const recentTransactions = (currMonth?.expenses ?? [])
        .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
        .slice(0, 10);

      // Faturas abertas (aproximado pelo cartão)
      const openInvoicesTotal = creditCards.reduce((acc, c) => acc + (c.limit * 0.15), 0); // Estimativa de uso de 15% do limite se não houver fatura direta

      // Alertas financeiros
      const alerts: string[] = [];
      if (simulation?.hasNegativeBalanceRisk) {
        alerts.push('Atenção: Risco detectado de saldo de caixa negativo no final do mês.');
      }
      if (expenses > income) {
        alerts.push('Alerta: Despesas totais superam a receita no mês corrente.');
      }
      nearClosingCards.slice(0, 2).forEach(card => {
        if (card.daysToClosing <= 3) {
          alerts.push(`Cartão ${card.name} fecha em ${card.daysToClosing} dias (${card.closingDay}).`);
        }
      });

      return {
        summary: cs,
        income,
        expenses,
        cashFlow,
        revMoM,
        expMoM,
        categoryExpenses,
        creditCards,
        nearClosingClosingCards: nearClosingCards,
        accounts,
        goals,
        simulation,
        curr: currMonth,
        recentTransactions,
        openInvoicesTotal,
        alerts,
      };
    },
    refetchOnWindowFocus: false,
    staleTime: 60 * 1000,
  });
}
