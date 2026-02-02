import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AuthLogEntry, AuthLogFilters, PagedResponse } from '../models/auth-log.model';

@Injectable({ providedIn: 'root' })
export class AuthLogService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/auth-logs';

  search(filters: AuthLogFilters): Observable<PagedResponse<AuthLogEntry>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.startDate) {
      params = params.set('startDate', filters.startDate);
    }

    if (filters.endDate) {
      params = params.set('endDate', filters.endDate);
    }

    if (filters.username) {
      params = params.set('username', filters.username);
    }

    if (filters.success !== null && filters.success !== undefined) {
      params = params.set('success', filters.success);
    }

    return this.api.get<PagedResponse<AuthLogEntry>>(this.basePath, { params });
  }
}
