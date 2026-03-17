import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { GenerateReturnsFileRequest, ReturnEligibleTransaction } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class AchReturnsApiService {
  private readonly api = inject(ApiService);

  getTransactionsByCycle(cycleId: string): Observable<ReturnEligibleTransaction[]> {
    return this.api
      .get<ReturnEligibleTransaction[]>(`ach-returns/cycles/${encodeURIComponent(cycleId)}/transactions`)
      .pipe(map((items) => (items ?? []).map((item) => ({ ...item, amount: Number(item.amount) }))));
  }

  generateFile(request: GenerateReturnsFileRequest): Observable<Blob> {
    return this.api.postBlob('ach-returns/generate-file', request);
  }
}
