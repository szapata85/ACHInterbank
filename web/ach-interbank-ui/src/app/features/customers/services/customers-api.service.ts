import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CustomerDetail, CustomerSummary, SaveCustomerRequest } from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomersApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'customers';

  getAll(): Observable<CustomerSummary[]> {
    return this.api.get<CustomerSummary[]>(this.basePath);
  }

  getById(id: string | number): Observable<CustomerDetail> {
    return this.api.get<CustomerDetail>(`${this.basePath}/${id}`);
  }

  create(request: SaveCustomerRequest): Observable<CustomerDetail> {
    return this.api.post<CustomerDetail>(this.basePath, request);
  }

  update(id: string | number, request: SaveCustomerRequest): Observable<CustomerDetail> {
    return this.api.put<CustomerDetail>(`${this.basePath}/${id}`, request);
  }

  delete(id: string | number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}
