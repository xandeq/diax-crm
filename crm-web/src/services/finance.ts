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

export enum AccountType {
    Checking = 0,
    Business = 1,
    Savings = 2,
    Cash = 3,
    Investment = 4,
    DigitalWallet = 5
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
    financialAccountId?: string;
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
    financialAccountId?: string;
}

export interface UpdateIncomeRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    incomeCategoryId: string;
    isRecurring: boolean;
    financialAccountId?: string;
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
    creditCardInvoiceId?: string;
    financialAccountId?: string;
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
    creditCardInvoiceId?: string;
    financialAccountId?: string;
}

export interface UpdateExpenseRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    category?: string;
    isRecurring: boolean;
    creditCardId?: string;
    creditCardInvoiceId?: string;
    financialAccountId?: string;
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

export interface FinancialAccount {
    id: string;
    name: string;
    accountType: AccountType;
    initialBalance: number;
    balance: number;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateFinancialAccountRequest {
    name: string;
    accountType: AccountType;
    initialBalance: number;
    isActive?: boolean;
}

export interface UpdateFinancialAccountRequest {
    name: string;
    accountType: AccountType;
    isActive: boolean;
}

export interface CreditCardInvoice {
    id: string;
    creditCardId: string;
    creditCardName: string;
    referenceMonth: number;
    referenceYear: number;
    closingDate: string;
    dueDate: string;
    totalAmount: number;
    isPaid: boolean;
    paymentDate?: string;
    paidFromAccountId?: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateCreditCardInvoiceRequest {
    creditCardId: string;
    referenceMonth: number;
    referenceYear: number;
}

export interface PayCreditCardInvoiceRequest {
    paymentDate: string;
    paidFromAccountId?: string;
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

    // Financial Accounts
    getFinancialAccounts: async () => {
        return apiFetch<FinancialAccount[]>('/financialaccounts');
    },
    getActiveFinancialAccounts: async () => {
        return apiFetch<FinancialAccount[]>('/financialaccounts/active');
    },
    getFinancialAccountById: async (id: string) => {
        return apiFetch<FinancialAccount>(`/financialaccounts/${id}`);
    },
    createFinancialAccount: async (data: CreateFinancialAccountRequest) => {
        return apiFetch<string>('/financialaccounts', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateFinancialAccount: async (id: string, data: UpdateFinancialAccountRequest) => {
        return apiFetch<void>(`/financialaccounts/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteFinancialAccount: async (id: string) => {
        return apiFetch<void>(`/financialaccounts/${id}`, {
            method: 'DELETE',
        });
    },

    // Credit Card Invoices
    getCreditCardInvoices: async () => {
        return apiFetch<CreditCardInvoice[]>('/creditcardinvoices');
    },
    getUnpaidInvoices: async () => {
        return apiFetch<CreditCardInvoice[]>('/creditcardinvoices/unpaid');
    },
    getInvoicesByCreditCard: async (creditCardId: string) => {
        return apiFetch<CreditCardInvoice[]>(`/creditcardinvoices/creditcard/${creditCardId}`);
    },
    getInvoiceById: async (id: string) => {
        return apiFetch<CreditCardInvoice>(`/creditcardinvoices/${id}`);
    },
    createOrGetInvoice: async (data: CreateCreditCardInvoiceRequest) => {
        return apiFetch<string>('/creditcardinvoices', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    payInvoice: async (id: string, data: PayCreditCardInvoiceRequest) => {
        return apiFetch<void>(`/creditcardinvoices/${id}/pay`, {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    unpayInvoice: async (id: string) => {
        return apiFetch<void>(`/creditcardinvoices/${id}/unpay`, {
            method: 'POST',
        });
    },
    deleteInvoice: async (id: string) => {
        return apiFetch<void>(`/creditcardinvoices/${id}`, {
            method: 'DELETE',
        });
    },
};
