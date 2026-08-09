import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  IncomingNachaIngestionDetail,
  IncomingNachaIngestionListItem,
  IncomingNachaManualActionRequest,
  IncomingNachaManualActionResult,
  IncomingNachaOrphan,
  IncomingNachaOrphanCandidate,
  IncomingNachaOrphanResolveRequest,
  IncomingNachaOrphanResolutionResult,
  IncomingNachaObservabilitySummary,
  IncomingNachaPageResult,
  IncomingNachaQueueDetail,
  IncomingNachaQueueListItem
} from '../models/incoming-nacha-command-center.models';

@Injectable({ providedIn: 'root' })
export class IncomingNachaCommandCenterApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'incoming-nacha-command-center';

  getObservabilitySummary(windowHours = 24): Observable<IncomingNachaObservabilitySummary> {
    return this.api.get<IncomingNachaObservabilitySummary>(`${this.basePath}/observability/summary`, {
      params: { windowHours }
    });
  }

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

  getOrphans(params: Record<string, string | number> = {}): Observable<IncomingNachaPageResult<IncomingNachaOrphan>> {
    return this.api.get<IncomingNachaPageResult<IncomingNachaOrphan>>(`${this.basePath}/orphans`, { params });
  }

  getOrphan(linkId: string): Observable<IncomingNachaOrphan> {
    return this.api.get<IncomingNachaOrphan>(`${this.basePath}/orphans/${linkId}`);
  }

  getOrphanCandidates(linkId: string, search = ''): Observable<IncomingNachaOrphanCandidate[]> {
    const params = search.trim() ? { search: search.trim() } : {};
    return this.api.get<IncomingNachaOrphanCandidate[]>(`${this.basePath}/orphans/${linkId}/candidates`, { params });
  }

  resolveOrphan(linkId: string, payload: IncomingNachaOrphanResolveRequest): Observable<IncomingNachaOrphanResolutionResult> {
    return this.api.post<IncomingNachaOrphanResolutionResult>(`${this.basePath}/orphans/${linkId}/resolve`, payload);
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
