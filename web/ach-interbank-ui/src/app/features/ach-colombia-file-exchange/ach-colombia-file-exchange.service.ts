import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { ExecutionOrigin, TransferDetail, TransferDirection, TransferStatus, TransferSummary } from './ach-colombia-file-exchange.models';

@Injectable({ providedIn: 'root' })
export class AchColombiaFileExchangeService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);
  private readonly base = 'api/ach-colombia/file-exchange';

  list(filter: { from?: string; to?: string; direction?: TransferDirection | ''; status?: TransferStatus | ''; executionOrigin?: ExecutionOrigin | ''; cycleId?: string }): Observable<TransferSummary[]> {
    let params = new HttpParams();
    Object.entries(filter).forEach(([key, value]) => { if (value) params = params.set(key, value); });
    return this.api.get<TransferSummary[]>(`${this.base}/transfers`, { params });
  }
  detail(id: string) { return this.api.get<TransferDetail>(`${this.base}/transfers/${id}`); }
  executeOutbound(cycleId: string) { return this.api.post(`${this.base}/outbound/execute`, { cycleId }); }
  executeInbound() { return this.api.post(`${this.base}/inbound/execute`, {}); }
  retry(id: string) { return this.api.post<TransferDetail>(`${this.base}/transfers/${id}/retry`, {}); }
  reprocess(id: string) { return this.api.post<TransferDetail>(`${this.base}/transfers/${id}/reprocess`, {}); }
  archive(id: string) { return this.api.post<TransferDetail>(`${this.base}/transfers/${id}/archive`, {}); }
  retire(id: string, reason: string) { return this.api.post<TransferDetail>(`${this.base}/transfers/${id}/retire`, { reason }); }
  download(id: string) { return this.http.get(this.api.resolveUrl(`${this.base}/transfers/${id}/download`), { responseType: 'blob', observe: 'response' }); }
}
