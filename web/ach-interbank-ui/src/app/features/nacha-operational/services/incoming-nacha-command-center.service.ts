import { HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IncomingNachaAddenda,
  IncomingNachaBatch,
  IncomingNachaFileDetail,
  IncomingNachaFileFilters,
  IncomingNachaFileListItem,
  IncomingNachaObservabilitySummary,
  IncomingNachaPage,
  IncomingNachaQueueDetail,
  IncomingNachaTransaction,
  IncomingNachaTransactionFilters,
  IncomingNachaValidation
} from '../models/incoming-nacha-command-center.models';

@Injectable({ providedIn: 'root' })
export class IncomingNachaCommandCenterService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'incoming-nacha-command-center';

  getFiles(filters: IncomingNachaFileFilters): Observable<IncomingNachaPage<IncomingNachaFileListItem>> {
    return this.api.get<IncomingNachaPage<IncomingNachaFileListItem>>(`${this.basePath}/ingestions`, {
      params: this.toParams(filters)
    });
  }

  getSummary(windowHours = 168): Observable<IncomingNachaObservabilitySummary> {
    return this.api.get<IncomingNachaObservabilitySummary>(`${this.basePath}/observability/summary`, {
      params: { windowHours }
    });
  }

  getFile(id: string): Observable<IncomingNachaFileDetail> {
    return this.api.get<IncomingNachaFileDetail>(`${this.basePath}/ingestions/${encodeURIComponent(id)}`);
  }

  getValidations(id: string): Observable<IncomingNachaValidation[]> {
    return this.api.get<IncomingNachaValidation[]>(`${this.basePath}/ingestions/${encodeURIComponent(id)}/validations`);
  }

  getBatches(id: string, page: number, pageSize: number, sortBy: string, sortDescending: boolean, search = ''):
    Observable<IncomingNachaPage<IncomingNachaBatch>> {
    return this.api.get<IncomingNachaPage<IncomingNachaBatch>>(`${this.basePath}/ingestions/${encodeURIComponent(id)}/batches`, {
      params: this.toParams({ page, pageSize, sortBy, sortDescending, search })
    });
  }

  getTransactions(id: string, filters: IncomingNachaTransactionFilters): Observable<IncomingNachaPage<IncomingNachaTransaction>> {
    return this.api.get<IncomingNachaPage<IncomingNachaTransaction>>(`${this.basePath}/ingestions/${encodeURIComponent(id)}/transactions`, {
      params: this.toParams(filters)
    });
  }

  getAddendas(id: string, entryDetailId: number): Observable<IncomingNachaAddenda[]> {
    return this.api.get<IncomingNachaAddenda[]>(
      `${this.basePath}/ingestions/${encodeURIComponent(id)}/transactions/${entryDetailId}/addendas`
    );
  }

  getQueueDetail(queueId: string): Observable<IncomingNachaQueueDetail> {
    return this.api.get<IncomingNachaQueueDetail>(`${this.basePath}/queue/${encodeURIComponent(queueId)}`);
  }

  toParams(values: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
  }
}
