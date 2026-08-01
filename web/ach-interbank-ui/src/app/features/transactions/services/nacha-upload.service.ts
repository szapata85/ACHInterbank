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

export interface NachaUploadOptions {
  forceReprocess?: boolean;
  parentIngestionId?: string;
}

@Injectable({ providedIn: 'root' })
export class NachaUploadService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  upload(file: File, clearingHouseId: number, options: NachaUploadOptions = {}): Observable<unknown> {
    const form = buildNachaUploadFormData(file, clearingHouseId, options);
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

export function buildNachaUploadFormData(
  file: File,
  clearingHouseId: number,
  options: NachaUploadOptions = {}
): FormData {
  const form = new FormData();
  form.append('file', file);
  form.append('clearingHouseId', clearingHouseId.toString());
  if (options.forceReprocess) {
    form.append('forceReprocess', 'true');
    if (options.parentIngestionId) {
      form.append('parentIngestionId', options.parentIngestionId);
    }
  }
  return form;
}
