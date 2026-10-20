import { HttpErrorResponse } from '@angular/common/http';

export type IntegrationLoadStatus = 'idle' | 'loading' | 'ready' | 'empty' | 'error' | 'forbidden';
export type IntegrationErrorKind = 'forbidden' | 'network' | 'http' | 'unknown';

export interface IntegrationErrorState {
  kind: IntegrationErrorKind;
  status: number | null;
  message: string;
}

export interface IntegrationPermissionsState {
  canRead: boolean;
  canManage: boolean;
}

export function toIntegrationErrorState(error: unknown): IntegrationErrorState {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 403) {
      return { kind: 'forbidden', status: 403, message: 'No tiene permisos para consultar este recurso.' };
    }

    if (error.status === 0) {
      return { kind: 'network', status: 0, message: 'No fue posible comunicarse con el servicio.' };
    }

    return {
      kind: 'http',
      status: error.status || null,
      message: readServerMessage(error.error) ?? error.message ?? 'La solicitud no pudo completarse.'
    };
  }

  if (error instanceof Error) {
    return { kind: 'unknown', status: null, message: error.message };
  }

  return { kind: 'unknown', status: null, message: 'La solicitud no pudo completarse.' };
}

export function statusFromError(error: IntegrationErrorState): IntegrationLoadStatus {
  return error.kind === 'forbidden' ? 'forbidden' : 'error';
}

function readServerMessage(value: unknown): string | null {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const record = value as Record<string, unknown>;
  const detail = record['detail'];
  if (typeof detail === 'string' && detail.trim().length > 0) {
    return detail;
  }

  const message = record['message'];
  return typeof message === 'string' && message.trim().length > 0 ? message : null;
}
