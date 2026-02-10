import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { ApiService } from '../../../core/services/api.service';
import { NavigationLogEntry, NavigationLogFilters, PagedResponse } from '../models/navigation-log.model';

@Injectable({ providedIn: 'root' })
export class NavigationLogListService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/navigation-logs';

  search(filters: NavigationLogFilters): Observable<PagedResponse<NavigationLogEntry>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.startDate) {
      params = params.set('startDate', filters.startDate);
    }

    if (filters.endDate) {
      params = params.set('endDate', filters.endDate);
    }

    if (filters.userId) {
      params = params.set('userId', filters.userId);
    }

    if (filters.route) {
      params = params.set('route', filters.route);
    }

    return this.api
      .get<PagedResponse<NavigationLogEntry> | ApiResponse<PagedResponse<NavigationLogEntry>>>(this.basePath, { params })
      .pipe(map((response) => this.unwrapPagedResponse(response)));
  }

  private unwrapPagedResponse(
    response: PagedResponse<NavigationLogEntry> | ApiResponse<PagedResponse<NavigationLogEntry>>
  ): PagedResponse<NavigationLogEntry> {
    const normalized = this.isApiResponse(response) ? response.data : response;

    return {
      items: normalized?.items ?? [],
      total: normalized?.total ?? 0,
      page: normalized?.page ?? 1,
      pageSize: normalized?.pageSize ?? 0
    };
  }

  private isApiResponse(
    response: PagedResponse<NavigationLogEntry> | ApiResponse<PagedResponse<NavigationLogEntry>>
  ): response is ApiResponse<PagedResponse<NavigationLogEntry>> {
    return !!response && typeof response === 'object' && 'statusCode' in response && 'sucess' in response && 'data' in response;
  }
}
