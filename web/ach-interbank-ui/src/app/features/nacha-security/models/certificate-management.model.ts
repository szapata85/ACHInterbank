export interface CertificateListItem {
  id: number;
  code: string;
  displayName: string;
  fileName: string;
  clearingHouseId: number;
  environment: string | number;
  purpose: string | number;
  holderType: string | number;
  status: string | number;
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
}

export interface CertificateVersion extends CertificateListItem {}

export interface CertificateValidationResult {
  isValid?: boolean;
  canActivate?: boolean;
  errors: string[];
  warnings?: string[];
}
