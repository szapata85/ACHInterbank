import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { ExportableAchCycle, ExportableAchCycleFilter } from '../models/ach-cycle-export.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class NachaExportApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  getExportableCycles(filter?: ExportableAchCycleFilter): Observable<ExportableAchCycle[]> {
    const params: Record<string, string | number> = {};

    if (filter?.clearingHouseId !== undefined && filter.clearingHouseId !== null) {
      params.clearingHouseId = filter.clearingHouseId;
    }

    if (filter?.startDate) {
      params.startDate = filter.startDate;
    }

    if (filter?.endDate) {
      params.endDate = filter.endDate;
    }

    return this.http
      .get<ExportableAchCycle[] | { items?: ExportableAchCycle[] }>(`${this.apiBaseUrl}/ach-cycles/exportable`, { params })
      .pipe(map((response) => (Array.isArray(response) ? response : response?.items ?? [])));
  }

  downloadFile(cycleId: string, encrypted = false): Observable<HttpResponse<Blob>> {
    const url = encrypted
      ? `${this.apiBaseUrl}/NachaExport/${cycleId}/sobre-digital?forceEncryption=true`
      : `${this.apiBaseUrl}/NachaExport/${cycleId}`;

    return this.http.get(url, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
