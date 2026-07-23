import { Injectable } from '@angular/core';
import { Observable, catchError, forkJoin, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AchReconciliationDashboard, AchReconciliationDetail, AchReconciliationItem } from '../models/nacha-operational.models';

export interface AchReconciliationConsoleData {
  dashboard: AchReconciliationDashboard;
  items: AchReconciliationItem[];
  exceptions: AchReconciliationException[];
}

export interface AchReconciliationException {
  id: string;
  clearingHouseId: number;
  achResponseId?: string | null;
  exceptionType: string;
  status: string;
  reference: string;
  details?: string | null;
  detectedAtUtc: string;
  resolution?: string | null;
  resolutionReason?: string | null;
  version: string;
}

@Injectable({ providedIn: 'root' })
export class AchReconciliationService {
  private readonly basePath = 'api/ach/reconciliation';

  constructor(private readonly api: ApiService) {}

  getDashboard(): Observable<AchReconciliationDashboard> {
    return this.api.get<AchReconciliationDashboard>(`${this.basePath}/dashboard`);
  }

  getItems(): Observable<AchReconciliationItem[]> {
    return this.api.get<AchReconciliationItem[]>(`${this.basePath}/items`);
  }

  getItem(reconciliationId: string): Observable<AchReconciliationDetail> {
    return this.api.get<AchReconciliationDetail>(`${this.basePath}/items/${encodeURIComponent(reconciliationId)}`).pipe(
      catchError((error) => throwError(() => new Error(error?.status === 404 ? 'Item de conciliacion no encontrado.' : 'No fue posible cargar el detalle de conciliacion.')))
    );
  }

  getItemByCorrelation(correlationId: string): Observable<AchReconciliationDetail> {
    return this.api.get<AchReconciliationDetail>(`${this.basePath}/items/by-correlation/${encodeURIComponent(correlationId)}`);
  }

  getExceptions(): Observable<AchReconciliationException[]> {
    return this.api.get<AchReconciliationException[]>(`${this.basePath}/exceptions`);
  }

  resolveException(id: string, expectedVersion: string, resolution: string, reason: string): Observable<AchReconciliationException> {
    return this.api.post<AchReconciliationException>(`${this.basePath}/exceptions/${encodeURIComponent(id)}/resolve`, {
      expectedVersion, resolution, reason, correlationId: crypto.randomUUID()
    });
  }

  getConsoleData(): Observable<AchReconciliationConsoleData> {
    return forkJoin({ dashboard: this.getDashboard(), items: this.getItems(), exceptions: this.getExceptions() });
  }
}
