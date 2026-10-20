import {
  PrenotificationValidationAttemptStatus,
  ThirdPartyAccountType,
  ThirdPartyAccountValidationStatus,
  ThirdPartyAdministrativeStatus,
  ThirdPartyTransactionNature
} from './third-party-administration.models';

export function administrativeStatusLabel(status: ThirdPartyAdministrativeStatus): string {
  switch (status) {
    case ThirdPartyAdministrativeStatus.Active: return 'Activo administrativo';
    case ThirdPartyAdministrativeStatus.Inactive: return 'Inactivo administrativo';
    default: return 'Borrador';
  }
}

export function administrativeStatusTone(status: ThirdPartyAdministrativeStatus): 'activo' | 'inactivo' | 'pendiente' {
  switch (status) {
    case ThirdPartyAdministrativeStatus.Active: return 'activo';
    case ThirdPartyAdministrativeStatus.Inactive: return 'inactivo';
    default: return 'pendiente';
  }
}

export function accountTypeLabel(type: ThirdPartyAccountType): string {
  switch (type) {
    case ThirdPartyAccountType.Checking: return 'Corriente';
    case ThirdPartyAccountType.Savings: return 'Ahorros';
    case ThirdPartyAccountType.ElectronicDeposits: return 'Depósito electrónico';
    default: return 'No identificado';
  }
}

export function transactionNatureLabel(nature: ThirdPartyTransactionNature): string {
  return nature === ThirdPartyTransactionNature.Debit ? 'Débito' : 'Crédito';
}

export function validationStatusLabel(status: ThirdPartyAccountValidationStatus): string {
  switch (status) {
    case ThirdPartyAccountValidationStatus.Accepted: return 'Aceptada / certificada';
    case ThirdPartyAccountValidationStatus.TacitlyAccepted: return 'Aceptación tácita';
    case ThirdPartyAccountValidationStatus.Rejected: return 'Rechazada';
    case ThirdPartyAccountValidationStatus.RequiresCorrection: return 'Requiere corrección / revalidación';
    case ThirdPartyAccountValidationStatus.TechnicalReview: return 'Revisión técnica';
    case ThirdPartyAccountValidationStatus.PendingSend:
    case ThirdPartyAccountValidationStatus.PendingResponse: return 'Pendiente';
    case ThirdPartyAccountValidationStatus.Expired: return 'Expirada';
    case ThirdPartyAccountValidationStatus.Suspended: return 'Suspendida';
    default: return 'Sin iniciar';
  }
}

export function validationStatusTone(status: ThirdPartyAccountValidationStatus): 'activo' | 'inactivo' | 'pendiente' | 'fallido' | 'exitoso' {
  switch (status) {
    case ThirdPartyAccountValidationStatus.Accepted:
    case ThirdPartyAccountValidationStatus.TacitlyAccepted:
      return 'exitoso';
    case ThirdPartyAccountValidationStatus.Rejected:
    case ThirdPartyAccountValidationStatus.RequiresCorrection:
    case ThirdPartyAccountValidationStatus.TechnicalReview:
      return 'fallido';
    case ThirdPartyAccountValidationStatus.Expired:
    case ThirdPartyAccountValidationStatus.Suspended:
      return 'inactivo';
    default:
      return 'pendiente';
  }
}

export function attemptStatusLabel(status: PrenotificationValidationAttemptStatus): string {
  switch (status) {
    case PrenotificationValidationAttemptStatus.Accepted: return 'Aceptada / certificada';
    case PrenotificationValidationAttemptStatus.TacitlyAccepted: return 'Aceptación tácita';
    case PrenotificationValidationAttemptStatus.Rejected: return 'Rechazada';
    case PrenotificationValidationAttemptStatus.RequiresCorrection: return 'Requiere corrección / revalidación';
    case PrenotificationValidationAttemptStatus.TechnicalReview: return 'Revisión técnica';
    case PrenotificationValidationAttemptStatus.Failed: return 'Fallida';
    case PrenotificationValidationAttemptStatus.Cancelled: return 'Cancelada';
    case PrenotificationValidationAttemptStatus.AwaitingResponse: return 'Pendiente de respuesta';
    case PrenotificationValidationAttemptStatus.Sent: return 'Enviada';
    case PrenotificationValidationAttemptStatus.Queued: return 'En cola';
    default: return 'Creada';
  }
}

export function maskForDisplay(value: string | null | undefined): string {
  const trimmed = value?.trim() ?? '';
  if (!trimmed) return '—';
  if (trimmed.length <= 4) return '••••';
  return `${'•'.repeat(Math.min(8, trimmed.length - 4))}${trimmed.slice(-4)}`;
}
