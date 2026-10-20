import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  CertificateListItem,
  CertificatePreview,
  CertificateValidationResult,
  CertificateVersion,
  DeleteCertificateResult,
  ManagedCertificatePurpose
} from '../models/certificate-management.model';

export interface ManagedCertificateUpload {
  purpose: ManagedCertificatePurpose;
  clearingHouseId?: number | null;
  file: File;
  password?: string;
}

@Injectable({ providedIn: 'root' })
export class CertificateManagementApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/nacha-security/certificates/management';

  list(): Observable<CertificateListItem[]> {
    return this.api.get<CertificateListItem[]>(this.basePath);
  }

  preview(request: ManagedCertificateUpload): Observable<CertificatePreview> {
    return this.api.post<CertificatePreview>(`${this.basePath}/managed/preview`, this.buildForm(request));
  }

  save(request: ManagedCertificateUpload): Observable<CertificateVersion> {
    return this.api.post<CertificateVersion>(`${this.basePath}/managed`, this.buildForm(request));
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

  delete(versionId: number): Observable<DeleteCertificateResult> {
    return this.api.delete<DeleteCertificateResult>(`${this.basePath}/versions/${versionId}`);
  }

  validate(versionId: number): Observable<CertificateValidationResult> {
    return this.api.post<CertificateValidationResult>(`${this.basePath}/versions/${versionId}/validate`, {});
  }

  audit(): Observable<unknown[]> {
    return this.api.get<unknown[]>(`${this.basePath}/audit`);
  }

  private buildForm(request: ManagedCertificateUpload): FormData {
    const form = new FormData();
    form.append('purpose', request.purpose);
    if (request.clearingHouseId) {
      form.append('clearingHouseId', String(request.clearingHouseId));
    }
    if (request.password) {
      form.append('password', request.password);
    }
    form.append('file', request.file, request.file.name);
    return form;
  }
}
