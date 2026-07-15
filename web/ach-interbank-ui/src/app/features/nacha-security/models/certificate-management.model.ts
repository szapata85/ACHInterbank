export interface CertificateListItem {
  id: number;
  code: string;
  displayName: string;
  fileName: string;
  clearingHouseId: number;
  environment: string;
  purpose: string;
  holderType: string;
  status: string;
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
  canActivate: boolean;
  errors: string[];
  warnings: string[];
}
