import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface TraceabilityReportFilter {
  fromUtc?: string;
  toUtc?: string;
  state?: 'Pending' | 'ReturnedByOperator' | 'ReturnedByEpr' | 'AppliedTacitly' | 'Certified' | '';
  achCycleId?: string[];
}

export interface TransactionMovementReportFilter {
  date?: string;
  clearingHouseId?: number;
  achCycleId?: string;
  state?: 'Pending' | 'ReturnedByOperator' | 'ReturnedByEpr' | 'AppliedTacitly' | 'Certified';
  reference?: string;
  bankId?: number;
  transactionType?: 'Credit' | 'Debit' | 'Prenotification' | 'Reversal' | 'Return';
  page?: number;
  pageSize?: number;
}

export interface TransactionMovementReportRow {
  transactionId: number;
  effectiveEntryDate: string;
  reference: string;
  amount: number;
  transactionType: string;
  state: string;
  clearingHouseName: string;
  achCycleId: string;
  achCycleName: string;
  batchId: number;
  batchSequenceNumber: number;
  sourceBankName: string;
  destinationBankName: string;
  nachaFileName: string;
}

export interface TransactionMovementReportResponse {
  items: TransactionMovementReportRow[];
  totals: {
    totalRecords: number;
    totalCreditAmount: number;
    totalDebitAmount: number;
  };
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  downloadTraceabilityPdf(filter: TraceabilityReportFilter): Observable<HttpResponse<Blob>> {
    const params: Record<string, string> = {};

    if (filter.fromUtc) {
      params.fromUtc = filter.fromUtc;
    }

    if (filter.toUtc) {
      params.toUtc = filter.toUtc;
    }

    if (filter.state) {
      params.state = filter.state;
    }

    if (filter.achCycleId?.length) {
      params.achCycleId = filter.achCycleId.join(',');
    }

    return this.http.get(this.api.resolveUrl('api/reports/traceability/pdf'), {
      params,
      observe: 'response',
      responseType: 'blob'
    });
  }

  getSentTransactions(filter: TransactionMovementReportFilter): Observable<TransactionMovementReportResponse> {
    return this.http.get<TransactionMovementReportResponse>(
      this.api.resolveUrl('api/reports/transactions/sent'),
      { params: this.buildTransactionMovementParams(filter) }
    );
  }

  getReceivedTransactions(filter: TransactionMovementReportFilter): Observable<TransactionMovementReportResponse> {
    return this.http.get<TransactionMovementReportResponse>(
      this.api.resolveUrl('api/reports/transactions/received'),
      { params: this.buildTransactionMovementParams(filter) }
    );
  }

  downloadSentTransactionsPdf(filter: TransactionMovementReportFilter): Observable<HttpResponse<Blob>> {
    return this.http.get(this.api.resolveUrl('api/reports/transactions/sent/pdf'), {
      params: this.buildTransactionMovementParams(filter),
      observe: 'response',
      responseType: 'blob'
    });
  }

  downloadReceivedTransactionsPdf(filter: TransactionMovementReportFilter): Observable<HttpResponse<Blob>> {
    return this.http.get(this.api.resolveUrl('api/reports/transactions/received/pdf'), {
      params: this.buildTransactionMovementParams(filter),
      observe: 'response',
      responseType: 'blob'
    });
  }

  private buildTransactionMovementParams(filter: TransactionMovementReportFilter): Record<string, string> {
    const params: Record<string, string> = {};

    if (filter.date) params.date = filter.date;
    if (filter.clearingHouseId != null) params.clearingHouseId = String(filter.clearingHouseId);
    if (filter.achCycleId) params.achCycleId = filter.achCycleId;
    if (filter.state) params.state = filter.state;
    if (filter.reference) params.reference = filter.reference;
    if (filter.bankId != null) params.bankId = String(filter.bankId);
    if (filter.transactionType) params.transactionType = filter.transactionType;
    if (filter.page != null) params.page = String(filter.page);
    if (filter.pageSize != null) params.pageSize = String(filter.pageSize);

    return params;
  }
}
