import { RoleSummary } from '../../../core/models/role.model';

export interface UserSummary {
  id: string;
  userName: string;
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  roles: RoleSummary[];
  isActive: boolean;
}

export interface UserFilter {
  search?: string;
  roleId?: string;
  page?: number;
  pageSize?: number;
}

export interface SaveUserRequest {
  id?: string;
  userName: string;
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  password?: string;
  roleIds?: string[];
}

export type { PagedResponse } from '../../../core/models/paged-response.model';
