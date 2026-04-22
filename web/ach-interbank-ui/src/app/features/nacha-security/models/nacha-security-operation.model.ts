export type NachaSecurityOperationStatus = 'Pending' | 'Running' | 'Success' | 'Failed' | 'Rejected' | 'Expired';

export type NachaSecurityOperationType =
  | 'NachaGeneratePlain'
  | 'NachaGenerateEncrypted'
  | 'ManualEnvelopeEncrypt'
  | 'ManualEnvelopeDecrypt'
  | 'DownloadArtifact'
  | 'InteroperabilityRun';

export interface NachaSecurityOperationError {
  code: string;
  message: string;
  retryable: boolean;
}

export interface NachaSecurityArtifact {
  externalFileName?: string | null;
  contentType?: string | null;
  plainHashSha256?: string | null;
  envelopeHashSha256?: string | null;
  downloadAvailable: boolean;
  downloadExpiresAtUtc?: string | null;
  sizeBytes?: number | null;
}

export interface NachaSecurityCertificateSummary {
  signingCertificateThumbprintMasked?: string | null;
  encryptionCertificateThumbprintMasked?: string | null;
  secretRefMasked?: string | null;
}

export interface NachaSecurityOperationResponse {
  operationId: string;
  operationType: NachaSecurityOperationType;
  status: NachaSecurityOperationStatus;
  clearingHouseId?: number | null;
  requestedBy: string;
  requestedAtUtc: string;
  finishedAtUtc?: string | null;
  failCloseApplied: boolean;
  legacyFallbackUsed: boolean;
  artifact: NachaSecurityArtifact;
  error?: NachaSecurityOperationError | null;
  certificateSummary: NachaSecurityCertificateSummary;
}

export interface AuthorizeDownloadResponse {
  operationId: string;
  authorized: boolean;
  expiresAtUtc?: string | null;
}

export interface NachaGenerateOperationRequest {
  cycleId: string;
}

export interface InteroperabilityStatus {
  officialVectorStatus: 'Pending' | 'Ready' | 'Approved' | 'Rejected';
  officialMetadataLoaded: boolean;
  goNoGo: 'GO' | 'NO_GO';
  identifierIvHardening: {
    allowed: boolean;
    reason: string;
  };
  lastHarnessRunUtc?: string | null;
}
