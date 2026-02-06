import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
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

    return this.api
      .get<PagedResponse<AuthLogEntry> | ApiResponse<PagedResponse<AuthLogEntry>>>(this.basePath, { params })
      .pipe(map((response) => this.unwrapPagedResponse(response)));
  }

  private unwrapPagedResponse(
    response: PagedResponse<AuthLogEntry> | ApiResponse<PagedResponse<AuthLogEntry>>
  ): PagedResponse<AuthLogEntry> {
    const normalized = this.isApiResponse(response) ? response.data : response;

    return {
      items: normalized?.items ?? [],
      total: normalized?.total ?? 0,
      page: normalized?.page ?? 1,
      pageSize: normalized?.pageSize ?? 0
    };
  }

  private isApiResponse(
    response: PagedResponse<AuthLogEntry> | ApiResponse<PagedResponse<AuthLogEntry>>
  ): response is ApiResponse<PagedResponse<AuthLogEntry>> {
    return !!response && typeof response === 'object' && 'statusCode' in response && 'sucess' in response && 'data' in response;
  }
}
