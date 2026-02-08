import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CatalogTypeItem, CatalogTypeUpsertRequest } from '../models/catalog-type.model';

@Injectable({ providedIn: 'root' })
export class CatalogTypesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'catalog-types';

  list(catalogType: string): Observable<CatalogTypeItem[]> {
    return this.api.get<CatalogTypeItem[]>(`${this.basePath}/${catalogType}`);
  }

  create(catalogType: string, request: CatalogTypeUpsertRequest): Observable<CatalogTypeItem> {
    return this.api.post<CatalogTypeItem>(`${this.basePath}/${catalogType}`, request);
  }

  update(catalogType: string, code: string, request: CatalogTypeUpsertRequest): Observable<CatalogTypeItem> {
    return this.api.put<CatalogTypeItem>(`${this.basePath}/${catalogType}/${encodeURIComponent(code)}`, request);
  }

  delete(catalogType: string, code: string): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${catalogType}/${encodeURIComponent(code)}`);
  }
}
