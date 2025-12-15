export type DigitalEnvelopeCertificateType = 'EncryptionPublic' | 'SigningKeyPair';

export interface DigitalEnvelopeCertificate {
  id: number;
  fileName: string;
  type: DigitalEnvelopeCertificateType;
  hasPrivateKey: boolean;
  subject: string;
  issuer: string;
  thumbprint: string;
  notBefore?: string;
  notAfter?: string;
  uploadedAt: string;
}
