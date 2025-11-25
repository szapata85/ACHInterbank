import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { AliasFilter, AliasSummary, PagedAliasResponse, SaveAliasRequest } from '../models/alias.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AliasesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/aliases';

  search(filter: AliasFilter): Observable<PagedAliasResponse> {
    const params: Record<string, string | number | boolean | undefined> = {
      search: filter.search,
      documentNumber: filter.documentNumber,
      phoneNumber: filter.phoneNumber,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 10
    };
    return this.api.get<PagedAliasResponse>(this.basePath, { params });
  }

  getById(id: string): Observable<AliasSummary> {
    return this.api.get<AliasSummary>(`${this.basePath}/${id}`);
  }

  create(request: SaveAliasRequest): Observable<AliasSummary> {
    return this.api.post<AliasSummary>(this.basePath, request);
  }

  update(id: string, request: SaveAliasRequest): Observable<AliasSummary> {
    return this.api.put<AliasSummary>(`${this.basePath}/${id}`, request);
  }
}
