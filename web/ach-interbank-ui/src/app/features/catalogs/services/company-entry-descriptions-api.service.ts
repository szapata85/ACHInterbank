import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CompanyEntryDescriptionItem, CompanyEntryDescriptionUpsertRequest } from '../models/company-entry-description.model';

@Injectable({ providedIn: 'root' })
export class CompanyEntryDescriptionsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'company-entry-descriptions';

  list(): Observable<CompanyEntryDescriptionItem[]> {
    return this.api.get<CompanyEntryDescriptionItem[]>(this.basePath);
  }

  create(request: CompanyEntryDescriptionUpsertRequest): Observable<CompanyEntryDescriptionItem> {
    return this.api.post<CompanyEntryDescriptionItem>(this.basePath, request);
  }

  update(id: number, request: CompanyEntryDescriptionUpsertRequest): Observable<CompanyEntryDescriptionItem> {
    return this.api.put<CompanyEntryDescriptionItem>(`${this.basePath}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}
