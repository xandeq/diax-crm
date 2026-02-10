import { apiFetch } from './api';

export enum PaymentMethod {
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    Pix = 3,
    BankTransfer = 4,
    Boleto = 5
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

export enum StatementImportType {
    Account = 1,
    CreditCard = 2
}

export enum ImportStatus {
    Pending = 1,
    Validating = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5
}

export enum ImportedTransactionStatus {
    Pending = 1,
    Matched = 2,
    Created = 3,
    Ignored = 4,
    Failed = 5
}

export interface IncomeCategory {
    id: string;
    name: string;
    isActive: boolean;
}

export interface ExpenseCategory {
    id: string;
    name: string;
    isActive: boolean;
}

export interface CreateIncomeCategoryRequest {
    name: string;
}

export interface UpdateIncomeCategoryRequest {
    name: string;
    isActive: boolean;
}

export interface CreateExpenseCategoryRequest {
    name: string;
}

export interface UpdateExpenseCategoryRequest {
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
    expenseCategoryId: string;
    expenseCategoryName?: string;
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
    expenseCategoryId: string;
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
    expenseCategoryId: string;
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

export interface StatementImport {
    id: string;
    importType: StatementImportType;
    fileName: string;
    status: ImportStatus;
    totalRecords: number;
    processedRecords: number;
    failedRecords: number;
    rowCount?: number; // Legacy/Migration support if needed
    processedCount?: number;
    errorMessage?: string;
    createdAt: string;
    processedAt?: string;
    financialAccountId?: string;
    financialAccountName?: string;
    creditCardGroupId?: string;
    creditCardGroupName?: string;
}

export interface ImportedTransaction {
    id: string;
    rawDescription: string;
    amount: number;
    transactionDate: string;
    date?: string; // Support for the component I wrote
    description?: string; // Support for the component I wrote
    status: ImportedTransactionStatus;
    matchedExpenseId?: string;
    createdExpenseId?: string;
    createdIncomeId?: string;
    errorMessage?: string;
}

export interface StatementImportPostPreview {
    total: number;
    expensesToCreate: number;
    incomesToCreate: number;
    alreadyCreated: number;
    toIgnore: number;
    failed: number;
}

export interface StatementImportPostResponse {
    createdExpenses: number;
    createdIncomes: number;
    skipped: number;
    failed: number;
}

export interface StatementImportDetail {
    summary: StatementImport;
    transactions: ImportedTransaction[];
}

export interface PagedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

export interface FinancialFilters {
    page?: number;
    pageSize?: number;
    search?: string;
    sortBy?: string;
    sortDescending?: boolean;
    startDate?: string;
    endDate?: string;
    categoryId?: string;
    financialAccountId?: string;
    minAmount?: number;
    maxAmount?: number;
    status?: number;
}

export interface UploadStatementRequest {
    importType: StatementImportType;
    financialAccountId?: string;
    creditCardGroupId?: string;
    creditCardId?: string;
}

export interface BulkDeleteResponse {
    success: boolean;
    deletedCount: number;
    failedCount: number;
    errors: string[];
}

export const financeService = {
    // Income Categories
    getIncomeCategories: async () => {
        return apiFetch<IncomeCategory[]>('/income-categories');
    },
    getAllIncomeCategories: async () => {
        return apiFetch<IncomeCategory[]>('/income-categories/all');
    },
    getIncomeCategoryById: async (id: string) => {
        return apiFetch<IncomeCategory>(`/income-categories/${id}`);
    },
    createIncomeCategory: async (data: CreateIncomeCategoryRequest) => {
        return apiFetch<string>('/income-categories', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateIncomeCategory: async (id: string, data: UpdateIncomeCategoryRequest) => {
        return apiFetch<void>(`/income-categories/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteIncomeCategory: async (id: string) => {
        return apiFetch<void>(`/income-categories/${id}`, {
            method: 'DELETE',
        });
    },

    // Expense Categories
    getExpenseCategories: async () => {
        return apiFetch<ExpenseCategory[]>('/expense-categories');
    },
    getAllExpenseCategories: async () => {
        return apiFetch<ExpenseCategory[]>('/expense-categories/all');
    },
    getExpenseCategoryById: async (id: string) => {
        return apiFetch<ExpenseCategory>(`/expense-categories/${id}`);
    },
    createExpenseCategory: async (data: CreateExpenseCategoryRequest) => {
        return apiFetch<string>('/expense-categories', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    updateExpenseCategory: async (id: string, data: UpdateExpenseCategoryRequest) => {
        return apiFetch<void>(`/expense-categories/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    deleteExpenseCategory: async (id: string) => {
        return apiFetch<void>(`/expense-categories/${id}`, {
            method: 'DELETE',
        });
    },

    // Incomes
    getIncomes: async (filters?: FinancialFilters) => {
        const params = new URLSearchParams();
        if (filters) {
            Object.entries(filters).forEach(([key, value]) => {
                if (value !== undefined && value !== null && value !== '') {
                    params.append(key, value.toString());
                }
            });
        }
        const query = params.toString();
        return apiFetch<PagedResponse<Income>>(`/incomes${query ? `?${query}` : ''}`);
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
    deleteIncomesBulk: async (ids: string[]) => {
        // Validar que todos os IDs são GUIDs válidos antes de enviar
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        const validIds = ids.filter(id => {
            const isValid = guidRegex.test(id);
            if (!isValid) {
                console.warn(`⚠️ ID inválido filtrado: ${id}`);
            }
            return isValid;
        });

        if (validIds.length === 0) {
            throw new Error('Nenhum ID válido foi fornecido para exclusão');
        }

        if (validIds.length !== ids.length) {
            console.warn(`⚠️ ${ids.length - validIds.length} IDs inválidos foram filtrados`);
        }

        console.log(`🔍 Enviando ${validIds.length} IDs para exclusão:`, validIds);

        return apiFetch<BulkDeleteResponse>('/incomes/bulk-delete', {
            method: 'POST',
            body: JSON.stringify({ ids: validIds }),
        });
    },

    // Expenses
    getExpenses: async (filters?: FinancialFilters) => {
        const params = new URLSearchParams();
        if (filters) {
            Object.entries(filters).forEach(([key, value]) => {
                if (value !== undefined && value !== null && value !== '') {
                    params.append(key, value.toString());
                }
            });
        }
        const query = params.toString();
        return apiFetch<PagedResponse<Expense>>(`/expenses${query ? `?${query}` : ''}`);
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
    deleteExpensesBulk: async (ids: string[]) => {
        // Validar que todos os IDs são GUIDs válidos antes de enviar
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        const validIds = ids.filter(id => {
            const isValid = guidRegex.test(id);
            if (!isValid) {
                console.warn(`⚠️ [Expenses] ID inválido filtrado: ${id}`);
            }
            return isValid;
        });

        if (validIds.length === 0) {
            throw new Error('Nenhum ID válido foi fornecido para exclusão de despesas');
        }

        if (validIds.length !== ids.length) {
            console.warn(`⚠️ [Expenses] ${ids.length - validIds.length} IDs inválidos foram filtrados`);
        }

        console.log(`🔍 [Expenses] Enviando ${validIds.length} IDs para exclusão:`, validIds);

        return apiFetch<BulkDeleteResponse>('/expenses/bulk-delete', {
            method: 'POST',
            body: JSON.stringify({ ids: validIds }),
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

    // Statement Imports
    getStatementImports: async () => {
        return apiFetch<StatementImport[]>('/StatementImports');
    },
    getStatementImportById: async (id: string) => {
        return apiFetch<StatementImportDetail>(`/StatementImports/${id}`);
    },
    uploadStatement: async (data: UploadStatementRequest, file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('ImportType', data.importType.toString());
        if (data.financialAccountId) formData.append('FinancialAccountId', data.financialAccountId);
        if (data.creditCardGroupId) formData.append('CreditCardGroupId', data.creditCardGroupId);

        return apiFetch<void>('/StatementImports/upload', {
            method: 'POST',
            body: formData,
        });
    },
    previewStatementImportPost: async (id: string) => {
        return apiFetch<StatementImportPostPreview>(`/StatementImports/${id}/preview-post`);
    },
    postStatementImport: async (id: string, data: { force: boolean }) => {
        return apiFetch<StatementImportPostResponse>(`/StatementImports/${id}/post`, {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    deleteStatementImport: async (id: string) => {
        return apiFetch<void>(`/StatementImports/${id}`, {
            method: 'DELETE',
        });
    },
};
