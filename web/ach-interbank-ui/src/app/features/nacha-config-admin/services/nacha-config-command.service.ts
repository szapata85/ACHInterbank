import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NachaConfigApiError } from '../models/nacha-config-admin.models';
import { NachaConfigApiService } from './nacha-config-api.service';

@Injectable({ providedIn: 'root' })
export class NachaConfigCommandService {
  private readonly api = inject(NachaConfigApiService);

  crearBorrador(payload: Record<string, unknown>) {
    return this.api.crearBorrador(payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  editarBorrador(profileId: number, payload: Record<string, unknown>) {
    return this.api.editarBorrador(profileId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  clonar(profileId: number, payload: Record<string, unknown>) {
    return this.api.clonarPerfil(profileId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  validar(profileId: number) {
    return this.api.validar(profileId).pipe(catchError((error) => this.normalizeError(error)));
  }

  publicar(profileId: number, expectedRowVersion: string) {
    return this.api.publicar(profileId, expectedRowVersion).pipe(catchError((error) => this.normalizeError(error)));
  }

  inactivar(profileId: number, expectedRowVersion: string) {
    return this.api.inactivar(profileId, expectedRowVersion).pipe(catchError((error) => this.normalizeError(error)));
  }

  archivar(profileId: number, expectedRowVersion: string) {
    return this.api.archivar(profileId, expectedRowVersion).pipe(catchError((error) => this.normalizeError(error)));
  }

  actualizarSecuencia(profileId: number, payload: Record<string, unknown>) {
    return this.api.actualizarSecuencia(profileId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  actualizarVariante(profileId: number, variantId: number, payload: Record<string, unknown>) {
    return this.api.actualizarVariante(profileId, variantId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  actualizarField(profileId: number, fieldId: number, payload: Record<string, unknown>) {
    return this.api.actualizarField(profileId, fieldId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  actualizarRule(profileId: number, ruleId: number, payload: Record<string, unknown>) {
    return this.api.actualizarRule(profileId, ruleId, payload).pipe(catchError((error) => this.normalizeError(error)));
  }

  preview(payload: Record<string, unknown>) {
    return this.api.preview(payload as any).pipe(catchError((error) => this.normalizeError(error)));
  }

  private normalizeError(error: unknown) {
    if (error instanceof HttpErrorResponse) {
      const backend = error.error as NachaConfigApiError | undefined;
      const payload: NachaConfigApiError = {
        errorCode: backend?.errorCode ?? `HTTP_${error.status}`,
        message: backend?.message ?? 'Ocurrió un error inesperado en la configuración NACHA-M.',
        currentRowVersion: backend?.currentRowVersion,
        issues: backend?.issues ?? []
      };

      return throwError(() => payload);
    }

    return throwError(() => ({
      errorCode: 'UNKNOWN',
      message: 'No fue posible completar la operación.',
      issues: []
    } as NachaConfigApiError));
  }
}
