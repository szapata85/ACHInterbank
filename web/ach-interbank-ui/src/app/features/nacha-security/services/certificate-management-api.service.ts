import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { CertificateListItem, CertificateValidationResult, CertificateVersion } from '../models/certificate-management.model';

@Injectable({ providedIn: 'root' })
export class CertificateManagementApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-security/certificates/management';

  list(): Observable<CertificateListItem[]> {
    return this.api.get<CertificateListItem[]>(this.basePath);
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
}
