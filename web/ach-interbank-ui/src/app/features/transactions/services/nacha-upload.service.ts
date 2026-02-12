import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface NachaUploadRecord {
  nachaId?: string | null;
  immediateOrigin?: string | null;
  immediateDestination?: string | null;
  immediateOriginName?: string | null;
  immediateDestinationName?: string | null;
  referenceCode?: string | null;
  fileCreationDate?: string | null;
  fileCreationTime?: string | null;
  achCycleId?: string | null;
  achCycleName?: string | null;
  clearingHouseName?: string | null;
  totalEntries: number;
  totalAddendas: number;
  totalBatches: number;
  totalAmount: number;
  totalDebitAmount: number;
  totalCreditAmount: number;
}

export interface NachaUploadFilters {
  immediateOrigin?: string;
  immediateDestination?: string;
  referenceCode?: string;
  achCycleId?: string;
  fileCreationDate?: string;
}

@Injectable({ providedIn: 'root' })
export class NachaUploadService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  upload(file: File): Observable<unknown> {
    const form = new FormData();
    form.append('file', file);

    return this.http.post(this.api.resolveUrl('NachaUpload/upload'), form);
  }

  listRecords(filters: NachaUploadFilters = {}): Observable<NachaUploadRecord[]> {
    let params = new HttpParams();

    Object.entries(filters).forEach(([key, value]) => {
      if (value != null && `${value}`.trim() !== '') {
        params = params.set(key, `${value}`.trim());
      }
    });

    return this.http.get<NachaUploadRecord[]>(this.api.resolveUrl('NachaUpload/records'), { params });
  }
}
