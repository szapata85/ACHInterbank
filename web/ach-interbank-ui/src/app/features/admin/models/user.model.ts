export interface RoleSummary {
  id: string;
  name: string;
  description?: string;
  permissions?: string[];
}

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

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}
