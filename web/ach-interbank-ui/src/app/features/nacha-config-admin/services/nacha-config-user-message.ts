import { HttpErrorResponse } from '@angular/common/http';
import { NachaConfigApiError } from '../../../core/models/nacha-config.model';

export function nachaConfigUserMessage(error: unknown, fallback: string): string {
  const apiError = readApiError(error);
  const code = apiError?.errorCode?.toUpperCase() ?? '';

  if (code === 'CONCURRENCY_CONFLICT' || code.includes('409')) {
    return 'La configuración fue modificada por otro usuario. Recarga los cambios antes de continuar.';
  }
  if (code === 'INVALID_PROFILE_STATE' || code === 'PROFILE_NOT_EDITABLE') {
    return 'Esta configuración está protegida y ya no admite edición directa. Recarga el perfil para continuar.';
  }
  if (code === 'VALIDATION_BLOCKED' || code === 'VALIDATION_ERROR' || code.includes('400')) {
    return 'La configuración contiene datos que requieren corrección. Revisa los campos indicados y vuelve a intentarlo.';
  }
  if (code === 'COMPARISON_BASE_CONTEXT_MISMATCH') {
    return 'No fue posible comparar esta versión con su base porque pertenecen a contextos distintos. La publicación permanece bloqueada.';
  }
  if (code.includes('403') || code.includes('401')) {
    return 'Tu sesión no tiene permisos para completar esta operación.';
  }
  if (code.includes('404')) {
    return 'La configuración solicitada ya no está disponible. Vuelve al listado y actualiza la consulta.';
  }
  if (code.includes('429')) {
    return 'El servidor está atendiendo varias solicitudes. Espera un momento y vuelve a intentarlo.';
  }
  if (code.startsWith('HTTP_5')) {
    return 'El servidor no pudo completar la operación. La configuración se conserva sin cambios; vuelve a intentarlo más tarde.';
  }

  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return 'No fue posible conectarse con el servidor. La configuración se conserva sin cambios.';
    }
    if (error.status === 401 || error.status === 403) {
      return 'Tu sesión no tiene permisos para completar esta operación.';
    }
    if (error.status === 404) {
      return 'La configuración solicitada ya no está disponible. Vuelve al listado y actualiza la consulta.';
    }
    if (error.status === 429) {
      return 'El servidor está atendiendo varias solicitudes. Espera un momento y vuelve a intentarlo.';
    }
    if (error.status >= 500) {
      return 'El servidor no pudo completar la operación. La configuración se conserva sin cambios; vuelve a intentarlo más tarde.';
    }
  }

  return fallback;
}

function readApiError(error: unknown): NachaConfigApiError | null {
  if (isApiError(error)) {
    return error;
  }
  if (error instanceof HttpErrorResponse && isApiError(error.error)) {
    return error.error;
  }
  return null;
}

function isApiError(value: unknown): value is NachaConfigApiError {
  return !!value
    && typeof value === 'object'
    && 'errorCode' in value
    && typeof (value as { errorCode?: unknown }).errorCode === 'string';
}
