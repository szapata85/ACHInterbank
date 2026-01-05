import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AuditLogEntry, AuditLogFilters, PagedResponse } from '../models/audit-log.model';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/audit-logs';

  search(filters: AuditLogFilters): Observable<PagedResponse<AuditLogEntry>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.startDate) {
      params = params.set('startDate', filters.startDate);
    }

    if (filters.endDate) {
      params = params.set('endDate', filters.endDate);
    }

    if (filters.changedBy) {
      params = params.set('changedBy', filters.changedBy);
    }

    if (filters.action) {
      params = params.set('action', filters.action);
    }

    return this.api.get<PagedResponse<AuditLogEntry>>(this.basePath, { params });
  }
}
