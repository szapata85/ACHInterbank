export interface IncomingNachaPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export type IncomingNachaIngestionStatus =
  | 'Recibido' | 'Duplicado' | 'EnValidacion' | 'PendienteResolucion'
  | 'ListoParaParseo' | 'Parseado' | 'Bloqueado' | 'Fallido' | 'Completado';

export type IncomingNachaProcessingStatus =
  | 'Scheduled' | 'Processing' | 'Completed' | 'RetryPending' | 'TechnicalFailed';

export type IncomingNachaBusinessOutcome =
  | 'Successful' | 'Rejected' | 'Returned' | 'PendingResponse' | 'NotProcessed';

export interface IncomingNachaFileFilters {
  page: number;
  pageSize: number;
  fileName?: string;
  clearingHouseId?: number;
  operationalDate?: string;
  uploadedFromUtc?: string;
  uploadedToUtc?: string;
  achCycleId?: string;
  ingestionStatus?: IncomingNachaIngestionStatus;
  businessOutcome?: IncomingNachaBusinessOutcome;
  resultCode?: string;
  hasIssues?: boolean;
  hasTechnicalErrors?: boolean;
  sortBy: string;
  sortDescending: boolean;
}

export interface IncomingNachaFileListItem {
  id: string;
  fileName: string;
  correlationId: string;
  ingestionStatus: IncomingNachaIngestionStatus | number;
  ingestionStatusText: string;
  stageCode: string;
  stageText: string;
  cycleResolutionStatus: string | number;
  parsingStatus: string | number;
  resolvedClearingHouseId?: number | null;
  clearingHouseName: string;
  resolvedAchCycleId?: string | null;
  operationalDate?: string | null;
  uploadedAtUtc: string;
  uploadedBy: string;
  queueItems: number;
  processingEvents: number;
  totalBatches: number;
  totalTransactions: number;
  totalDebit: number;
  totalCredit: number;
  processingStatusText: string;
  overallResultText: string;
  scheduledAtUtc?: string | null;
  hasTechnicalErrors: boolean;
  hasIssues: boolean;
}

export interface IncomingNachaFileSummary {
  totalBatches: number;
  totalTransactions: number;
  totalAddendas: number;
  totalDebit: number;
  totalCredit: number;
  successfulTransactions: number;
  rejectedTransactions: number;
  returnedTransactions: number;
  technicalFailures: number;
}

export interface IncomingNachaFileDetail {
  id: string;
  fileName: string;
  correlationId: string;
  ingestionStatus: IncomingNachaIngestionStatus | number;
  ingestionStatusText: string;
  stageCode: string;
  stageText: string;
  cycleResolutionStatus: string | number;
  parsingStatus: string | number;
  detectedClearingHouseId?: number | null;
  resolvedClearingHouseId?: number | null;
  clearingHouseName: string;
  resolvedAchCycleId?: string | null;
  operationalDate?: string | null;
  notes: string;
  uploadedBy: string;
  uploadedAtUtc: string;
  receivedAtUtc?: string | null;
  overallResultText: string;
  pendingTransactions: number;
  summary: IncomingNachaFileSummary;
  admissionIssue?: IncomingNachaAdmissionIssue | null;
  queue: IncomingNachaQueueItem[];
  events: IncomingNachaEvent[];
}

export interface IncomingNachaAdmissionIssue {
  code: string;
  title: string;
  message: string;
  suggestedAction: string;
  expectedValue?: string | null;
  foundValue?: string | null;
  severity: string;
}

export interface IncomingNachaValidation {
  code: string;
  title: string;
  message: string;
  expectedValue?: string | null;
  foundValue?: string | null;
  suggestedAction: string;
  errorType: string;
  severity: string;
  isSuccessful: boolean;
  occurredAtUtc?: string | null;
}

export interface IncomingNachaBatch {
  id: number;
  batchNumber: number;
  companyName: string;
  serviceClassCode: string;
  standardEntryClassCode: string;
  companyEntryDescription: string;
  effectiveEntryDate?: string | null;
  totalTransactions: number;
  totalAmount: number;
  totalDebit: number;
  totalCredit: number;
}

