import { UserRole } from './auth';

export interface UserResponse {
  id: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  role: UserRole;
}

export interface UpdateUserRequest {
  role: UserRole;
  isActive: boolean;
  password?: string;
}
