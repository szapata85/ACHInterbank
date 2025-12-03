import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import {
  AchCycleFilter,
  AchCycleSummary,
  ClearingHouseOption,
  PagedAchCycleResponse,
  SaveAchCycleRequest
} from '../models/ach-cycle.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AchCyclesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = '/ach-cycles';

  search(filter: AchCycleFilter): Observable<PagedAchCycleResponse> {
    const params: Record<string, string | number | boolean> = {
      date: filter.date,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 10
    };

    if (filter.clearingHouseId !== undefined && filter.clearingHouseId !== null) {
      params.clearingHouseId = filter.clearingHouseId;
    }
    return this.api.get<PagedAchCycleResponse>(this.basePath, { params });
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
}

@Injectable({ providedIn: 'root' })
export class ClearingHousesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'clearing-houses';

  list(): Observable<ClearingHouseOption[]> {
    return this.api.get<ClearingHouseOption[]>(this.basePath);
  }
}
