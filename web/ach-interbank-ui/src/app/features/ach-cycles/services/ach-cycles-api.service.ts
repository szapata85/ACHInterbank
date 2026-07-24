import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import {
  AchCycleFilter,
  AchCycleConfigurationOption,
  AchCycleSummary,
  ClearingHouseOption,
  PagedAchCycleResponse,
  SaveAchCycleRequest
} from '../models/ach-cycle.model';
import { Observable, map, shareReplay } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AchCyclesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/ach-cycles';

  search(filter: AchCycleFilter): Observable<PagedAchCycleResponse> {
    const params: Record<string, string | number | boolean> = {
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 10
    };

    if (filter.startDate) {
      params.startDate = filter.startDate;
    }

    if (filter.endDate) {
      params.endDate = filter.endDate;
    }

    if (filter.clearingHouseId !== undefined && filter.clearingHouseId !== null) {
      params.clearingHouseId = filter.clearingHouseId;
    }

    return this.api.get<PagedAchCycleResponse | AchCycleSummary[]>(this.basePath, { params }).pipe(
      map((response) => {
        if (Array.isArray(response)) {
          const page = filter.page ?? 1;
          const pageSize = filter.pageSize ?? 10;
          const start = Math.max(0, (page - 1) * pageSize);

          return {
            items: response.slice(start, start + pageSize),
            total: response.length,
            page,
            pageSize
          } satisfies PagedAchCycleResponse;
        }

        return {
          items: response?.items ?? [],
          total: response?.total ?? 0,
          page: response?.page ?? filter.page ?? 1,
          pageSize: response?.pageSize ?? filter.pageSize ?? 10
        } satisfies PagedAchCycleResponse;
      })
    );
  }

  getById(id: string): Observable<AchCycleSummary> {
    return this.api.get<AchCycleSummary>(`${this.basePath}/${id}`);
  }

  create(request: SaveAchCycleRequest): Observable<AchCycleSummary> {
    return this.api.post<AchCycleSummary>(this.basePath, request);
  }

  update(id: string, request: SaveAchCycleRequest): Observable<AchCycleSummary> {
    return this.api.put<AchCycleSummary>(`${this.basePath}/${id}`, request);
  }

  getCurrentConfigurations(
    clearingHouseId: number,
    effectiveAt: string
  ): Observable<AchCycleConfigurationOption[]> {
    return this.api.get<AchCycleConfigurationOption[]>('clearing-house-cycle-configs/current', {
      params: { clearingHouseId, effectiveAt }
    });
  }
}

@Injectable({ providedIn: 'root' })
export class ClearingHousesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/clearing-houses';
  private cachedClearingHouses$?: Observable<ClearingHouseOption[]>;

  list(): Observable<ClearingHouseOption[]> {
    if (!this.cachedClearingHouses$) {
      this.cachedClearingHouses$ = this.api
        .get<ClearingHouseOption[] | { items?: ClearingHouseOption[] }>(`${this.basePath}/operational`)
        .pipe(
          map((response) => (Array.isArray(response) ? response : response?.items ?? [])),
          shareReplay(1)
        );
    }

    return this.cachedClearingHouses$;
  }

  listAdministrative(): Observable<ClearingHouseOption[]> {
    return this.api
      .get<ClearingHouseOption[] | { items?: ClearingHouseOption[] }>(this.basePath)
      .pipe(map((response) => (Array.isArray(response) ? response : response?.items ?? [])));
  }
}
