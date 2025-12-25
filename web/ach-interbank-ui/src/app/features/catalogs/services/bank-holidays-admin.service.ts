import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { BankHoliday } from '../models/bank-holiday.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BankHolidaysAdminService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'bank-holidays';

  list(year?: number): Observable<BankHoliday[]> {
    const params = year ? { year } : undefined;
    return this.api.get<BankHoliday[]>(this.basePath, { params });
  }

  create(payload: BankHoliday): Observable<BankHoliday> {
    return this.api.post<BankHoliday>(this.basePath, payload);
  }

  update(payload: BankHoliday): Observable<BankHoliday> {
    return this.api.put<BankHoliday>(`${this.basePath}/${payload.id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}
