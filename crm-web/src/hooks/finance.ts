'use client';

import {
    financeService,
    FinancialFilters,
    TransactionFilters,
} from '@/services/finance';
import { plannerService } from '@/services/plannerService';
import { CreateRecurringTransactionRequest, UpdateRecurringTransactionRequest } from '@/types/planner';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

// ─── Query keys ───────────────────────────────────────────────────────────────

export const financeKeys = {
    all: ['finance'] as const,
    creditCards: () => [...financeKeys.all, 'credit-cards'] as const,
    creditCard: (id: string) => [...financeKeys.all, 'credit-card', id] as const,
    creditCardInvoices: (id: string) => [...financeKeys.all, 'credit-card-invoices', id] as const,
    invoiceTransactions: (invoiceId: string) => [...financeKeys.all, 'invoice-transactions', invoiceId] as const,
    accounts: () => [...financeKeys.all, 'accounts'] as const,
    categories: () => [...financeKeys.all, 'categories'] as const,
    transactions: (filters?: TransactionFilters) => [...financeKeys.all, 'transactions', filters] as const,
    summary: (filters?: FinancialFilters) => [...financeKeys.all, 'summary', filters] as const,
    recurringTransactions: () => [...financeKeys.all, 'recurring-transactions'] as const,
} as const;

// ─── Read hooks ────────────────────────────────────────────────────────────────

export function useCreditCards() {
    return useQuery({
        queryKey: financeKeys.creditCards(),
        queryFn: () => financeService.getCreditCards(),
    });
}

export function useFinancialAccounts() {
    return useQuery({
        queryKey: financeKeys.accounts(),
        queryFn: () => financeService.getFinancialAccounts(),
    });
}

export function useTransactionCategories() {
    return useQuery({
        queryKey: financeKeys.categories(),
        queryFn: () => financeService.getAllTransactionCategories(),
        staleTime: 5 * 60 * 1000,
    });
}

export function useTransactions(filters: TransactionFilters) {
    return useQuery({
        queryKey: financeKeys.transactions(filters),
        queryFn: () => financeService.getTransactions(filters),
    });
}

export function useFinancialSummary(filters?: FinancialFilters) {
    return useQuery({
        queryKey: financeKeys.summary(filters),
        queryFn: () => financeService.getFinancialSummary(filters ?? {}),
    });
}

// ─── Mutation hooks ────────────────────────────────────────────────────────────

export function useDeleteTransaction() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => financeService.deleteTransaction(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.transactions() }),
    });
}

export function useDeleteTransactionsBulk() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (ids: string[]) => financeService.deleteTransactionsBulk(ids),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.transactions() }),
    });
}

export function useDeleteFinancialAccount() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => financeService.deleteFinancialAccount(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.accounts() }),
    });
}

export function useDeleteCreditCard() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => financeService.deleteCreditCard(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.creditCards() }),
    });
}

// ─── Credit card detail hooks ─────────────────────────────────────────────────

export function useCreditCardDetail(id: string | null) {
    return useQuery({
        queryKey: financeKeys.creditCard(id ?? ''),
        queryFn: () => financeService.getCreditCardById(id!),
        enabled: !!id,
    });
}

export function useCreditCardInvoices(id: string | null) {
    return useQuery({
        queryKey: financeKeys.creditCardInvoices(id ?? ''),
        queryFn: () => financeService.getInvoicesByCreditCard(id!),
        enabled: !!id,
        select: (data) => [...data].sort((a, b) => {
            if (a.referenceYear !== b.referenceYear) return b.referenceYear - a.referenceYear;
            return b.referenceMonth - a.referenceMonth;
        }),
    });
}

export function useInvoiceTransactions(invoiceId: string | null) {
    return useQuery({
        queryKey: financeKeys.invoiceTransactions(invoiceId ?? ''),
        queryFn: () => financeService.getTransactions({ creditCardInvoiceId: invoiceId!, page: 1, pageSize: 100 }),
        enabled: !!invoiceId,
    });
}

// ─── Recurring transactions hooks ─────────────────────────────────────────────

export function useRecurringTransactions() {
    return useQuery({
        queryKey: financeKeys.recurringTransactions(),
        queryFn: () => plannerService.getRecurringTransactions(),
    });
}

export function useCreateRecurringTransaction() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (req: CreateRecurringTransactionRequest) => plannerService.createRecurringTransaction(req),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.recurringTransactions() }),
    });
}

export function useUpdateRecurringTransaction() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, req }: { id: string; req: UpdateRecurringTransactionRequest }) =>
            plannerService.updateRecurringTransaction(id, req),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.recurringTransactions() }),
    });
}

export function useDeleteRecurringTransaction() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => plannerService.deleteRecurringTransaction(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: financeKeys.recurringTransactions() }),
    });
}
