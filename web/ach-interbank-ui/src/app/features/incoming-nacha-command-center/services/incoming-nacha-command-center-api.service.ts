import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IncomingNachaIngestionDetail,
  IncomingNachaIngestionListItem,
  IncomingNachaManualActionRequest,
  IncomingNachaManualActionResult,
  IncomingNachaPageResult,
  IncomingNachaQueueDetail,
  IncomingNachaQueueListItem
} from '../models/incoming-nacha-command-center.models';

@Injectable({ providedIn: 'root' })
export class IncomingNachaCommandCenterApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'incoming-nacha-command-center';

  getIngestions(params: Record<string, string | number | boolean>): Observable<IncomingNachaPageResult<IncomingNachaIngestionListItem>> {
    return this.api.get<IncomingNachaPageResult<IncomingNachaIngestionListItem>>(`${this.basePath}/ingestions`, { params });
  }

  getIngestionDetail(ingestionId: string): Observable<IncomingNachaIngestionDetail> {
    return this.api.get<IncomingNachaIngestionDetail>(`${this.basePath}/ingestions/${ingestionId}`);
  }

  getQueue(params: Record<string, string | number | boolean>): Observable<IncomingNachaPageResult<IncomingNachaQueueListItem>> {
    return this.api.get<IncomingNachaPageResult<IncomingNachaQueueListItem>>(`${this.basePath}/queue`, { params });
  }

  getQueueDetail(queueId: string): Observable<IncomingNachaQueueDetail> {
    return this.api.get<IncomingNachaQueueDetail>(`${this.basePath}/queue/${queueId}`);
  }

  retry(queueId: string, payload: IncomingNachaManualActionRequest): Observable<IncomingNachaManualActionResult> {
    return this.api.post<IncomingNachaManualActionResult>(`${this.basePath}/queue/${queueId}/retry`, payload);
  }

  unblock(queueId: string, payload: IncomingNachaManualActionRequest): Observable<IncomingNachaManualActionResult> {
    return this.api.post<IncomingNachaManualActionResult>(`${this.basePath}/queue/${queueId}/unblock`, payload);
  }

  requeue(queueId: string, payload: IncomingNachaManualActionRequest): Observable<IncomingNachaManualActionResult> {
    return this.api.post<IncomingNachaManualActionResult>(`${this.basePath}/queue/${queueId}/requeue`, payload);
  }

  markFailedFinal(queueId: string, payload: IncomingNachaManualActionRequest): Observable<IncomingNachaManualActionResult> {
    return this.api.post<IncomingNachaManualActionResult>(`${this.basePath}/queue/${queueId}/mark-failed-final`, payload);
  }
}
