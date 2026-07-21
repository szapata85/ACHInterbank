import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ClearingHouse, ClearingHouseInput, ClearingHousePage, NachaProfileOption, PaymentRailOption } from './clearing-houses.models';

@Injectable({ providedIn: 'root' })
export class ClearingHousesService {
  private readonly api = inject(ApiService);
  private readonly path = 'clearing-houses';

  list(search: string, isActive: boolean | null, page: number): Observable<ClearingHousePage> {
    const params: Record<string, string | number | boolean> = { page, pageSize: 20 };
    if (search.trim()) params['search'] = search.trim();
    if (isActive !== null) params['isActive'] = isActive;
    return this.api.get<ClearingHousePage>(this.path, { params });
  }

  get(id: number): Observable<ClearingHouse> { return this.api.get<ClearingHouse>(`${this.path}/${id}`); }
  create(value: ClearingHouseInput): Observable<ClearingHouse> { return this.api.post<ClearingHouse>(this.path, value); }
  update(id: number, value: ClearingHouseInput): Observable<ClearingHouse> { return this.api.put<ClearingHouse>(`${this.path}/${id}`, value); }
  changeStatus(id: number, isActive: boolean): Observable<ClearingHouse> {
    return this.api.patch<ClearingHouse>(`${this.path}/${id}/status`, { isActive });
  }
  profiles(code: string): Observable<NachaProfileOption[]> {
    return this.api.get<NachaProfileOption[]>(`${this.path}/nacha-profiles`, { params: { clearingHouseCode: code } });
  }
  paymentRailOptions(): Observable<PaymentRailOption[]> {
    return this.api.get<PaymentRailOption[]>(`${this.path}/payment-rail-options`);
  }
}
