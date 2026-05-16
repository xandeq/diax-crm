export type TaskStatus = 'Todo' | 'InProgress' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface TaskItem {
    id: string;
    title: string;
    description?: string;
    status: TaskStatus;
    priority: TaskPriority;
    dueDate?: string;
    completedAt?: string;
    isArchived: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface CreateTaskRequest {
    title: string;
    description?: string;
    priority: TaskPriority;
    dueDate?: string;
}

export interface UpdateTaskRequest {
    title: string;
    description?: string;
    priority: TaskPriority;
    status: TaskStatus;
    dueDate?: string;
}

export interface TasksQuery {
    status?: TaskStatus;
    priority?: TaskPriority;
    includeArchived?: boolean;
    overdueOnly?: boolean;
}
