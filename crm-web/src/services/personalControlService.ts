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
  totalCardStatements: number;
  totalCardPaid: number;
  totalCardPending: number;
  cardsPaidCount: number;
  cardsPendingCount: number;
  totalToPay: number;
  availableToInvest: number;
}

export interface PersonalControlCardSummary {
  creditCardId: string;
  creditCardName: string;
  totalAmount: number;
  paidAmount: number;
  pendingAmount: number;
  itemCount: number;
  statementAmount?: number;
  invoiceId?: string;
  invoicePaid?: boolean;
  invoicePaymentDate?: string;
  creditLimit?: number;
  availableCredit?: number;
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
  hasVariableAmount?: boolean;
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
  hasVariableAmount?: boolean;
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
  invoicesDueThisMonth: PersonalControlInvoiceDueThisMonth[];
}

export interface CreatePersonalControlIncomeRequest {
  year: number;
  month: number;
  name: string;
  amount: number;
  dayOfMonth: number;
  isRecurring?: boolean;
  isPaid?: boolean;
  paymentDate?: string;
  details?: string;
}

export interface UpdatePersonalControlIncomeRequest extends CreatePersonalControlIncomeRequest {}

export interface CreatePersonalControlExpenseRequest {
  year: number;
  month: number;
  name: string;
  amount: number;
  paymentType: PersonalControlPaymentType;
  dueDay: number;
  isPaid?: boolean;
  paymentDate?: string;
  details?: string;
  creditCardId?: string;
  hasVariableAmount?: boolean;
}

export interface UpdatePersonalControlExpenseRequest extends CreatePersonalControlExpenseRequest {}

export interface CreatePersonalControlSubscriptionRequest {
  year: number;
  month: number;
  name: string;
  amount: number;
  billingFrequency: PersonalControlBillingFrequency;
  paymentType: PersonalControlPaymentType;
  isPaid?: boolean;
  paymentDate?: string;
  details?: string;
  creditCardId?: string;
  hasVariableAmount?: boolean;
}

export interface UpdatePersonalControlSubscriptionRequest extends CreatePersonalControlSubscriptionRequest {}

export interface TogglePersonalControlStatusRequest {
  year?: number;
  month?: number;
  isPaid: boolean;
  paymentDate?: string;
}

export interface InvoiceTransactionItem {
  transactionId: string;
  description: string;
  amount: number;
  date: string;
  isPaid: boolean;
  creditCardId?: string;
  creditCardName?: string;
}

export interface LinkedSubscriptionPreview {
  templateId: string;
  description: string;
  amount: number;
  hasVariableAmount: boolean;
  creditCardId?: string;
  creditCardName?: string;
}

export interface PersonalControlInvoiceDueThisMonth {
  invoiceId: string;
  creditCardGroupId: string;
  creditCardGroupName: string;
  dueDate: string;
  referenceMonth: number;
  referenceYear: number;
  totalTransactionsAmount: number;
  statementAmount?: number;
  isPaid: boolean;
  paymentDate?: string;
  transactions: InvoiceTransactionItem[];
  linkedSubscriptions: LinkedSubscriptionPreview[];
}

export interface CopyRecurringItem {
  templateId: string;
  description: string;
  amount: number;
  createdTransactionId?: string;
  skipReason?: 'AlreadyExists' | 'CreditCardSkipped' | 'NoInvoiceFound' | 'MissingAccount' | 'InvalidAccount' | 'UnsupportedType' | 'BeforeStartDate' | string | null;
  hasVariableAmount?: boolean;
}

export interface CopyRecurringMonthResult {
  year: number;
  month: number;
  created: CopyRecurringItem[];
  skipped: CopyRecurringItem[];
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
      method: 'POST',
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
      method: 'POST',
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
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  setCardStatementAmount: async (invoiceId: string, amount: number | null) => {
    return apiFetch<void>(`/credit-card-invoices/${invoiceId}/statement`, {
      method: 'POST',
      body: JSON.stringify({ amount }),
    });
  },

  payCardInvoice: async (invoiceId: string, paymentDate: string, statementAmount?: number) => {
    return apiFetch<void>(`/credit-card-invoices/${invoiceId}/pay`, {
      method: 'POST',
      body: JSON.stringify({ paymentDate, statementAmount }),
    });
  },

  importFromSheet: async (year: number, month: number) => {
    return apiFetch<{
      matchedCards: number;
      unmatchedCards: number;
      results: Array<{
        sheetName: string;
        matchedCrmName?: string;
        amount?: number;
        matched: boolean;
        error?: string;
      }>;
    }>(`${basePath}/import-sheet/${year}/${month}`, { method: 'POST' });
  },

  createInvoice: async (creditCardGroupId: string, year: number, month: number) => {
    return apiFetch<string>(`/credit-card-invoices`, {
      method: 'POST',
      body: JSON.stringify({ creditCardGroupId, referenceYear: year, referenceMonth: month }),
    });
  },

  parseStatement: async (file: File): Promise<{ transactions: Array<{ description: string; amount: number; date: string }> }> => {
    const formData = new FormData();
    formData.append('file', file);
    return apiFetch<{ transactions: Array<{ description: string; amount: number; date: string }> }>(
      `${basePath}/parse-statement`,
      { method: 'POST', body: formData }
    );
  },

  copyRecurring: async (year: number, month: number): Promise<CopyRecurringMonthResult> => {
    return apiFetch<CopyRecurringMonthResult>(`${basePath}/copy-recurring/${year}/${month}`, {
      method: 'POST',
    });
  },
};

// ─── InvestIQ Integration ────────────────────────────────────────────────────

export interface InvestIQAllocationItem {
  asset_class: string;
  total_value: number;
  percentage: number;
}

export interface InvestIQPortfolioSummary {
  portfolio_value: number;
  total_invested: number;
  unrealized_pnl: number;
  realized_pnl: number;
  total_return_pct: number | null;
  monthly_dividends: number;
  position_count: number;
  asset_allocation: InvestIQAllocationItem[];
  cached_at: string;
  configured?: boolean;
}

export const investiqService = {
  getPortfolioSummary: async (): Promise<InvestIQPortfolioSummary | null> => {
    try {
      return await apiFetch<InvestIQPortfolioSummary>('/planner/investiq/portfolio-summary');
    } catch {
      return null;
    }
  },
};
