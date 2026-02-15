import { apiFetch } from './api';
import {
  FinancialGoal,
  CreateFinancialGoalRequest,
  UpdateFinancialGoalRequest,
  AddContributionRequest,
  RecurringTransaction,
  CreateRecurringTransactionRequest,
  MonthlySimulation
} from '@/types/planner';

export const plannerService = {
  // ===== FINANCIAL GOALS =====

  /**
   * Obtém todas as metas financeiras do usuário
   */
  getFinancialGoals: async () => {
    return apiFetch<FinancialGoal[]>('/planner/goals');
  },

  /**
   * Obtém apenas as metas ativas
   */
  getActiveFinancialGoals: async () => {
    return apiFetch<FinancialGoal[]>('/planner/goals/active');
  },

  /**
   * Obtém uma meta específica por ID
   */
  getFinancialGoalById: async (id: string) => {
    return apiFetch<FinancialGoal>(`/planner/goals/${id}`);
  },

  /**
   * Cria uma nova meta financeira
   */
  createFinancialGoal: async (request: CreateFinancialGoalRequest) => {
    return apiFetch<FinancialGoal>('/planner/goals', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  /**
   * Atualiza uma meta existente
   */
  updateFinancialGoal: async (id: string, request: UpdateFinancialGoalRequest) => {
    return apiFetch<FinancialGoal>(`/planner/goals/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    });
  },

  /**
   * Adiciona uma contribuição à meta
   */
  addContribution: async (id: string, request: AddContributionRequest) => {
    return apiFetch<void>(`/planner/goals/${id}/contribute`, {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  /**
   * Exclui uma meta financeira
   */
  deleteFinancialGoal: async (id: string) => {
    return apiFetch<void>(`/planner/goals/${id}`, {
      method: 'DELETE'
    });
  },

  // ===== RECURRING TRANSACTIONS =====

  /**
   * Obtém todas as transações recorrentes
   */
  getRecurringTransactions: async () => {
    return apiFetch<RecurringTransaction[]>('/planner/recurring');
  },

  /**
   * Obtém apenas as transações recorrentes ativas
   */
  getActiveRecurringTransactions: async () => {
    return apiFetch<RecurringTransaction[]>('/planner/recurring/active');
  },

  /**
   * Obtém uma transação recorrente por ID
   */
  getRecurringTransactionById: async (id: string) => {
    return apiFetch<RecurringTransaction>(`/planner/recurring/${id}`);
  },

  /**
   * Cria uma nova transação recorrente
   */
  createRecurringTransaction: async (request: CreateRecurringTransactionRequest) => {
    return apiFetch<RecurringTransaction>('/planner/recurring', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  /**
   * Exclui uma transação recorrente
   */
  deleteRecurringTransaction: async (id: string) => {
    return apiFetch<void>(`/planner/recurring/${id}`, {
      method: 'DELETE'
    });
  },

  // ===== MONTHLY SIMULATIONS =====

  /**
   * Obtém ou gera a simulação para um mês específico
   */
  getOrGenerateSimulation: async (year: number, month: number) => {
    return apiFetch<MonthlySimulation>(`/planner/simulations/${year}/${month}`);
  },

  /**
   * Força a regeneração da simulação
   */
  regenerateSimulation: async (year: number, month: number) => {
    return apiFetch<MonthlySimulation>(`/planner/simulations/${year}/${month}/regenerate`, {
      method: 'POST'
    });
  }
};
