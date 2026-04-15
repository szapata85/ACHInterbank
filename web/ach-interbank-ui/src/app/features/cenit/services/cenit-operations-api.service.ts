import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';
import {
  CycleReportFilter,
  CycleReportRow,
  ReportsApiService,
  ReturnRejectionReportFilter,
  ReturnRejectionReportRow
} from '../../reports/services/reports-api.service';
import {
  CenitQueueRow,
  CenitNetPositionRow,
  CenitOptimizationDecisionRow,
  CenitSimplePage,
  CenitTraceabilityRow
} from '../models/cenit.models';

@Injectable({ providedIn: 'root' })
export class CenitOperationsApiService {
  private readonly reportsApi = inject(ReportsApiService);
  private readonly api = inject(ApiService);

  getCycles(filter: CycleReportFilter): Observable<CycleReportRow[]> {
    return this.reportsApi.getCyclesReport(filter).pipe(map((response) => response.items ?? []));
  }

  getQueueTransactions(status = '', page = 1, pageSize = 50): Observable<CenitSimplePage<CenitQueueRow>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) {
      params.status = status;
    }

    return this.api
      .get<CenitSimplePage<CenitQueueRow>>('api/cenit/queues', { params })
      .pipe(catchError(() => of({ items: [] })));
  }

  getDeferredTransactions(page = 1, pageSize = 50): Observable<CenitSimplePage<CenitQueueRow>> {
    return this.getQueueTransactions('Queued', page, pageSize);
  }

  getOptimizationDecisions(decisionType = ''): Observable<CenitSimplePage<CenitOptimizationDecisionRow>> {
    const params: Record<string, string> = {};
    if (decisionType) {
      params.decisionType = decisionType;
    }

    return this.api
      .get<CenitSimplePage<CenitOptimizationDecisionRow>>('api/cenit/optimization-decisions', { params })
      .pipe(catchError(() => of({ items: [] })));
  }

  getNetPositions(): Observable<CenitSimplePage<CenitNetPositionRow>> {
    return this.api.get<CenitSimplePage<CenitNetPositionRow>>('api/cenit/net-positions').pipe(catchError(() => of({ items: [] })));
  }

  getTraceability(filter: ReturnRejectionReportFilter): Observable<CenitTraceabilityRow[]> {
    return this.reportsApi.getReturns(filter).pipe(
      map((response) => (response.items ?? []).map((row) => this.toTraceabilityRow(row)))
    );
  }

  private toTraceabilityRow(row: ReturnRejectionReportRow): CenitTraceabilityRow {
    return {
      transactionId: row.transactionId,
      effectiveEntryDate: row.effectiveEntryDate,
      transactionExternalId: row.transactionExternalId,
      reference: row.reference,
      amount: row.amount,
      state: row.state,
      causalCode: row.causalCode,
      causalDescription: row.causalDescription,
      clearingHouseName: row.clearingHouseName,
      achCycleId: row.achCycleId,
      achCycleName: row.achCycleName,
      originalTraceRef: row.originalTraceRef
    };
  }
}
