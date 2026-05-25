import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ExportableAchCycle, ExportableAchCycleFilter } from '../models/ach-cycle-export.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from '../../../core/services/api.service';

@Injectable({ providedIn: 'root' })
export class NachaExportApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

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
      .get<ExportableAchCycle[] | { items?: ExportableAchCycle[] }>(this.api.resolveUrl('ach-cycles/exportable'), { params })
      .pipe(map((response) => (Array.isArray(response) ? response : response?.items ?? [])));
  }

  downloadFile(cycleId: string, encrypted = false): Observable<HttpResponse<Blob>> {
    const encodedCycleId = encodeURIComponent(cycleId);
    const url = encrypted
      ? this.api.resolveUrl(`NachaExport/${encodedCycleId}/sobre-digital?forceEncryption=true`)
      : this.api.resolveUrl(`NachaExport/${encodedCycleId}`);

    return this.http.get(url, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
