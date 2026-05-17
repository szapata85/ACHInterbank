import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  AchReturnOfReturnEligibilityResult,
  EvaluateReturnOfReturnRequest,
  GenerateReturnOfReturnAuditFileRequest,
  GenerateReturnOfReturnNachaFileRequest,
  GenerateReturnsFileRequest,
  ReturnEligibleTransaction
} from '../transactions.models';

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

  evaluateReturnOfReturn(request: EvaluateReturnOfReturnRequest): Observable<AchReturnOfReturnEligibilityResult> {
    return this.api.post<AchReturnOfReturnEligibilityResult>('ach-returns/return-of-return/evaluate', request);
  }

  generateReturnOfReturnAuditFile(request: GenerateReturnOfReturnAuditFileRequest): Observable<Blob> {
    return this.api.postBlob('ach-returns/return-of-return/generate-audit-file', request);
  }

  generateReturnOfReturnNachaFile(request: GenerateReturnOfReturnNachaFileRequest): Observable<Blob> {
    return this.api.postBlob('ach-returns/return-of-return/generate-nacha-file', request);
  }
}
