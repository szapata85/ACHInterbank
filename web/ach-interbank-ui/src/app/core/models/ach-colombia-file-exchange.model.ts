export type TransferDirection = 'Outbound' | 'Inbound';
export type ExecutionOrigin = 'Automatic' | 'Manual';
export type TransferStatus = 'Ready' | 'InProgress' | 'Transferred' | 'Received' | 'Processed' | 'Rejected' | 'Duplicate' | 'RetryPending' | 'Uncertain' | 'Failed' | 'Retired';

export interface TransferSummary {
  id: string; fileName: string; direction: TransferDirection; operationalDate: string; cycleId?: string;
  status: TransferStatus; executionOrigin: ExecutionOrigin; attemptCount: number; updatedAtUtc: string;
  archived: boolean; retired: boolean;
  statusLabel?: string; externalConfirmationStatus?: 'NotConfirmed' | 'NotApplicable'; externalConfirmationMessage?: string;
}
export interface TransferEvent {
  id: number; occurredAtUtc: string; eventType: string; result: string; message: string; executionOrigin: ExecutionOrigin; actor: string;
  displayName?: string; displayResult?: string; displayMessage?: string; actorDisplay?: string;
}
export interface TransferDetail extends TransferSummary {
  fileSize: number; contentSha256: string; createdAtUtc: string; transferredAtUtc?: string; processedAtUtc?: string;
  lastError?: string; archivedAtUtc?: string; retiredAtUtc?: string; retirementReason?: string;
  correctedFromTransferId?: string; history: TransferEvent[];
  lastAttemptAtUtc?: string; lastErrorCode?: string; correlationId?: string;
  achFileExportId?: number; incomingNachaFileIngestionId?: string; transactionIds: number[];
  contentAvailable: boolean; canRetry: boolean; canReprocess: boolean; canArchive: boolean; canRetire: boolean;
  clearingHouseCode?: string; clearingHouseName?: string; statusLabel?: string;
  externalConfirmationStatus?: 'NotConfirmed' | 'NotApplicable'; externalConfirmationMessage?: string;
  sanitizedActiveReference?: string; sanitizedArchiveReference?: string;
}

export interface TransferFilter {
  from: string; to: string; direction: TransferDirection | ''; status: TransferStatus | '';
  executionOrigin: ExecutionOrigin | ''; cycleId: string; fileName: string; transferId: string;
  archived: '' | 'true' | 'false'; pageNumber: number; pageSize: number;
}

export interface ManagedMftAdministration {
  profileName: string; provider: string; protocol: string; profileEnabled: boolean; endpoint?: string; port?: number; principal?: string;
  automaticOutboundEnabled: boolean; automaticInboundEnabled: boolean; manualOutboundAllowed: boolean; manualInboundAllowed: boolean;
  maximumRetries: number; retryDelaySeconds: number; retentionDays: number; outboundLocation: string; inboundLocation: string; archiveLocation: string;
  credentialConfigured: boolean; credentialType?: string; credentialUpdatedAtUtc?: string; concurrencyToken: string;
  sftpHostKeyFingerprint?: string; sftpTimeoutSeconds: number;
}

export interface ManagedMftConnectionTestResult {
  succeeded: boolean;
  code: string;
  message: string;
}
