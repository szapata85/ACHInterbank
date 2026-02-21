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
}

