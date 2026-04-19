// src/types/agenda.ts

export type AppointmentType = 'Medical' | 'HomeService' | 'Payment' | 'Other';

export interface AppointmentLabel {
    id: string;
    name: string;
    color: string;
    order: number;
}

export interface Appointment {
    id: string;
    title: string;
    description?: string;
    date: string; // ISO 8601 UTC
    type: AppointmentType;
    durationMinutes: number;
    dailyNotificationSent: boolean;
    labelId?: string;
    label?: AppointmentLabel;
    recurrenceGroupId?: string;
    isCancelled: boolean;
}

export interface CreateAppointmentDto {
    title: string;
    description?: string;
    date: string; // ISO 8601 UTC
    type: AppointmentType;
    durationMinutes?: number;
    labelId?: string;
}

export interface UpdateAppointmentDto {
    title?: string;
    description?: string;
    date?: string; // ISO 8601 UTC
    type?: AppointmentType;
    durationMinutes?: number;
    labelId?: string;
}

export interface RecurringAppointmentDto {
    title: string;
    description?: string;
    type: AppointmentType;
    labelId?: string;
    durationMinutes?: number;
    timeHHmm: string; // "10:30"
    daysOfWeek: number[]; // 0=Dom, 1=Seg...6=Sáb
    startDate: string; // "yyyy-MM-dd"
    endDate: string;   // "yyyy-MM-dd"
    excludedDates?: string[];
}

export interface CreateAppointmentLabelDto {
    name: string;
    color: string;
    order: number;
}

// AI Batch Command
export interface AiBatchCommandRequest {
    command: string;
    appointments: {
        id: string;
        title: string;
        date: string;
        labelName?: string;
    }[];
}

export interface AiBatchChange {
    id: string;
    newDate?: string;
    newTitle?: string;
    delete?: boolean;
}

export interface AiBatchResponse {
    summary: string;
    changes: AiBatchChange[];
}
