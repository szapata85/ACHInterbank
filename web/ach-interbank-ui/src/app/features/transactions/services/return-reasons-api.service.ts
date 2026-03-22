import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ReturnReason } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class ReturnReasonsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'return-reasons';

  getAll(onlyForReturn = false): Observable<ReturnReason[]> {
    const params = onlyForReturn ? { onlyForReturn: true } : undefined;
    return this.api.get<ReturnReason[]>(this.basePath, { params });
  }

  getForReturns(): Observable<ReturnReason[]> {
    return this.getAll(true);
  }
}
