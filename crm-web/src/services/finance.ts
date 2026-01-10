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

export enum CardBrand {
    Unknown = 0,
    Visa = 1,
    Mastercard = 2,
    Elo = 3,
    Amex = 4,
    Hipercard = 5,
    Diners = 6,
    Discover = 7,
    JCB = 8
}

export enum CardKind {
    Physical = 1,
    Virtual = 2
}

export enum ExpenseStatus {
    Pending = 1,
    Paid = 2
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
    financialAccountId: string;
    financialAccountName?: string;
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
    financialAccountId: string;
}

export interface UpdateIncomeRequest {
    description: string;
    amount: number;
    date: string;
    paymentMethod: PaymentMethod;
    incomeCategoryId: string;
    isRecurring: boolean;
    financialAccountId: string;
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
    status: ExpenseStatus;
    paidDate?: string;
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
    status?: ExpenseStatus;
    paidDate?: string;
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
    status?: ExpenseStatus;
    paidDate?: string;
}

export interface CreditCard {
    id: string;
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
    brand: CardBrand;
    cardKind: CardKind;
    isActive: boolean;
    creditCardGroupId?: string;
    creditCardGroupName?: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateCreditCardRequest {
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
    brand?: CardBrand;
    cardKind?: CardKind;
    isActive?: boolean;
    creditCardGroupId?: string;
}

export interface UpdateCreditCardRequest {
    name: string;
    lastFourDigits: string;
    closingDay: number;
    dueDay: number;
    limit: number;
    brand?: CardBrand;
    cardKind?: CardKind;
    isActive?: boolean;
    creditCardGroupId?: string;
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
    creditCardGroupId: string;
    creditCardGroupName: string;
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
    creditCardId?: string;
    creditCardGroupId?: string;
    referenceMonth: number;
    referenceYear: number;
}

export interface PayCreditCardInvoiceRequest {
    paymentDate: string;
    paidFromAccountId?: string;
}

export interface CreditCardGroup {
    id: string;
    name: string;
    bank: string;
    closingDay: number;
    dueDay: number;
    sharedLimit: number;
    isActive: boolean;
    totalCardLimits: number;
    availableLimit: number;
    cardCount: number;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateCreditCardGroupRequest {
    name: string;
    bank: string;
    closingDay: number;
    dueDay: number;
    sharedLimit: number;
    isActive?: boolean;
}

export interface UpdateCreditCardGroupRequest {
    name: string;
    bank: string;
    closingDay: number;
    dueDay: number;
    sharedLimit: number;
    isActive: boolean;
}

export interface FinancialSummary {
    startDate: string;
    endDate: string;
    totalIncome: number;
    totalExpenses: number;
    totalPaidExpenses: number;
    totalPendingExpenses: number;
    pendingCash: number;
    pendingCredit: number;
    netCashFlow: number;
    projectedCashFlow: number;
}

export interface FinancialSummaryRequest {
    startDate?: string;
    endDate?: string;
}

export interface AccountTransfer {
    id: string;
    fromFinancialAccountId: string;
    fromAccountName: string;
    toFinancialAccountId: string;
    toAccountName: string;
    amount: number;
    date: string;
    description: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateAccountTransferRequest {
    fromFinancialAccountId: string;
    toFinancialAccountId: string;
    amount: number;
    date: string;
    description: string;
}

export interface UpdateAccountTransferRequest {
    fromFinancialAccountId: string;
    toFinancialAccountId: string;
    amount: number;
    date: string;
    description: string;
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
    markExpenseAsPaid: async (id: string, paidDate?: string) => {
        return apiFetch<void>(`/expenses/${id}/mark-paid`, {
            method: 'POST',
            body: JSON.stringify({ paidDate }),
        });
    },
    markExpenseAsPending: async (id: string) => {
        return apiFetch<void>(`/expenses/${id}/mark-pending`, {
            method: 'POST',
        });
    },
    getExpensesByStatus: async (status: ExpenseStatus) => {
        return apiFetch<Expense[]>(`/expenses/status/${status}`);
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

    // Credit Card Groups
    getCreditCardGroups: async () => {
        return apiFetch<CreditCardGroup[]>('/creditcardgroups');
    },
    getActiveCreditCardGroups: async () => {
        return apiFetch<CreditCardGroup[]>('/creditcardgroups/active');
    },
    getCreditCardGroupById: async (id: string) => {
        return apiFetch<CreditCardGroup>(`/creditcardgroups/${id}`);
    },
    createCreditCardGroup: async (data: CreateCreditCardGroupRequest) => {
        return apiFetch<string>('/creditcardgroups', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateCreditCardGroup: async (id: string, data: UpdateCreditCardGroupRequest) => {
        return apiFetch<void>(`/creditcardgroups/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteCreditCardGroup: async (id: string) => {
        return apiFetch<void>(`/creditcardgroups/${id}`, {
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

    // Financial Summary
    getFinancialSummary: async (params?: FinancialSummaryRequest) => {
        const queryParams = new URLSearchParams();
        if (params?.startDate) queryParams.append('startDate', params.startDate);
        if (params?.endDate) queryParams.append('endDate', params.endDate);
        const queryString = queryParams.toString();
        return apiFetch<FinancialSummary>(`/finance/summary${queryString ? '?' + queryString : ''}`);
    },

    // Account Transfers
    getAccountTransfers: async () => {
        return apiFetch<AccountTransfer[]>('/accounttransfers');
    },
    getAccountTransferById: async (id: string) => {
        return apiFetch<AccountTransfer>(`/accounttransfers/${id}`);
    },
    getAccountTransfersByAccountId: async (accountId: string) => {
        return apiFetch<AccountTransfer[]>(`/accounttransfers/account/${accountId}`);
    },
    getAccountTransfersByDateRange: async (startDate: string, endDate: string) => {
        return apiFetch<AccountTransfer[]>(`/accounttransfers/daterange?startDate=${startDate}&endDate=${endDate}`);
    },
    createAccountTransfer: async (data: CreateAccountTransferRequest) => {
        return apiFetch<string>('/accounttransfers', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateAccountTransfer: async (id: string, data: UpdateAccountTransferRequest) => {
        return apiFetch<void>(`/accounttransfers/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteAccountTransfer: async (id: string) => {
        return apiFetch<void>(`/accounttransfers/${id}`, {
            method: 'DELETE',
        });
    },
};
