'use client';

import {
    financeService,
    FinancialFilters,
    TransactionFilters,
} from '@/services/finance';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

// ─── Query keys ───────────────────────────────────────────────────────────────

export const financeKeys = {
    all: ['finance'] as const,
    creditCards: () => [...financeKeys.all, 'credit-cards'] as const,
    accounts: () => [...financeKeys.all, 'accounts'] as const,
    categories: () => [...financeKeys.all, 'categories'] as const,
    transactions: (filters?: TransactionFilters) => [...financeKeys.all, 'transactions', filters] as const,
    summary: (filters?: FinancialFilters) => [...financeKeys.all, 'summary', filters] as const,
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
