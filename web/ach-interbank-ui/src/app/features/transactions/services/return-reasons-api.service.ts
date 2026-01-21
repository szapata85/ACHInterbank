import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ReturnReason } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class ReturnReasonsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'return-reasons';

  getAll(): Observable<ReturnReason[]> {
    return this.api.get<ReturnReason[]>(this.basePath);
  }
}
