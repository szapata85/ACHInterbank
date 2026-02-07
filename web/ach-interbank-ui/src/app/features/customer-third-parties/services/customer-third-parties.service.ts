import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CustomerThirdPartyFilters,
  CustomerThirdPartyRow,
  PagedResponse,
  UpdateCustomerThirdPartyStatusRequest
} from '../models/customer-third-party.model';

@Injectable({ providedIn: 'root' })
export class CustomerThirdPartiesService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/customer-third-parties';

  search(filters: CustomerThirdPartyFilters): Observable<PagedResponse<CustomerThirdPartyRow>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.search) {
      params = params.set('search', filters.search);
    }

    if (filters.destinationAccountNumber) {
      params = params.set('destinationAccountNumber', filters.destinationAccountNumber);
    }

    if (filters.recipientIdNumber) {
      params = params.set('recipientIdNumber', filters.recipientIdNumber);
    }

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    return this.api.get<PagedResponse<CustomerThirdPartyRow>>(this.basePath, { params });
  }

  updateStatus(id: number, request: UpdateCustomerThirdPartyStatusRequest): Observable<CustomerThirdPartyRow> {
    return this.api.patch<CustomerThirdPartyRow>(`${this.basePath}/${id}/status`, request);
  }
}
