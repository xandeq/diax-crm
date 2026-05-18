import { apiRequest, apiFetch } from '@/services/api';

export type TicketStatus = 'Open' | 'InProgress' | 'WaitingResponse' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type TicketCategory = 'Bug' | 'FeatureRequest' | 'Question' | 'Billing' | 'Other';

export interface SupportTicket {
    id: string;
    subject: string;
    description?: string;
    status: TicketStatus;
    priority: TicketPriority;
    category: TicketCategory;
    customerId?: string;
    customerName?: string;
    resolvedAt?: string;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateTicketRequest {
    subject: string;
    description?: string;
    priority: TicketPriority;
    category: TicketCategory;
    customerId?: string;
    customerName?: string;
}

export interface UpdateTicketRequest {
    subject: string;
    description?: string;
    status: TicketStatus;
    priority: TicketPriority;
    category: TicketCategory;
    customerId?: string;
    customerName?: string;
}

export interface TicketsQuery {
    status?: TicketStatus;
    priority?: TicketPriority;
    category?: TicketCategory;
    customerId?: string;
}

export const helpdeskService = {
    async getAll(query?: TicketsQuery): Promise<SupportTicket[]> {
        const params = new URLSearchParams();
        if (query?.status) params.set('status', query.status);
        if (query?.priority) params.set('priority', query.priority);
        if (query?.category) params.set('category', query.category);
        if (query?.customerId) params.set('customerId', query.customerId);
        const qs = params.toString();
        return await apiFetch<SupportTicket[]>(`helpdesk/tickets${qs ? `?${qs}` : ''}`);
    },

    async getById(id: string): Promise<SupportTicket> {
        return await apiFetch<SupportTicket>(`helpdesk/tickets/${id}`);
    },

    async create(data: CreateTicketRequest): Promise<SupportTicket> {
        return await apiRequest<SupportTicket>('helpdesk/tickets', 'POST', data);
    },

    async update(id: string, data: UpdateTicketRequest): Promise<SupportTicket> {
        return await apiRequest<SupportTicket>(`helpdesk/tickets/${id}`, 'PUT', data);
    },

    async delete(id: string): Promise<void> {
        await apiFetch<void>(`helpdesk/tickets/${id}`, { method: 'DELETE' });
    },

    async resolve(id: string): Promise<SupportTicket> {
        return await apiRequest<SupportTicket>(`helpdesk/tickets/${id}/resolve`, 'POST');
    },

    async reopen(id: string): Promise<SupportTicket> {
        return await apiRequest<SupportTicket>(`helpdesk/tickets/${id}/reopen`, 'POST');
    },

    async close(id: string): Promise<SupportTicket> {
        return await apiRequest<SupportTicket>(`helpdesk/tickets/${id}/close`, 'POST');
    },
};
