export const NACHA_SECURITY_PERMISSIONS = {
  canGenerateNacha: 'CanGenerateNacha',
  canGenerateEncryptedNacha: 'CanGenerateEncryptedNacha',
  canManualEncryptEnvelope: 'CanManualEncryptEnvelope',
  canManualDecryptEnvelope: 'CanManualDecryptEnvelope',
  canDownloadPlainNacha: 'CanDownloadPlainNacha',
  canDownloadEnvelope: 'CanDownloadEnvelope',
  canViewNachaSecurityAudit: 'CanViewNachaSecurityAudit',
  canManageCertificates: 'CanManageCertificates',
  canRunInteroperabilityHarness: 'CanRunInteroperabilityHarness',
  canManageAch: 'CanManageAch',
  canReadAch: 'CanReadAch'
} as const;
