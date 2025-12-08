import { HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ExportableAchCycle } from '../models/ach-cycle-export.model';

@Injectable({ providedIn: 'root' })
export class NachaExportApiService {
  private readonly api = inject(ApiService);

  getExportableCycles(): Observable<ExportableAchCycle[]> {
    return this.api.get<ExportableAchCycle[]>('ach-cycles/exportable');
  }

  downloadFile(cycleId: number): Observable<HttpResponse<Blob>> {
    return this.api.get<HttpResponse<Blob>>(`NachaExport/${cycleId}`, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
