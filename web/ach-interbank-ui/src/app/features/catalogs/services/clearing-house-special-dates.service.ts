import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { ClearingHouseSpecialDate } from '../models/clearing-house-special-date.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ClearingHouseSpecialDatesService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'clearing-house-special-dates';

  list(year?: number): Observable<ClearingHouseSpecialDate[]> {
    const params = year ? { year } : undefined;
    return this.api.get<ClearingHouseSpecialDate[]>(this.basePath, { params });
  }

  create(payload: ClearingHouseSpecialDate): Observable<ClearingHouseSpecialDate> {
    return this.api.post<ClearingHouseSpecialDate>(this.basePath, payload);
  }

  update(payload: ClearingHouseSpecialDate): Observable<ClearingHouseSpecialDate> {
    return this.api.put<ClearingHouseSpecialDate>(`${this.basePath}/${payload.id}`, payload);
  }

  changeStatus(id: number, isActive: boolean): Observable<ClearingHouseSpecialDate> {
    return this.api.patch<ClearingHouseSpecialDate>(`${this.basePath}/${id}/status`, { isActive });
  }
}
