import {
    ChecklistCategory,
    ChecklistItem,
    ChecklistItemBulkRequest,
    ChecklistItemsQuery,
    CreateChecklistCategoryRequest,
    CreateChecklistItemRequest,
    PagedResponse,
    UpdateChecklistCategoryRequest,
    UpdateChecklistItemRequest
} from '@/types/household';
import { apiFetch } from './api';

export const checklistService = {
  // Categories
  getCategories: async () => {
    return apiFetch<ChecklistCategory[]>('/Checklists/categories');
  },

  createCategory: async (request: CreateChecklistCategoryRequest) => {
    return apiFetch<ChecklistCategory>('/Checklists/categories', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  updateCategory: async (id: string, request: UpdateChecklistCategoryRequest) => {
    return apiFetch<ChecklistCategory>(`/Checklists/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    });
  },

  deleteCategory: async (id: string) => {
    return apiFetch<void>(`/Checklists/categories/${id}`, {
      method: 'DELETE'
    });
  },

  // Items
  getItems: async (query: ChecklistItemsQuery) => {
    const params = new URLSearchParams();
    if (query.categoryId) params.append('categoryId', query.categoryId);
    if (query.status !== undefined) params.append('status', query.status.toString());
    if (query.priority !== undefined) params.append('priority', query.priority.toString());
    if (query.q) params.append('q', query.q);
    if (query.dateFrom) params.append('dateFrom', query.dateFrom);
    if (query.dateTo) params.append('dateTo', query.dateTo);
    if (query.includeArchived) params.append('includeArchived', 'true');
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.sortDir) params.append('sortDir', query.sortDir);

    return apiFetch<PagedResponse<ChecklistItem>>(`/Checklists/items?${params.toString()}`);
  },

  createItem: async (request: CreateChecklistItemRequest) => {
    return apiFetch<ChecklistItem>('/Checklists/items', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  updateItem: async (id: string, request: UpdateChecklistItemRequest) => {
    return apiFetch<ChecklistItem>(`/Checklists/items/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request)
    });
  },

  deleteItem: async (id: string) => {
    return apiFetch<void>(`/Checklists/items/${id}`, {
      method: 'DELETE'
    });
  },

  markBought: async (id: string, actualPrice?: number) => {
    return apiFetch<void>(`/Checklists/items/${id}/mark-bought`, {
      method: 'POST',
      body: JSON.stringify({ actualPrice })
    });
  },

  markCanceled: async (id: string) => {
    return apiFetch<void>(`/Checklists/items/${id}/mark-canceled`, {
      method: 'POST'
    });
  },

  reactivate: async (id: string) => {
    return apiFetch<void>(`/Checklists/items/${id}/reactivate`, {
      method: 'POST'
    });
  },

  archive: async (id: string) => {
    return apiFetch<void>(`/Checklists/items/${id}/archive`, {
      method: 'POST'
    });
  },

  unarchive: async (id: string) => {
    return apiFetch<void>(`/Checklists/items/${id}/unarchive`, {
      method: 'POST'
    });
  },

  bulkAction: async (request: ChecklistItemBulkRequest) => {
    return apiFetch<{ affectedCount: number }>('/Checklists/items/bulk', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  },

  importItems: async (items: any[]) => {
    return apiFetch<{ importedCount: number }>('/Checklists/import', {
      method: 'POST',
      body: JSON.stringify({ items })
    });
  }
};
