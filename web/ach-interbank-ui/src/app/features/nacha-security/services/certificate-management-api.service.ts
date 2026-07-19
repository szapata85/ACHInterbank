import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CertificateListItem, CertificateValidationResult, CertificateVersion } from '../models/certificate-management.model';

export interface CertificateUploadContext {
  code: string;
  displayName: string;
  clearingHouseId: number;
  environment: 'Test' | 'Production';
  purpose: 'OutboundEncryption' | 'InboundDecryption' | 'OutboundSigning' | 'InboundSignatureValidation';
  holderType: 'Participant' | 'ClearingHouse' | 'ThirdPartyProvider';
}

@Injectable({ providedIn: 'root' })
export class CertificateManagementApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/nacha-security/certificates/management';

  list(filters?: { clearingHouseId?: number; environment?: string; purpose?: string; status?: string }): Observable<CertificateListItem[]> {
    const params = Object.fromEntries(
      Object.entries(filters ?? {}).filter(([, value]) => value !== undefined && value !== null && value !== '')
    ) as Record<string, string | number>;
    return this.api.get<CertificateListItem[]>(this.basePath, { params });
  }

  getVersions(id: number): Observable<CertificateVersion[]> {
    return this.api.get<CertificateVersion[]>(`${this.basePath}/${id}/versions`);
  }

  activate(versionId: number): Observable<CertificateVersion> {
    return this.api.post<CertificateVersion>(`${this.basePath}/versions/${versionId}/activate`, {});
  }

  revoke(versionId: number, reason: string): Observable<CertificateVersion> {
    return this.api.post<CertificateVersion>(`${this.basePath}/versions/${versionId}/revoke`, { reason });
  }

  validate(versionId: number): Observable<CertificateValidationResult> {
    return this.api.post<CertificateValidationResult>(`${this.basePath}/versions/${versionId}/validate`, {});
  }

  audit(): Observable<unknown[]> {
    return this.api.get<unknown[]>(`${this.basePath}/audit`);
  }

  uploadPublic(context: CertificateUploadContext, file: File): Observable<CertificateVersion> {
    const form = this.buildForm(context, file);
    return this.api.post<CertificateVersion>(`${this.basePath}/public`, form);
  }

  uploadPrivate(context: CertificateUploadContext, file: File, password: string): Observable<CertificateVersion> {
    const form = this.buildForm(context, file);
    form.append('password', password);
    form.append('storageMode', 'DatabaseEncrypted');
    return this.api.post<CertificateVersion>(`${this.basePath}/private`, form);
  }

  private buildForm(context: CertificateUploadContext, file: File): FormData {
    const form = new FormData();
    form.append('code', context.code);
    form.append('displayName', context.displayName);
    form.append('clearingHouseId', String(context.clearingHouseId));
    form.append('environment', context.environment);
    form.append('purpose', context.purpose);
    form.append('holderType', context.holderType);
    form.append('file', file, file.name);
    return form;
  }
}
