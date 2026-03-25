import { apiFetch } from './api';

export type PersonalControlKind = 'income' | 'expense' | 'subscription';

export type PersonalControlPaymentType = 'debit' | 'credit';

export type PersonalControlBillingFrequency = 'weekly' | 'monthly' | 'quarterly' | 'yearly';

export interface PersonalControlPeriod {
  year: number;
  month: number;
  label: string;
}

export interface PersonalControlSummary {
  totalIncome: number;
  totalExpenses: number;
  totalCreditExpenses: number;
  remainingBalance: number;
  paidAmount: number;
  unpaidAmount: number;
  withCardAmount: number;
  withoutCardAmount: number;
  paidCount: number;
  unpaidCount: number;
}

export interface PersonalControlCardSummary {
  creditCardId: string;
  creditCardName: string;
  totalAmount: number;
  paidAmount: number;
  pendingAmount: number;
  itemCount: number;
}

export interface PersonalControlIncomeItem {
  id: string;
  name: string;
  amount: number;
  dayOfMonth: number;
  isRecurring: boolean;
  isPaid: boolean;
  paymentDate?: string;
  details?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface PersonalControlExpenseItem {
  id: string;
  name: string;
  amount: number;
  paymentType: PersonalControlPaymentType;
  dueDay: number;
  isPaid: boolean;
  paymentDate?: string;
  details?: string;
  creditCardId?: string;
  creditCardName?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface PersonalControlSubscriptionItem {
  id: string;
  name: string;
  amount: number;
  billingFrequency: PersonalControlBillingFrequency;
  paymentType: PersonalControlPaymentType;
  isPaid: boolean;
  paymentDate?: string;
  details?: string;
  creditCardId?: string;
  creditCardName?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface PersonalControlMonthView {
  period: PersonalControlPeriod;
  summary: PersonalControlSummary;
  incomes: PersonalControlIncomeItem[];
  expenses: PersonalControlExpenseItem[];
  subscriptions: PersonalControlSubscriptionItem[];
  cardSummaries: PersonalControlCardSummary[];
}

export interface CreatePersonalControlIncomeRequest {
  name: string;
  amount: number;
  dayOfMonth: number;
  isRecurring?: boolean;
  details?: string;
}

export interface UpdatePersonalControlIncomeRequest extends CreatePersonalControlIncomeRequest {}

export interface CreatePersonalControlExpenseRequest {
  name: string;
  amount: number;
  paymentType: PersonalControlPaymentType;
  dueDay: number;
  details?: string;
  creditCardId?: string;
}

export interface UpdatePersonalControlExpenseRequest extends CreatePersonalControlExpenseRequest {}

export interface CreatePersonalControlSubscriptionRequest {
  name: string;
  amount: number;
  billingFrequency: PersonalControlBillingFrequency;
  paymentType: PersonalControlPaymentType;
  details?: string;
  creditCardId?: string;
}

export interface UpdatePersonalControlSubscriptionRequest extends CreatePersonalControlSubscriptionRequest {}

export interface TogglePersonalControlStatusRequest {
  isPaid: boolean;
  paymentDate?: string;
}

const basePath = '/personal-control';

export const personalControlService = {
  getMonthView: async (year: number, month: number) => {
    return apiFetch<PersonalControlMonthView>(`${basePath}/months/${year}/${month}`);
  },

  createIncome: async (request: CreatePersonalControlIncomeRequest) => {
    return apiFetch<string>(`${basePath}/incomes`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateIncome: async (id: string, request: UpdatePersonalControlIncomeRequest) => {
    return apiFetch<void>(`${basePath}/incomes/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteIncome: async (id: string) => {
    return apiFetch<void>(`${basePath}/incomes/${id}`, {
      method: 'DELETE',
    });
  },

  toggleIncomeStatus: async (id: string, request: TogglePersonalControlStatusRequest) => {
    return apiFetch<void>(`${basePath}/incomes/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify(request),
    });
  },

  createExpense: async (request: CreatePersonalControlExpenseRequest) => {
    return apiFetch<string>(`${basePath}/expenses`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateExpense: async (id: string, request: UpdatePersonalControlExpenseRequest) => {
    return apiFetch<void>(`${basePath}/expenses/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteExpense: async (id: string) => {
    return apiFetch<void>(`${basePath}/expenses/${id}`, {
      method: 'DELETE',
    });
  },

  toggleExpenseStatus: async (id: string, request: TogglePersonalControlStatusRequest) => {
    return apiFetch<void>(`${basePath}/expenses/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify(request),
    });
  },

  createSubscription: async (request: CreatePersonalControlSubscriptionRequest) => {
    return apiFetch<string>(`${basePath}/subscriptions`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateSubscription: async (id: string, request: UpdatePersonalControlSubscriptionRequest) => {
    return apiFetch<void>(`${basePath}/subscriptions/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteSubscription: async (id: string) => {
    return apiFetch<void>(`${basePath}/subscriptions/${id}`, {
      method: 'DELETE',
    });
  },

  toggleSubscriptionStatus: async (id: string, request: TogglePersonalControlStatusRequest) => {
    return apiFetch<void>(`${basePath}/subscriptions/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify(request),
    });
  },
};
