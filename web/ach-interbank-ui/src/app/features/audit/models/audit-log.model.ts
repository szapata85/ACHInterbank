export interface AuditLogEntry {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  changedBy: string;
  changedAt: string;
  changedFields?: string | null;
}

export type { PagedResponse } from '../../../core/models/paged-response.model';

export interface AuditLogFilters {
  startDate?: string | null;
  endDate?: string | null;
  changedBy?: string | null;
  action?: string | null;
  page: number;
  pageSize: number;
}
