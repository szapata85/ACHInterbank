import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CustomerThirdPartyFilters,
  CustomerThirdPartyRow,
  PagedResponse,
  UpdateCustomerThirdPartyStatusRequest
} from '../models/customer-third-party.model';

type CustomerThirdPartyStatusCode = 0 | 1 | 2;
type CustomerThirdPartyApiRow = Omit<CustomerThirdPartyRow, 'status'> & {
  status: CustomerThirdPartyStatusCode | CustomerThirdPartyRow['status'];
};

const statusCodes: Record<CustomerThirdPartyRow['status'], CustomerThirdPartyStatusCode> = {
  Pending: 0,
  Active: 1,
  Rejected: 2
};

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
      params = params.set('status', statusCodes[filters.status]);
    }

    return this.api
      .get<PagedResponse<CustomerThirdPartyApiRow>>(this.basePath, { params })
      .pipe(map(response => ({
        ...response,
        items: (response.items ?? []).map(item => this.toUiRow(item))
      })));
  }

  updateStatus(id: number, request: UpdateCustomerThirdPartyStatusRequest): Observable<CustomerThirdPartyRow> {
    return this.api
      .patch<CustomerThirdPartyApiRow>(`${this.basePath}/${id}/status`, {
        ...request,
        status: statusCodes[request.status]
      })
      .pipe(map(response => this.toUiRow(response)));
  }

  private toUiRow(row: CustomerThirdPartyApiRow): CustomerThirdPartyRow {
    const status = typeof row.status === 'number'
      ? (['Pending', 'Active', 'Rejected'] as const)[row.status]
      : row.status;

    return {
      ...row,
      status
    };
  }
}
