import { apiRequest, apiFetch } from '@/services/api';
import { CreateTaskRequest, TaskItem, TasksQuery, UpdateTaskRequest } from '@/types/tasks';

export const tasksService = {
    async getAll(query?: TasksQuery): Promise<TaskItem[]> {
        const params = new URLSearchParams();
        if (query?.status) params.set('status', query.status);
        if (query?.priority) params.set('priority', query.priority);
        if (query?.includeArchived) params.set('includeArchived', 'true');
        if (query?.overdueOnly) params.set('overdueOnly', 'true');
        const qs = params.toString();
        return await apiFetch<TaskItem[]>(`tasks${qs ? `?${qs}` : ''}`);
    },

    async getById(id: string): Promise<TaskItem> {
        return await apiFetch<TaskItem>(`tasks/${id}`);
    },

    async create(data: CreateTaskRequest): Promise<TaskItem> {
        return await apiRequest<TaskItem>('tasks', 'POST', data);
    },

    async update(id: string, data: UpdateTaskRequest): Promise<TaskItem> {
        return await apiRequest<TaskItem>(`tasks/${id}`, 'PUT', data);
    },

    async delete(id: string): Promise<void> {
        await apiFetch<void>(`tasks/${id}`, { method: 'DELETE' });
    },

    async complete(id: string): Promise<TaskItem> {
        return await apiRequest<TaskItem>(`tasks/${id}/complete`, 'POST');
    },

    async reopen(id: string): Promise<TaskItem> {
        return await apiRequest<TaskItem>(`tasks/${id}/reopen`, 'POST');
    },

    async archive(id: string): Promise<TaskItem> {
        return await apiRequest<TaskItem>(`tasks/${id}/archive`, 'POST');
    },
};
