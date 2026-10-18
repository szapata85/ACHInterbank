export enum ClaimCauseCode {
  Dev14 = 1
}

export enum ClaimCaseStatus {
  Open = 1,
  Resolved = 2,
  Expired = 3
}

export enum ClaimCaseEventType {
  Opened = 1,
  Progress = 2,
  Resolved = 3,
  Expired = 4
}

export enum AchTransactionType {
  Credit = 1,
  Debit = 2,
  Prenotification = 3,
  Reversal = 4,
  Return = 5
}

export interface ClaimParticipant {
  id: number;
  name: string;
  routingNumber: string;
  transitCode: string;
}

export interface ClaimCaseListItem {
  id: number;
  causeCode: ClaimCauseCode;
  status: ClaimCaseStatus;
  filedAt: string;
  deadlineAt?: string | null;
  closedAt?: string | null;
  externalReference?: string | null;
  requestingParticipant: ClaimParticipant;
  respondingParticipant: ClaimParticipant;
  transactionCount: number;
}

export interface ClaimTransaction {
  id: number;
  traceNumber: string;
  transactionExternalId: string;
  amount: number;
  type: AchTransactionType;
  sourceInstitution: ClaimParticipant;
  destinationInstitution: ClaimParticipant;
}

export interface ClaimCaseEvent {
  id: number;
  eventType: ClaimCaseEventType;
  occurredAt: string;
  observation?: string | null;
  actor?: string | null;
}

export interface ClaimCaseDetail extends Omit<ClaimCaseListItem, 'transactionCount'> {
  transactions: ClaimTransaction[];
  history: ClaimCaseEvent[];
  resolutionNotes?: string | null;
}

export interface ClaimCaseQuery {
  status?: ClaimCaseStatus;
  causeCode?: ClaimCauseCode;
  transactionId?: number;
  participantId?: number;
  from?: string;
  to?: string;
  pageNumber: number;
  pageSize: number;
}

export interface ClaimCaseQueryResult {
  items: ClaimCaseListItem[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
}

export interface CreateDev14ClaimCaseRequest {
  transactionIds: number[];
  externalReference?: string;
}

export interface RecordClaimProgressRequest {
  observation?: string;
}

export interface ResolveClaimCaseRequest {
  resolutionNotes?: string;
}

export interface ExpireClaimCaseRequest {
  observation?: string;
}
