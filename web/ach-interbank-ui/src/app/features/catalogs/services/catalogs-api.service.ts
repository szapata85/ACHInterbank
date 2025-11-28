import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { CatalogItem } from '../models/catalog.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CatalogsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'financial-institutions';

  listBanks(): Observable<CatalogItem[]> {
    return this.api.get<CatalogItem[]>(this.basePath);
  }
}
