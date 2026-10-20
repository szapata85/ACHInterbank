export const NACHA_SECURITY_PERMISSIONS = {
  canGenerateNacha: 'CanGenerateNacha',
  canGenerateEncryptedNacha: 'CanGenerateEncryptedNacha',
  canManualEncryptEnvelope: 'CanManualEncryptEnvelope',
  canManualDecryptEnvelope: 'CanManualDecryptEnvelope',
  digitalEnvelopeEncrypt: 'DigitalEnvelope.Encrypt',
  digitalEnvelopeDecrypt: 'DigitalEnvelope.Decrypt',
  canDownloadPlainNacha: 'CanDownloadPlainNacha',
  canDownloadEnvelope: 'CanDownloadEnvelope',
  canViewNachaSecurityAudit: 'CanViewNachaSecurityAudit',
  certificatesRead: 'Certificates.Read',
  certificatesUploadPublic: 'Certificates.UploadPublic',
  certificatesRegisterPrivate: 'Certificates.RegisterPrivate',
  certificatesRevoke: 'Certificates.Revoke',
  canManageCertificates: 'CanManageCertificates',
  canManageAch: 'CanManageAch',
  canReadAch: 'CanReadAch'
} as const;
