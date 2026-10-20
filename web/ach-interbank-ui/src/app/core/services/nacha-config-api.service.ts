import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  NachaConfigChangeReview,
  NachaConfigHistoryItem,
  NachaConfigOperationalTestResult,
  NachaConfigFilterCatalogs,
  NachaConfigFieldEditor,
  NachaConfigFieldEditorUpdateRequest,
  NachaConfigProfileDetail,
  NachaConfigProfileDetailReadModel,
  NachaConfigProfileFieldReadModel,
  NachaConfigProfileListItem,
  NachaConfigProfileReadModel,
  NachaConfigProfilesDashboardReadModel,
  NachaConfigProfileVariantReadModel,
  NachaConfigWorkspace,
  NachaConfigPublicationResult,
  NachaConfigResolverPreviewRequest,
  NachaConfigResolverPreviewResult,
  NachaConfigSnapshotItem,
  NachaConfigValidationResult
} from '../models/nacha-config.model';
import { NachaConfigReadRetryEvent, retryNachaConfigRead } from './nacha-config-read-retry';

@Injectable({ providedIn: 'root' })
export class NachaConfigApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-config';
  private readonly readOnlyBasePath = 'api/ach/nacha/config-profiles';

  dashboardReadOnly(): Observable<NachaConfigProfilesDashboardReadModel> {
    return this.api
      .get<NachaConfigProfilesDashboardReadModel>(`${this.readOnlyBasePath}/dashboard`)
      .pipe(retryNachaConfigRead());
  }

  listarPerfilesReadOnly(): Observable<NachaConfigProfileReadModel[]> {
    return this.api.get<NachaConfigProfileReadModel[]>(this.readOnlyBasePath).pipe(retryNachaConfigRead());
  }

  obtenerPerfilReadOnly(id: number): Observable<NachaConfigProfileDetailReadModel> {
    return this.api.get<NachaConfigProfileDetailReadModel>(`${this.readOnlyBasePath}/${id}`);
  }

  obtenerPerfilPorCodigoReadOnly(profileCode: string): Observable<NachaConfigProfileDetailReadModel> {
    return this.api.get<NachaConfigProfileDetailReadModel>(`${this.readOnlyBasePath}/by-code/${profileCode}`);
  }

  variantesReadOnly(id: number): Observable<NachaConfigProfileVariantReadModel[]> {
    return this.api.get<NachaConfigProfileVariantReadModel[]>(`${this.readOnlyBasePath}/${id}/variants`);
  }

  fieldsReadOnly(id: number): Observable<NachaConfigProfileFieldReadModel[]> {
    return this.api.get<NachaConfigProfileFieldReadModel[]>(`${this.readOnlyBasePath}/${id}/fields`);
  }

  listarPerfiles(): Observable<NachaConfigProfileListItem[]> {
    return this.api.get<NachaConfigProfileListItem[]>(`${this.basePath}/perfiles`);
  }

  catalogosFiltro(onRetry?: (event: NachaConfigReadRetryEvent) => void): Observable<NachaConfigFilterCatalogs> {
    return this.api
      .get<NachaConfigFilterCatalogs>(`${this.basePath}/catalogos-filtro`)
      .pipe(retryNachaConfigRead(onRetry));
  }

  catalogosOperativos(
    camaraCode?: string | null,
    flujoCode?: string | null,
    onRetry?: (event: NachaConfigReadRetryEvent) => void
  ): Observable<NachaConfigFilterCatalogs> {
    const params: Record<string, string> = {};
    if (camaraCode?.trim()) {
      params['camaraCode'] = camaraCode.trim();
    }
    if (flujoCode?.trim()) {
      params['flujoCode'] = flujoCode.trim();
    }

    return this.api
      .get<NachaConfigFilterCatalogs>(`${this.basePath}/catalogos-operativos`, { params })
      .pipe(retryNachaConfigRead(onRetry));
  }

  obtenerPerfil(id: number): Observable<NachaConfigProfileDetail> {
    return this.api.get<NachaConfigProfileDetail>(`${this.basePath}/perfiles/${id}`);
  }

  workspace(id: number): Observable<NachaConfigWorkspace> {
    return this.api.get<NachaConfigWorkspace>(`${this.basePath}/perfiles/${id}/workspace`);
  }

  editorCampo(profileId: number, fieldId: number): Observable<NachaConfigFieldEditor> {
    return this.api.get<NachaConfigFieldEditor>(`${this.basePath}/perfiles/${profileId}/fields/${fieldId}/editor`);
  }

  actualizarEditorCampo(profileId: number, fieldId: number, payload: NachaConfigFieldEditorUpdateRequest): Observable<void> {
    return this.api.put<void>(`${this.basePath}/perfiles/${profileId}/fields/${fieldId}/editor`, payload);
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

  probar(id: number): Observable<NachaConfigOperationalTestResult> {
    return this.api.post<NachaConfigOperationalTestResult>(`${this.basePath}/perfiles/${id}/probar`, {});
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

  cambios(id: number): Observable<NachaConfigChangeReview> {
    return this.api.get<NachaConfigChangeReview>(`${this.basePath}/perfiles/${id}/cambios`);
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
