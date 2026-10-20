import {
  NachaSecurityOperationError,
  NachaSecurityOperationStatus
} from '../models/nacha-security-operation.model';

const STATUS_LABELS: Readonly<Record<NachaSecurityOperationStatus, string>> = {
  Pending: 'Pendiente',
  Running: 'En proceso',
  Success: 'Completado',
  Failed: 'Fallido',
  Rejected: 'Rechazado',
  Expired: 'Vencido'
};

export function nachaOperationStatusLabel(status: NachaSecurityOperationStatus): string {
  return STATUS_LABELS[status] ?? 'Estado no disponible';
}

export function nachaOperationStatusClass(status: NachaSecurityOperationStatus): string {
  switch (status) {
    case 'Success':
      return 'status-success';
    case 'Pending':
    case 'Running':
      return 'status-warning';
    case 'Failed':
    case 'Rejected':
    case 'Expired':
      return 'status-error';
    default:
      return 'status-neutral';
  }
}

export function nachaOperationErrorMessage(error: NachaSecurityOperationError): string {
  switch (error.code) {
    case 'SIGNATURE_VALIDATION_FAILED':
      return 'No fue posible validar la firma digital del archivo. La descarga permanece bloqueada.';
    default:
      return error.retryable
        ? 'La operación no pudo completarse. Puedes intentarlo nuevamente.'
        : 'La operación no pudo completarse. Revisa la configuración y, si el problema continúa, contacta a soporte.';
  }
}
