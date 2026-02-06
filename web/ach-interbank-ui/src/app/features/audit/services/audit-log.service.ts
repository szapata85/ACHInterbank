import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
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

    return this.api
      .get<PagedResponse<AuditLogEntry> | ApiResponse<PagedResponse<AuditLogEntry>>>(this.basePath, { params })
      .pipe(map((response) => this.unwrapPagedResponse(response)));
  }

  private unwrapPagedResponse(
    response: PagedResponse<AuditLogEntry> | ApiResponse<PagedResponse<AuditLogEntry>>
  ): PagedResponse<AuditLogEntry> {
    const normalized = this.isApiResponse(response) ? response.data : response;

    return {
      items: normalized?.items ?? [],
      total: normalized?.total ?? 0,
      page: normalized?.page ?? 1,
      pageSize: normalized?.pageSize ?? 0
    };
  }

  private isApiResponse(
    response: PagedResponse<AuditLogEntry> | ApiResponse<PagedResponse<AuditLogEntry>>
  ): response is ApiResponse<PagedResponse<AuditLogEntry>> {
    return !!response && typeof response === 'object' && 'statusCode' in response && 'sucess' in response && 'data' in response;
  }
}
