// ===== ENUMS =====

export enum GoalCategory {
  Emergency = 1,
  Baby = 2,
  House = 3,
  Travel = 4,
  Investment = 5,
  Debt = 6,
  Other = 99
}

export enum FrequencyType {
  Daily = 1,
  Weekly = 2,
  Monthly = 3,
  Quarterly = 4,
  Yearly = 5
}

export enum TransactionType {
  Income = 1,
  Expense = 2
}

export enum SimulationStatus {
  Draft = 1,
  Active = 2,
  Archived = 3
}

export enum RecommendationType {
  DeferExpense = 1,
  ChangeCard = 2,
  IncreaseIncome = 3,
  Alert = 4,
  OptimizePayment = 5
}

export enum PaymentMethod {
  Cash = 1,
  DebitCard = 2,
  CreditCard = 3,
  BankTransfer = 4,
  Pix = 5,
  Check = 6,
  Other = 99
}

// ===== FINANCIAL GOALS =====

export interface FinancialGoal {
  id: string;
  userId: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  targetDate?: string;
  category: GoalCategory;
  priority: number;
  isActive: boolean;
  autoAllocateSurplus: boolean;
  progress: number;
  remainingAmount: number;
  isCompleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateFinancialGoalRequest {
  name: string;
  targetAmount: number;
  currentAmount?: number;
  targetDate?: string;
  category: GoalCategory;
  priority?: number;
  autoAllocateSurplus: boolean;
}

export interface UpdateFinancialGoalRequest {
  name: string;
  targetAmount: number;
  targetDate?: string;
  category: GoalCategory;
  priority: number;
  isActive: boolean;
  autoAllocateSurplus: boolean;
}

export interface AddContributionRequest {
  amount: number;
}

// ===== RECURRING TRANSACTIONS =====

export interface RecurringTransaction {
  id: string;
  userId: string;
  type: TransactionType;
  description: string;
  amount: number;
  categoryId: string;
  frequencyType: FrequencyType;
  dayOfMonth: number;
  startDate: string;
  endDate?: string;
  paymentMethod: PaymentMethod;
  creditCardId?: string;
  financialAccountId?: string;
  isActive: boolean;
  priority: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRecurringTransactionRequest {
  type: TransactionType;
  description: string;
  amount: number;
  categoryId: string;
  frequencyType?: FrequencyType;
  dayOfMonth: number;
  startDate?: string;
  endDate?: string;
  paymentMethod: PaymentMethod;
  creditCardId?: string;
  financialAccountId?: string;
  priority?: number;
}

// ===== MONTHLY SIMULATIONS =====

export interface MonthlySimulation {
  id: string;
  userId: string;
  referenceMonth: number;
  referenceYear: number;
  simulationDate: string;
  startingBalance: number;
  projectedEndingBalance: number;
  totalProjectedIncome: number;
  totalProjectedExpenses: number;
  hasNegativeBalanceRisk: boolean;
  firstNegativeBalanceDate?: string;
  lowestProjectedBalance: number;
  status: SimulationStatus;
  dailyBalances: DailyBalance[];
  recommendations: Recommendation[];
}

export interface DailyBalance {
  date: string;
  openingBalance: number;
  totalIncome: number;
  totalExpenses: number;
  closingBalance: number;
  isNegative: boolean;
  riskLevel: 'Safe' | 'Warning' | 'Critical';
}

export interface Recommendation {
  type: RecommendationType;
  priority: number;
  title: string;
  message: string;
}

// ===== HELPER TYPES =====

export interface PlannerFilters {
  month: number;
  year: number;
}

export const GOAL_CATEGORY_LABELS: Record<GoalCategory, string> = {
  [GoalCategory.Emergency]: 'Emergência',
  [GoalCategory.Baby]: 'Bebê',
  [GoalCategory.House]: 'Casa',
  [GoalCategory.Travel]: 'Viagem',
  [GoalCategory.Investment]: 'Investimento',
  [GoalCategory.Debt]: 'Dívida',
  [GoalCategory.Other]: 'Outro'
};

export const FREQUENCY_TYPE_LABELS: Record<FrequencyType, string> = {
  [FrequencyType.Daily]: 'Diário',
  [FrequencyType.Weekly]: 'Semanal',
  [FrequencyType.Monthly]: 'Mensal',
  [FrequencyType.Quarterly]: 'Trimestral',
  [FrequencyType.Yearly]: 'Anual'
};

export const TRANSACTION_TYPE_LABELS: Record<TransactionType, string> = {
  [TransactionType.Income]: 'Receita',
  [TransactionType.Expense]: 'Despesa'
};
