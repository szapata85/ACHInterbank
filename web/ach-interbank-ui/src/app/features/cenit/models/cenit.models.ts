export interface CenitReturnCode {
  code: string;
  description: string;
  appliesToDebit: boolean;
  appliesToCredit: boolean;
  appliesToPrenotification: boolean;
  appliesToReturn: boolean;
  requiresAddenda: boolean;
  maxDaysAllowed?: number | null;
  isActive: boolean;
  regulatorySource?: string;
}

export interface CenitFileRejectionCode {
  code: string;
  description: string;
  severity: string;
  appliesToStage: string;
  isRetryable: boolean;
  isActive: boolean;
}

export interface CenitTransactionTypePolicy {
  transactionType: string;
  priorityOrder: number;
  isMonetary: boolean;
  requiresPrenotification: boolean;
  canBeReturned: boolean;
  canBeReturnedAgain: boolean;
  isActive: boolean;
}

export interface CenitReturnPolicy {
  transactionType: string;
  allowedReturnCodesCsv: string;
  maxDays: number;
  requiredOriginalTransactionState: string;
  allowsReturnOfReturn: boolean;
  requiresAddenda: boolean;
  isActive: boolean;
}

export interface CenitReturnOfReturnPolicy {
  originalReturnCode: string;
  allowedNewReturnCodesCsv: string;
  maxDays: number;
  requiredOriginalState: string;
  isUniquePerTransaction: boolean;
  isActive: boolean;
}

export interface CenitPrenotificationPolicy {
  transactionType: string;
  isRequired: boolean;
  requiresAddenda: boolean;
  blocksMonetaryTransactionIfMissing: boolean;
  isActive: boolean;
}

export interface CenitTraceabilityRow {
  transactionId: number;
  effectiveEntryDate: string;
  transactionExternalId: string;
  reference: string;
  amount: number;
  state: string;
  causalCode: string;
  causalDescription: string;
  causalKind?: string;
  clearingHouseName: string;
  achCycleId: string;
  achCycleName: string;
  originalTraceRef: string;
  batchId?: number;
  batchSequenceNumber?: number;
  decisionType?: string;
  sourceFileReference?: string;
}

export interface CenitQueueRow {
  id: number;
  status: string;
  queueReason: string;
  enqueuedAtUtc: string;
  dequeuedAtUtc?: string | null;
  targetAchCycleId: string;
  targetCycleName: string;
  originalAchCycleId?: string | null;
  transactionId: number;
  transactionExternalId?: string | null;
  reference?: string | null;
  amount: number;
  transactionType: string;
  transactionState: string;
  effectiveEntryDate: string;
  cenitCycleExecutionId?: number | null;
}

export interface CenitSimplePage<T> {
  items: T[];
}

export interface CenitCycleRow {
  cycleId: string;
  cycleName: string;
  processingDate: string;
  status: string;
  clearingHouseName: string;
  totalTransactions: number;
  totalAmount: number;
}

export interface CenitNetPositionRow {
  financialInstitutionName?: string;
  financialInstitutionId?: number;
  netAmount?: number;
  availableLiquidity?: number;
  liquiditySourceType?: string;
}

export interface CenitOptimizationDecisionRow {
  achTransactionId: number;
  decisionType: string;
  decisionReason: string;
  priority: number;
  fromCycleId: string;
  toCycleId?: string | null;
  decidedAtUtc: string;
}

export interface CenitChamberResponseRow {
  id: string;
  isDuplicate: boolean;
  sourceResponseId: string;
  sourceFileName: string;
  rawTechnicalReference: string;
  responseType: 'Unknown' | 'Ack' | 'Nack' | 'OperatorRejected' | 'Reconciliation' | 'NoActivity';
  state: 'Pending' | 'Accepted' | 'Rejected' | 'OperatorRejected' | 'Reconciliation' | 'NoActivity';
  correlationOutcome: 'Pending' | 'Matched' | 'NotFound' | 'Ambiguous' | 'TransactionNotFound' | 'TransactionAmbiguous' | 'Invalid' | 'InvalidTransition';
  relatedFileId?: number | null;
  relatedFileName?: string | null;
  relatedTransactionId?: number | null;
  transactionTraceNumber?: string | null;
  reasonCode?: string | null;
  description?: string | null;
  receivedAtUtc: string;
  processedAtUtc?: string | null;
  isApplied: boolean;
  problemCode?: string | null;
}
