import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { InteroperabilityStatus } from '../models/nacha-security-operation.model';

@Injectable({ providedIn: 'root' })
export class InteroperabilityApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-security/interoperability';

  getStatus(): Observable<InteroperabilityStatus> {
    return this.api.get<InteroperabilityStatus>(`${this.basePath}/status`);
  }

  runHarness(): Observable<unknown> {
    return this.api.post<unknown>(`${this.basePath}/run-harness`, {});
  }

  getReport(reportId: string): Observable<unknown> {
    return this.api.get<unknown>(`${this.basePath}/reports/${reportId}`);
  }
}
