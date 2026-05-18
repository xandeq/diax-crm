'use client';

import {
    helpdeskService,
    CreateTicketRequest,
    UpdateTicketRequest,
    TicketsQuery,
} from '@/services/helpdesk';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

// ─── Query keys ───────────────────────────────────────────────────────────────

export const helpdeskKeys = {
    all: ['helpdesk'] as const,
    tickets: (query?: TicketsQuery) => [...helpdeskKeys.all, 'tickets', query] as const,
} as const;

// ─── Read hooks ────────────────────────────────────────────────────────────────

export function useTickets(query?: TicketsQuery) {
    return useQuery({
        queryKey: helpdeskKeys.tickets(query),
        queryFn: () => helpdeskService.getAll(query),
    });
}

export function useCustomerTickets(customerId: string) {
    return useQuery({
        queryKey: helpdeskKeys.tickets({ customerId }),
        queryFn: () => helpdeskService.getAll({ customerId }),
        enabled: !!customerId,
    });
}

// ─── Mutation hooks ────────────────────────────────────────────────────────────

export function useCreateTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (req: CreateTicketRequest) => helpdeskService.create(req),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}

export function useUpdateTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: ({ id, req }: { id: string; req: UpdateTicketRequest }) =>
            helpdeskService.update(id, req),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}

export function useDeleteTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => helpdeskService.delete(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}

export function useResolveTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => helpdeskService.resolve(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}

export function useReopenTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => helpdeskService.reopen(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}

export function useCloseTicket() {
    const qc = useQueryClient();
    return useMutation({
        mutationFn: (id: string) => helpdeskService.close(id),
        onSuccess: () => qc.invalidateQueries({ queryKey: helpdeskKeys.all }),
    });
}
