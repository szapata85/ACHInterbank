export type TransferDirection = 'Outbound' | 'Inbound';
export type ExecutionOrigin = 'Automatic' | 'Manual';
export type TransferStatus = 'Ready' | 'InProgress' | 'Transferred' | 'Received' | 'Processed' | 'Rejected' | 'Duplicate' | 'RetryPending' | 'Uncertain' | 'Failed' | 'Retired';

export interface TransferSummary {
  id: string; fileName: string; direction: TransferDirection; operationalDate: string; cycleId?: string;
  status: TransferStatus; executionOrigin: ExecutionOrigin; attemptCount: number; updatedAtUtc: string;
  archived: boolean; retired: boolean;
}
export interface TransferEvent { id: number; occurredAtUtc: string; eventType: string; result: string; message: string; executionOrigin: ExecutionOrigin; actor: string; }
export interface TransferDetail extends TransferSummary {
  fileSize: number; contentSha256: string; createdAtUtc: string; transferredAtUtc?: string; processedAtUtc?: string;
  lastError?: string; archivedAtUtc?: string; retiredAtUtc?: string; retirementReason?: string;
  correctedFromTransferId?: string; history: TransferEvent[];
}
export interface TransferConfiguration {
  automaticOutboundEnabled: boolean; automaticInboundEnabled: boolean; manualOutboundAllowed: boolean;
  manualInboundAllowed: boolean; maximumRetries: number; retentionDays: number; outboundLocation: string;
  inboundLocation: string; archiveLocation: string; concurrencyToken: string;
}
