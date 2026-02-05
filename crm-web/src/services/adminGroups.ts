import { apiFetch, apiRequest } from './api';
import { AiProvider, AiModel } from './aiCatalog';

export interface UserGroup {
  id: string;
  name: string;
  description?: string;
  memberCount?: number;
}

export interface GroupAiAccessRequest {
  allowedProviderIds: string[];
  allowedModelIds: string[];
}

export const adminGroupsService = {
  getAll: async (): Promise<UserGroup[]> => {
    // This endpoint might not exist yet in API? I need to check UserGroupController
    return await apiFetch<UserGroup[]>('/admin/groups');
  },

  create: async (data: { name: string, description?: string }): Promise<UserGroup> => {
    return await apiRequest<UserGroup>('/admin/groups', 'POST', data);
  },

  update: async (id: string, data: { name: string, description?: string }): Promise<void> => {
    await apiRequest(`/admin/groups/${id}`, 'PUT', data);
  },

  delete: async (id: string): Promise<void> => {
    await apiRequest(`/admin/groups/${id}`, 'DELETE');
  },
  
  // AI Access
  getAiAccess: async (groupId: string): Promise<GroupAiAccessDto> => {
    return await apiFetch<GroupAiAccessDto>(`/admin/groups/${groupId}/ai-access`);
  },

  updateAiAccess: async (groupId: string, data: GroupAiAccessRequest): Promise<void> => {
    await apiRequest(`/admin/groups/${groupId}/ai-access`, 'POST', data);
  }
};

export interface GroupAiAccessDto {
    groupId: string;
    allowedProviderIds: string[];
    allowedModelIds: string[];
}

