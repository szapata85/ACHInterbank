import { CertificateListItem } from '../models/certificate-management.model';

export type CertificateEnumValue = string | number | null | undefined;

const PURPOSE_LABELS: Readonly<Record<string, string>> = {
  '1': 'Cifrado de salida',
  OutboundEncryption: 'Cifrado de salida',
  '2': 'Descifrado de entrada',
  InboundDecryption: 'Descifrado de entrada',
  '3': 'Firma de salida',
  OutboundSigning: 'Firma de salida',
  '4': 'Validación de firma de entrada',
  InboundSignatureValidation: 'Validación de firma de entrada',
  '5': 'Firmar y descifrar información de CFA',
  CfaSigningAndDecryption: 'Firmar y descifrar información de CFA',
  '6': 'Validar información recibida',
  ClearingHouseValidation: 'Validar información recibida'
};

const HOLDER_LABELS: Readonly<Record<string, string>> = {
  '1': 'Entidad participante',
  Participant: 'Entidad participante',
  '2': 'Cámara compensadora',
  ClearingHouse: 'Cámara compensadora',
  '3': 'Proveedor de servicios',
  ThirdPartyProvider: 'Proveedor de servicios'
};

const STATUS_LABELS: Readonly<Record<string, string>> = {
  '1': 'Borrador',
  Draft: 'Borrador',
  '2': 'Activo',
  Active: 'Activo',
  '3': 'Inactivo',
  Inactive: 'Inactivo',
  '4': 'Vencido',
  Expired: 'Vencido',
  '5': 'Revocado',
  Revoked: 'Revocado',
  '6': 'Reemplazado',
  Replaced: 'Reemplazado',
  '7': 'Pendiente de vincular secreto',
  PendingSecretBinding: 'Pendiente de vincular secreto',
  '8': 'No válido',
  Invalid: 'No válido',
  NotYetValid: 'Aún no vigente'
};

const ENVIRONMENT_LABELS: Readonly<Record<string, string>> = {
  '1': 'Pruebas',
  Test: 'Pruebas',
  '2': 'Producción',
  Production: 'Producción'
};

export function certificatePurposeLabel(value: CertificateEnumValue): string {
  return PURPOSE_LABELS[String(value ?? '')] ?? 'Propósito no reconocido';
}

export function certificateHolderLabel(value: CertificateEnumValue): string {
  return HOLDER_LABELS[String(value ?? '')] ?? 'Tipo de titular no reconocido';
}

export function certificateEnvironmentLabel(value: CertificateEnumValue): string {
  return ENVIRONMENT_LABELS[String(value ?? '')] ?? 'Ambiente no reconocido';
}

export function certificateStatusLabel(value: CertificateEnumValue): string {
  return STATUS_LABELS[String(value ?? '')] ?? 'Estado no reconocido';
}

export function normalizedCertificateStatus(value: CertificateEnumValue): string {
  const raw = String(value ?? '');
  const numeric: Readonly<Record<string, string>> = {
    '1': 'Draft',
    '2': 'Active',
    '3': 'Inactive',
    '4': 'Expired',
    '5': 'Revoked',
    '6': 'Replaced',
    '7': 'PendingSecretBinding',
    '8': 'Invalid'
  };
  return numeric[raw] ?? raw;
}

export function effectiveCertificateStatus(certificate: CertificateListItem, now = new Date()): string {
  const status = normalizedCertificateStatus(certificate.status);
  if (status === 'Revoked' || status === 'Replaced' || status === 'Invalid') {
    return status;
  }
  const starts = new Date(certificate.notBefore).getTime();
  const ends = new Date(certificate.notAfter).getTime();
  if (Number.isFinite(starts) && starts > now.getTime()) {
    return 'NotYetValid';
  }
  if (Number.isFinite(ends) && ends <= now.getTime()) {
    return 'Expired';
  }
  return status;
}

export function certificateStatusClass(certificate: CertificateListItem): string {
  return `certificate-status-${effectiveCertificateStatus(certificate).replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()}`;
}

export function certificateDaysRemaining(certificate: CertificateListItem, now = new Date()): number | null {
  const end = new Date(certificate.notAfter).getTime();
  if (!Number.isFinite(end)) {
    return null;
  }
  return Math.ceil((end - now.getTime()) / 86_400_000);
}

export function certificateValidityMessage(certificate: CertificateListItem, now = new Date()): string {
  const status = effectiveCertificateStatus(certificate, now);
  const days = certificateDaysRemaining(certificate, now);
  if (status === 'Expired') {
    return 'El certificado está vencido.';
  }
  if (status === 'NotYetValid') {
    return 'El certificado aún no está vigente.';
  }
  if (days !== null && days <= 30) {
    return days === 1
      ? 'Este certificado vence mañana.'
      : `Este certificado vence dentro de ${Math.max(days, 0)} días.`;
  }
  return 'El certificado se encuentra dentro de su periodo de vigencia.';
}

export function maskCertificateThumbprint(value: string | null | undefined): string {
  const normalized = (value ?? '').replace(/[^A-Fa-f0-9]/g, '').toUpperCase();
  if (normalized.length < 12) {
    return 'No disponible';
  }
  return `${normalized.slice(0, 6).match(/.{1,2}/g)?.join(':')}:••:••:${normalized.slice(-6).match(/.{1,2}/g)?.join(':')}`;
}

export function certificatePurposeCode(value: CertificateEnumValue): string {
  const raw = String(value ?? '');
  const numeric: Readonly<Record<string, string>> = {
    '1': 'OutboundEncryption',
    '2': 'InboundDecryption',
    '3': 'OutboundSigning',
    '4': 'InboundSignatureValidation',
    '5': 'CfaSigningAndDecryption',
    '6': 'ClearingHouseValidation'
  };
  return numeric[raw] ?? raw;
}

export function certificateHolderCode(value: CertificateEnumValue): string {
  const raw = String(value ?? '');
  const numeric: Readonly<Record<string, string>> = {
    '1': 'Participant',
    '2': 'ClearingHouse',
    '3': 'ThirdPartyProvider'
  };
  return numeric[raw] ?? raw;
}

export function certificateEnvironmentCode(value: CertificateEnumValue): string {
  const raw = String(value ?? '');
  return raw === '1' ? 'Test' : raw === '2' ? 'Production' : raw;
}