export interface IncomingNachaTransactionFilters {
  page: number;
  pageSize: number;
  batchId?: number;
  search?: string;
  transactionCode?: string;
  processingStatus?: IncomingNachaProcessingStatus;
  businessOutcome?: IncomingNachaBusinessOutcome;
  resultCode?: string;
  hasAddenda?: boolean;
  hasTechnicalError?: boolean;
  sortBy: string;
  sortDescending: boolean;
}

export interface IncomingNachaTransaction {
  id: number;
  batchId: number;
  batchNumber: number;
  traceNumber: string;
  transactionCode: string;
  transactionCodeDescription: string;
  amount: number;
  addendaCount: number;
  classificationCode: string;
  classificationText: string;
  dispatchQueueId?: string | null;
  dispatchStatusCode: string;
  dispatchStatusText: string;
  attemptCount: number;
  maxAttempts: number;
  processingStatus?: IncomingNachaProcessingStatus | number | null;
  processingStatusText: string;
  businessOutcome?: IncomingNachaBusinessOutcome | number | null;
  businessOutcomeText: string;
  resultCode: string;
  resultDescription: string;
  processedAtUtc?: string | null;
  scheduledAtUtc?: string | null;
  startedAtUtc?: string | null;
  finishedAtUtc?: string | null;
  nextRetryAtUtc?: string | null;
  correlationId: string;
  clearingHouseId: number;
  operationalDate?: string | null;
  achCycleId: string;
  soapOperation: string;
  externalTransactionId: string;
  achReturnCodeId?: number | null;
  technicalErrorCode: string;
  technicalErrorMessage: string;
  accountNumberMasked: string;
  originInstitution: string;
  destinationInstitution: string;
  recipientNameMasked: string;
  effectiveEntryDate: string;
}

export interface IncomingNachaAddenda {
  id: number;
  typeCode: string;
  sequence: string;
  returnReasonCode: string;
  originalTraceNumber: string;
  paymentInformation: string;
}

export interface IncomingNachaQueueItem {
  id: string;
  ingestionId: string;
  entryDetailId?: number | null;
  queueStatus: string | number;
  queueStatusText: string;
  attemptCount: number;
  maxAttempts: number;
  nextAttemptAtUtc?: string | null;
  lastAttemptAtUtc?: string | null;
  scheduledAtUtc: string;
  soapOperation: string;
  lastErrorCode: string;
  lastErrorMessage: string;
  lastResponseCode: string;
}

export interface IncomingNachaQueueDetail {
  queue: IncomingNachaQueueItem;
  classification: IncomingNachaClassification;
  executions: IncomingNachaExecution[];
  events: IncomingNachaEvent[];
}

export interface IncomingNachaClassification {
  functionalClass: string | number;
  eligibilityStatus: string | number;
  requiresManualResolution: boolean;
  returnReasonCode?: string | null;
  prenoteStatus: string | number;
  businessMeaning: string;
}

export interface IncomingNachaExecution {
  id: string;
  attemptNumber: number;
  methodName: string;
  correlationId: string;
  processingStatusText: string;
  businessOutcomeText: string;
  resultCode: string;
  resultDescription: string;
  isSuccess: boolean;
  isRetryable: boolean;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
  processedAtUtc?: string | null;
  logicalEndpoint: string;
  durationMs: number;
  transportStatusText: string;
  technicalErrorCode: string;
  technicalErrorMessage: string;
  externalTransactionId: string;
}

export interface IncomingNachaEvent {
  id: string;
  eventType: string;
  eventStatus: string;
  message: string;
  occurredAtUtc: string;
  raisedBy: string;
}

export interface IncomingNachaObservabilitySummary {
  generatedAtUtc: string;
  windowHours: number;
  pipelineHealth: {
    totalIngestions: number;
    totalQueueItems: number;
    backlogItems: number;
    blockedItems: number;
    retryPendingItems: number;
    failedFinalItems: number;
    confirmedItems: number;
  };
}

export interface ClearingHouseOption {
  id: number;
  name: string;
}
