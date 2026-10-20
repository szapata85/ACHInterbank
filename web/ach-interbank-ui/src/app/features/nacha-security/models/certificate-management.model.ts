export type ManagedCertificatePurpose =
  | 'CfaSigningAndDecryption'
  | 'ClearingHouseValidation';

export type CertificateFunctionalStatus =
  | 'PendingValidity'
  | 'Valid'
  | 'ExpiringSoon'
  | 'Expired'
  | 'Revoked'
  | 'Replaced'
  | 'Inactive'
  | number;

export interface CertificateListItem {
  id: number;
  code: string;
  displayName: string;
  fileName: string;
  financialInstitutionId?: number | null;
  financialInstitutionName?: string | null;
  clearingHouseId?: number | null;
  clearingHouseName?: string | null;
  environment: string | number;
  purpose: string | number;
  holderType: string | number;
  status: string | number;
  functionalStatus: CertificateFunctionalStatus;
  daysRemaining?: number | null;
  versionNumber: number;
  subject: string;
  issuer: string;
  serialNumber: string;
  thumbprint: string;
  fingerprintSha256: string;
  notBefore: string;
  notAfter: string;
  hasPrivateKey: boolean;
  keyAlgorithm: string;
  keySize: number;
  signatureAlgorithm: string;
  secretRefMasked?: string | null;
  uploadedAtUtc: string;
  uploadedBy: string;
  activatedAtUtc?: string | null;
  revokedAtUtc?: string | null;
  revocationReason?: string | null;
  revokedBy?: string | null;
  canDelete: boolean;
}

export interface CertificatePreview {
  purpose: ManagedCertificatePurpose | number;
  financialInstitutionId?: number | null;
  financialInstitutionName?: string | null;
  clearingHouseId?: number | null;
  clearingHouseName?: string | null;
  subject: string;
  issuer: string;
  serialNumber: string;
  thumbprint: string;
  notBefore: string;
  notAfter: string;
  hasPrivateKey: boolean;
  keyAlgorithm: string;
  keySize: number;
  signatureAlgorithm: string;
  functionalStatus: CertificateFunctionalStatus;
  daysRemaining?: number | null;
  canSignAndDecrypt: boolean;
  isValid: boolean;
  warnings: string[];
}

export interface CertificateVersion extends CertificateListItem {}

export interface CertificateValidationResult {
  isValid?: boolean;
  canActivate?: boolean;
  errors: string[];
  warnings?: string[];
}

export interface DeleteCertificateResult {
  versionId: number;
  deleted: boolean;
}
