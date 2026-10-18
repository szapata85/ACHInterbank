export interface NavigationLogEntry {
  id: string;
  userId?: string | null;
  username?: string | null;
  route: string;
  visitedAt: string;
  sessionId?: string | null;
  durationMs?: number | null;
  ipAddress?: string | null;
  userAgent?: string | null;
}

export type { PagedResponse } from '../../../core/models/paged-response.model';

export interface NavigationLogFilters {
  startDate?: string | null;
  endDate?: string | null;
  userId?: string | null;
  username?: string | null;
  route?: string | null;
  page: number;
  pageSize: number;
}
