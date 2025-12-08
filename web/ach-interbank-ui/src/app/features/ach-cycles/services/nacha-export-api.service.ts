import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NachaExportApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  getExportableCycles(): Observable<ExportableAchCycle[]> {
    return this.http.get<ExportableAchCycle[]>(`${this.apiBaseUrl}/ach-cycles/exportable`);
  }

  downloadFile(cycleId: number): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.apiBaseUrl}/NachaExport/${cycleId}`, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
