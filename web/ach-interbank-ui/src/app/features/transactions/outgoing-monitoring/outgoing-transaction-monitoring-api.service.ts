import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  OutgoingMonitoringDetail,
  OutgoingMonitoringListItem,
  OutgoingMonitoringPage,
  OutgoingMonitoringQuery,
  OutgoingMonitoringOption
} from './outgoing-transaction-monitoring.models';

@Injectable({ providedIn: 'root' })
export class OutgoingTransactionMonitoringApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/transactions/outgoing-monitoring';

  search(query: OutgoingMonitoringQuery): Observable<OutgoingMonitoringPage<OutgoingMonitoringListItem>> {
    const params: Record<string, string | number | boolean> = {};
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') params[key] = value;
    });
    return this.api.get<OutgoingMonitoringPage<OutgoingMonitoringListItem>>(this.basePath, {
      params,
      headers: { 'X-Skip-Loading': 'true' }
    }).pipe(
      map(page => ({
        ...page,
        items: (page?.items ?? []).map(item => ({ ...item, amount: Number(item.amount) }))
      }))
    );
  }

  getDetail(id: number): Observable<OutgoingMonitoringDetail> {
    return this.api.get<OutgoingMonitoringDetail>(`${this.basePath}/${id}`, {
      headers: { 'X-Skip-Loading': 'true' }
    });
  }

  getClearingHouses(): Observable<OutgoingMonitoringOption[]> {
    return this.api.get<OutgoingMonitoringOption[] | { items?: OutgoingMonitoringOption[] }>('api/clearing-houses/operational', {
      headers: { 'X-Skip-Loading': 'true' }
    }).pipe(map(response => Array.isArray(response) ? response : response?.items ?? []));
  }

  getDestinationInstitutions(): Observable<OutgoingMonitoringOption[]> {
    return this.api.get<OutgoingMonitoringOption[]>('financial-institutions', {
      params: { includeInactive: false },
      headers: { 'X-Skip-Loading': 'true' }
    }).pipe(map(response => response ?? []));
  }
}
