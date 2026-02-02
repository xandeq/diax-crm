export enum ChecklistItemStatus {
  ToBuy = 0,
  Bought = 1,
  Canceled = 2,
  Archived = 3
}

export enum ChecklistPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Urgent = 3
}

export interface ChecklistCategory {
  id: string;
  name: string;
  description?: string;
  color?: string;
  icon?: string;
  displayOrder: number;
}

export interface ChecklistItem {
  id: string;
  categoryId: string;
  categoryName: string;
  title: string;
  description?: string;
  priority: ChecklistPriority;
  targetDate?: string;
  estimatedPrice?: number;
  actualPrice?: number;
  quantity: number;
  storeOrLink?: string;
  status: ChecklistItemStatus;
  isArchived: boolean;
  boughtAt?: string;
  canceledAt?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ChecklistItemsQuery {
  categoryId?: string;
  status?: ChecklistItemStatus;
  q?: string;
  dateFrom?: string;
  dateTo?: string;
  includeArchived?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export interface CreateChecklistItemRequest {
  categoryId: string;
  title: string;
  description?: string;
  priority: ChecklistPriority;
  targetDate?: string;
  estimatedPrice?: number;
  quantity: number;
  storeOrLink?: string;
}

export interface UpdateChecklistItemRequest {
  categoryId: string;
  title: string;
  description?: string;
  priority: ChecklistPriority;
  status: ChecklistItemStatus;
  targetDate?: string;
  estimatedPrice?: number;
  actualPrice?: number;
  quantity: number;
  storeOrLink?: string;
}

export interface CreateChecklistCategoryRequest {
  name: string;
  description?: string;
  color?: string;
  icon?: string;
  displayOrder: number;
}

export interface UpdateChecklistCategoryRequest {
  name: string;
  description?: string;
  color?: string;
  icon?: string;
  displayOrder: number;
}

export interface ChecklistItemBulkRequest {
  ids: string[];
  action: 'markbought' | 'markcanceled' | 'archive' | 'unarchive' | 'reactivate' | 'delete' | 'changecategory';
  actualPrice?: number;
  targetCategoryId?: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
