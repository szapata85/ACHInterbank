import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';

export interface SobreDigitalCertificate {
  id: number;
  code: string;
  displayName: string;
  fileName: string;
  clearingHouseId: number | null;
  financialInstitutionId: number | null;
  environment: string | number;
  purpose: string | number;
  versionNumber: number;
  hasPrivateKey: boolean;
  thumbprintMasked: string;
  notBefore: string;
  notAfter: string;
  canEncrypt: boolean;
  canDecrypt: boolean;
}

@Injectable({ providedIn: 'root' })
export class SobreDigitalService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/nacha-security/digital-envelope';

  listCertificates(): Observable<SobreDigitalCertificate[]> {
    return this.http.get<SobreDigitalCertificate[]>(this.api.resolveUrl(`${this.basePath}/certificates`));
  }

  encrypt(
    file: File,
    certificateVersionId: number,
    clearingHouseId: number,
    operationMode: 'LIVE'
  ): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);
    form.append('certificateVersionId', certificateVersionId.toString());
    form.append('clearingHouseId', clearingHouseId.toString());
    form.append('operationMode', operationMode);

    return this.http.post(this.api.resolveUrl(`${this.basePath}/encrypt`), form, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  decrypt(
    file: File,
    clearingHouseId: number,
    operationMode: 'LIVE'
  ): Observable<HttpResponse<Blob>> {
    const form = new FormData();
    form.append('file', file);
    form.append('clearingHouseId', clearingHouseId.toString());
    form.append('operationMode', operationMode);

    return this.http.post(this.api.resolveUrl(`${this.basePath}/decrypt`), form, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
