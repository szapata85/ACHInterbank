import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  ClearingHouseCycleConfigFilters,
  ClearingHouseCycleConfigItem,
  InactivateCycleConfigRequest,
  UpsertCycleConfigRequest
} from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class ClearingHouseCycleConfigsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'clearing-house-cycle-configs';

  getByClearingHouse(filters: ClearingHouseCycleConfigFilters) {
    const params: Record<string, string | number> = {
      clearingHouseId: filters.clearingHouseId
    };

    if (filters.effectiveAt) {
      params.effectiveAt = filters.effectiveAt;
    }

    return this.api.get<ClearingHouseCycleConfigItem[]>(this.basePath, { params }).pipe(map((items) => items ?? []));
  }

  getCurrentByClearingHouse(clearingHouseId: number, effectiveAt?: string | null) {
    const params: Record<string, string | number> = { clearingHouseId };

    if (effectiveAt) {
      params.effectiveAt = effectiveAt;
    }

    return this.api
      .get<ClearingHouseCycleConfigItem[]>(`${this.basePath}/current`, { params })
      .pipe(map((items) => items ?? []));
  }

  createVersion(payload: UpsertCycleConfigRequest) {
    return this.api.post<ClearingHouseCycleConfigItem>(this.basePath, payload);
  }

  inactivate(id: number, payload: InactivateCycleConfigRequest) {
    return this.api.post<ClearingHouseCycleConfigItem>(`${this.basePath}/${id}/inactivate`, payload);
  }
}
