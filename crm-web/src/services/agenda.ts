// src/services/agenda.ts
import { apiFetch, apiRequest } from '@/services/api';
import { AiBatchCommandRequest, AiBatchResponse, Appointment, CreateAppointmentDto, RecurringAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';

export const agendaService = {
    async create(data: CreateAppointmentDto): Promise<Appointment> {
        return await apiRequest<Appointment>('appointments', 'POST', data);
    },

    async getById(id: string): Promise<Appointment> {
        return await apiRequest<Appointment>(`appointments/${id}`, 'GET');
    },

    async getByDateRange(startDate: string, endDate: string): Promise<Appointment[]> {
        return await apiFetch<Appointment[]>(`appointments?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`);
    },

    async update(id: string, data: UpdateAppointmentDto): Promise<Appointment> {
        return await apiRequest<Appointment>(`appointments/${id}`, 'PUT', data);
    },

    async delete(id: string, scope: 'one' | 'forward' | 'all' = 'one'): Promise<void> {
        await apiFetch<void>(`appointments/${id}?scope=${scope}`, { method: 'DELETE' });
    },

    async createRecurring(data: RecurringAppointmentDto): Promise<Appointment[]> {
        return await apiRequest<Appointment[]>('appointments/recurring', 'POST', data);
    },

    async importFromText(text: string): Promise<CreateAppointmentDto[]> {
        return await apiRequest<CreateAppointmentDto[]>('appointments/import-text', 'POST', { text });
    },

    async aiBatchCommand(data: AiBatchCommandRequest): Promise<AiBatchResponse> {
        return await apiRequest('appointments/ai-batch-command', 'POST', data);
    }
};
