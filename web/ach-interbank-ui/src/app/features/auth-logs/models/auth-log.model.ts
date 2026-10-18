export interface AuthLogEntry {
  id: string;
  username: string;
  success: boolean;
  failureReason?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  loggedAt: string;
}

export interface AuthLogFilters {
  startDate?: string | null;
  endDate?: string | null;
  username?: string | null;
  success?: boolean | null;
  page: number;
  pageSize: number;
}

export type { PagedResponse } from '../../../core/models/paged-response.model';
