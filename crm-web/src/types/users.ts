export interface UserResponse {
  id: string;
  email: string;
  isActive: boolean;
  isAdmin: boolean;
  groups: string[];
  permissions: string[];
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  groupKeys?: string[];
}

export interface UpdateUserRequest {
  isActive: boolean;
  password?: string;
  groupKeys?: string[];
}
