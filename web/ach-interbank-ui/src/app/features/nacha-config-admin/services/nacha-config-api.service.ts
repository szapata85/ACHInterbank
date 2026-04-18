import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  NachaConfigHistoryItem,
  NachaConfigProfileDetail,
  NachaConfigProfileListItem,
  NachaConfigPublicationResult,
  NachaConfigResolverPreviewRequest,
  NachaConfigResolverPreviewResult,
  NachaConfigSnapshotItem,
  NachaConfigValidationResult
} from '../models/nacha-config-admin.models';

@Injectable({ providedIn: 'root' })
export class NachaConfigApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-config';

  listarPerfiles(): Observable<NachaConfigProfileListItem[]> {
    return this.api.get<NachaConfigProfileListItem[]>(`${this.basePath}/perfiles`);
  }

  obtenerPerfil(id: number): Observable<NachaConfigProfileDetail> {
    return this.api.get<NachaConfigProfileDetail>(`${this.basePath}/perfiles/${id}`);
  }

  crearBorrador(payload: Record<string, unknown>): Observable<NachaConfigProfileDetail> {
    return this.api.post<NachaConfigProfileDetail>(`${this.basePath}/perfiles`, payload);
  }

  editarBorrador(id: number, payload: Record<string, unknown>): Observable<NachaConfigProfileDetail> {
    return this.api.put<NachaConfigProfileDetail>(`${this.basePath}/perfiles/${id}`, payload);
  }

  clonarPerfil(id: number, payload: Record<string, unknown>): Observable<NachaConfigProfileDetail> {
    return this.api.post<NachaConfigProfileDetail>(`${this.basePath}/perfiles/${id}/clonar`, payload);
  }

  validar(id: number): Observable<NachaConfigValidationResult> {
    return this.api.post<NachaConfigValidationResult>(`${this.basePath}/perfiles/${id}/validar`, {});
  }

  publicar(id: number, expectedRowVersion: string): Observable<NachaConfigPublicationResult> {
    return this.api.post<NachaConfigPublicationResult>(`${this.basePath}/perfiles/${id}/publicar`, { expectedRowVersion });
  }

  inactivar(id: number, expectedRowVersion: string): Observable<void> {
    return this.api.post<void>(`${this.basePath}/perfiles/${id}/inactivar`, { expectedRowVersion });
  }

  archivar(id: number, expectedRowVersion: string): Observable<void> {
    return this.api.post<void>(`${this.basePath}/perfiles/${id}/archivar`, { expectedRowVersion });
  }

  actualizarSecuencia(id: number, payload: Record<string, unknown>): Observable<void> {
    return this.api.put<void>(`${this.basePath}/perfiles/${id}/records/secuencia`, payload);
  }

  actualizarVariante(id: number, variantId: number, payload: Record<string, unknown>): Observable<void> {
    return this.api.put<void>(`${this.basePath}/perfiles/${id}/variantes/${variantId}`, payload);
  }

  actualizarField(id: number, fieldId: number, payload: Record<string, unknown>): Observable<void> {
    return this.api.put<void>(`${this.basePath}/perfiles/${id}/fields/${fieldId}`, payload);
  }

  actualizarRule(id: number, ruleId: number, payload: Record<string, unknown>): Observable<void> {
    return this.api.put<void>(`${this.basePath}/perfiles/${id}/rules/${ruleId}`, payload);
  }

  historial(id: number): Observable<NachaConfigHistoryItem[]> {
    return this.api.get<NachaConfigHistoryItem[]>(`${this.basePath}/perfiles/${id}/historial`);
  }

  snapshots(id: number): Observable<NachaConfigSnapshotItem[]> {
    return this.api.get<NachaConfigSnapshotItem[]>(`${this.basePath}/perfiles/${id}/snapshots`);
  }

  preview(payload: NachaConfigResolverPreviewRequest): Observable<NachaConfigResolverPreviewResult> {
    return this.api.post<NachaConfigResolverPreviewResult>(`${this.basePath}/resolver-preview`, payload);
  }
}
