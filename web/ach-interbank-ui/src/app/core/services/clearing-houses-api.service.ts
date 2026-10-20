import { Injectable, inject } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { ClearingHouseOption } from '../models/clearing-house.model';
import { ApiService } from './api.service';

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
