import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class NachaExportApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  getExportableCycles(): Observable<ExportableAchCycle[]> {
    return this.http
      .get<ExportableAchCycle[] | { items?: ExportableAchCycle[] }>(`${this.apiBaseUrl}/ach-cycles/exportable`)
      .pipe(map((response) => (Array.isArray(response) ? response : response?.items ?? [])));
  }

  downloadFile(cycleId: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.apiBaseUrl}/NachaExport/${cycleId}`, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
