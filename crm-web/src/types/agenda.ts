// src/types/agenda.ts

export type AppointmentType = 'Medical' | 'HomeService' | 'Payment' | 'Other';

export interface Appointment {
    id: string;
    title: string;
    description?: string;
    date: string; // ISO 8601
    type: AppointmentType;
    dailyNotificationSent: boolean;
}

export interface CreateAppointmentDto {
    title: string;
    description?: string;
    date: string; // ISO 8601
    type: AppointmentType;
}

export interface UpdateAppointmentDto {
    title?: string;
    description?: string;
    date?: string; // ISO 8601
    type?: AppointmentType;
}
