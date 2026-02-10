export interface NavigationLogEntry {
  id: string;
  userId?: string | null;
  route: string;
  visitedAt: string;
  sessionId?: string | null;
  durationMs?: number | null;
  ipAddress?: string | null;
  userAgent?: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface NavigationLogFilters {
  startDate?: string | null;
  endDate?: string | null;
  userId?: string | null;
  route?: string | null;
  page: number;
  pageSize: number;
}
