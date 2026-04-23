import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  AuthorizeDownloadResponse,
  NachaGenerateOperationRequest,
  NachaSecurityOperationResponse
} from '../models/nacha-security-operation.model';

@Injectable({ providedIn: 'root' })
export class NachaSecurityOperationsApiService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);
  private readonly basePath = 'nacha-security/operations';

  generatePlain(request: NachaGenerateOperationRequest): Observable<NachaSecurityOperationResponse> {
    return this.api.post<NachaSecurityOperationResponse>(`${this.basePath}/nacha/generate`, request);
  }

  generateEncrypted(request: NachaGenerateOperationRequest): Observable<NachaSecurityOperationResponse> {
    return this.api.post<NachaSecurityOperationResponse>(`${this.basePath}/nacha/generate-encrypted`, request);
  }

  manualEncrypt(file: File): Observable<NachaSecurityOperationResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.api.post<NachaSecurityOperationResponse>(`${this.basePath}/envelope/manual-encrypt`, form);
  }

  manualDecrypt(file: File): Observable<NachaSecurityOperationResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.api.post<NachaSecurityOperationResponse>(`${this.basePath}/envelope/manual-decrypt`, form);
  }

  getOperation(operationId: string): Observable<NachaSecurityOperationResponse> {
    return this.api.get<NachaSecurityOperationResponse>(`${this.basePath}/${operationId}`);
  }

  getAudit(take = 100): Observable<NachaSecurityOperationResponse[]> {
    return this.api.get<NachaSecurityOperationResponse[]>(`${this.basePath}/audit`, { params: { take } });
  }

  authorizeDownload(operationId: string): Observable<AuthorizeDownloadResponse> {
    return this.api.post<AuthorizeDownloadResponse>(`${this.basePath}/${operationId}/authorize-download`, {});
  }

  downloadArtifact(operationId: string): Observable<HttpResponse<Blob>> {
    return this.http.get(this.api.resolveUrl(`${this.basePath}/${operationId}/download`), {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
