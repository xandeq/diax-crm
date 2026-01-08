import { apiFetch } from './api';

export enum PaymentMethod {
    CreditCard = 0,
    DebitCard = 1,
    Pix = 2,
    Cash = 3,
    BankTransfer = 4,
    Boleto = 5,
    Other = 6
}

export interface IncomeCategory {
    id: string;
    name: string;
    isActive: boolean;
}

export interface Income {
    id: string;
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    incomeCategoryId: string;
    incomeCategoryName?: string;
    isRecurring: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateIncomeRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    incomeCategoryId: string;
    isRecurring: boolean;
}

export interface UpdateIncomeRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    incomeCategoryId: string;
    isRecurring: boolean;
}

export interface Expense {
    id: string;
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    category?: string;
    isRecurring: boolean;
    creditCardId?: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateExpenseRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    category?: string;
    isRecurring: boolean;
    creditCardId?: string;
}

export interface UpdateExpenseRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    category?: string;
    isRecurring: boolean;
    creditCardId?: string;
}

export interface CreditCard {
    id: string;
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateCreditCardRequest {
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
}

export interface UpdateCreditCardRequest {
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
}

export const financeService = {
    // Categories
    getIncomeCategories: async () => {
        return apiFetch<IncomeCategory[]>('/income-categories');
    },

    // Incomes
    getIncomes: async () => {
        return apiFetch<Income[]>('/incomes');
    },
    getIncomeById: async (id: string) => {
        return apiFetch<Income>(`/incomes/${id}`);
    },
    getIncomesByMonth: async (year: number, month: number) => {
        return apiFetch<Income[]>(`/incomes/month/${year}/${month}`);
    },
    createIncome: async (data: CreateIncomeRequest) => {
        return apiFetch<string>('/incomes', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateIncome: async (id: string, data: UpdateIncomeRequest) => {
        return apiFetch<void>(`/incomes/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteIncome: async (id: string) => {
        return apiFetch<void>(`/incomes/${id}`, {
            method: 'DELETE',
        });
    },

    // Expenses
    getExpenses: async () => {
        return apiFetch<Expense[]>('/expenses');
    },
    getExpenseById: async (id: string) => {
        return apiFetch<Expense>(`/expenses/${id}`);
    },
    getExpensesByMonth: async (year: number, month: number) => {
        return apiFetch<Expense[]>(`/expenses/month/${year}/${month}`);
    },
    createExpense: async (data: CreateExpenseRequest) => {
        return apiFetch<string>('/expenses', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateExpense: async (id: string, data: UpdateExpenseRequest) => {
        return apiFetch<void>(`/expenses/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteExpense: async (id: string) => {
        return apiFetch<void>(`/expenses/${id}`, {
            method: 'DELETE',
        });
    },

    // Credit Cards
    getCreditCards: async () => {
        return apiFetch<CreditCard[]>('/creditcards');
    },
    getCreditCardById: async (id: string) => {
        return apiFetch<CreditCard>(`/creditcards/${id}`);
    },
    createCreditCard: async (data: CreateCreditCardRequest) => {
        return apiFetch<string>('/creditcards', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateCreditCard: async (id: string, data: UpdateCreditCardRequest) => {
        return apiFetch<void>(`/creditcards/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteCreditCard: async (id: string) => {
        return apiFetch<void>(`/creditcards/${id}`, {
            method: 'DELETE',
        });
    },
};
