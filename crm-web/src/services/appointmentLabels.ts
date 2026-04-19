import { apiRequest } from '@/services/api';
import { AppointmentLabel, CreateAppointmentLabelDto } from '@/types/agenda';

export const appointmentLabelsService = {
    async getAll(): Promise<AppointmentLabel[]> {
        return await apiRequest<AppointmentLabel[]>('appointment-labels', 'GET');
    },

    async create(data: CreateAppointmentLabelDto): Promise<AppointmentLabel> {
        return await apiRequest<AppointmentLabel>('appointment-labels', 'POST', data);
    },

    async update(id: string, data: CreateAppointmentLabelDto): Promise<AppointmentLabel> {
        return await apiRequest<AppointmentLabel>(`appointment-labels/${id}`, 'PUT', data);
    },

    async delete(id: string): Promise<void> {
        await apiRequest<void>(`appointment-labels/${id}`, 'DELETE');
    }
};
