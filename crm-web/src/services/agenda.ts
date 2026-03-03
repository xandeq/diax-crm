// src/services/agenda.ts
import { apiFetch, apiRequest } from '@/services/api';
import { Appointment, CreateAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';

export const agendaService = {
    async create(data: CreateAppointmentDto): Promise<Appointment> {
        return await apiRequest<Appointment>('appointments', 'POST', data);
    },

    async getById(id: string): Promise<Appointment> {
        return await apiRequest<Appointment>(`appointments/${id}`, 'GET');
    },

    async getByDateRange(startDate: string, endDate: string): Promise<Appointment[]> {
        // using apiFetch to pass query params in the URL since apiRequest focuses on method/body
        return await apiFetch<Appointment[]>(`appointments?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}`);
    },

    async update(id: string, data: UpdateAppointmentDto): Promise<Appointment> {
        return await apiRequest<Appointment>(`appointments/${id}`, 'PUT', data);
    },

    async delete(id: string): Promise<void> {
        await apiRequest<void>(`appointments/${id}`, 'DELETE');
    },

    async importFromText(text: string): Promise<CreateAppointmentDto[]> {
        return await apiRequest<CreateAppointmentDto[]>('appointments/import-text', 'POST', { prompt: text });
    }
};
