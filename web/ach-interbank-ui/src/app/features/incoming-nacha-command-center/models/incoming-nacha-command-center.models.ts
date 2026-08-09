export interface IncomingNachaPageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface IncomingNachaAllowedActions {
  currentStatus: string;
  canRetry: boolean;
  canUnblock: boolean;
  canRequeue: boolean;
  canMarkFailedFinal: boolean;
  allowedActions: string[];
}

export interface IncomingNachaIngestionListItem {
  id: string;
  fileName: string;
  correlationId: string;
  ingestionStatus: string;
  cycleResolutionStatus: string;
  parsingStatus: string;
  resolvedClearingHouseId?: number | null;
  resolvedAchCycleId?: string | null;
  operationalDate?: string | null;
  isReprocess: boolean;
  uploadedAtUtc: string;
  queueItems: number;
  processingEvents: number;
}

export interface IncomingNachaQueueListItem {
  id: string;
  ingestionId: string;
  achTransactionId: number;
  achCycleId: string;
  clearingHouseId: number;
  queueStatus: string;
  priority: number;
  attemptCount: number;
  nextAttemptAtUtc?: string | null;
  lastAttemptAtUtc?: string | null;
  lastErrorCode: string;
  lastErrorMessage: string;
  lastResponseCode: string;
  confirmedAtUtc?: string | null;
  createdAtUtc: string;
  allowedActions: IncomingNachaAllowedActions;
}

export interface IncomingNachaProcessingEvent {
  id: string;
  eventType: string;
  eventStatus: string;
  message: string;
  occurredAtUtc: string;
  raisedBy: string;
  achTransactionId?: number | null;
}

export interface IncomingNachaIngestionDetail {
  id: string;
  fileName: string;
  correlationId: string;
  ingestionStatus: string;
  cycleResolutionStatus: string;
  parsingStatus: string;
  detectedClearingHouseId?: number | null;
  resolvedClearingHouseId?: number | null;
  resolvedAchCycleId?: string | null;
  operationalDate?: string | null;
  notes: string;
  isReprocess: boolean;
  parentIngestionId?: string | null;
  queue: IncomingNachaQueueListItem[];
  events: IncomingNachaProcessingEvent[];
}

export interface IncomingNachaEntryClassification {
  id: string;
  entryDetailId: number;
  addendaRecordId?: number | null;
  functionalClass: string;
  eligibilityStatus: string;
  requiresManualResolution: boolean;
  returnReasonCode?: string | null;
  prenoteStatus: string;
  businessMeaning: string;
}

export interface IncomingNachaIntegrationExecution {
  id: string;
  dispatchQueueId: string;
  methodName: string;
  correlationId: string;
  responseCode: string;
  responseMessage: string;
  isSuccess: boolean;
  isRetryable: boolean;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
}

export interface IncomingNachaQueueDetail {
  queue: IncomingNachaQueueListItem;
  ingestion: IncomingNachaIngestionListItem;
  classification: IncomingNachaEntryClassification;
  executions: IncomingNachaIntegrationExecution[];
  events: IncomingNachaProcessingEvent[];
}

export interface IncomingNachaManualActionRequest {
  justification: string;
  idempotencyKey: string;
  priority?: number | null;
}

export interface IncomingNachaManualActionResult {
  queueId: string;
  action: string;
  previousStatus: string;
  currentStatus: string;
  isIdempotentReplay: boolean;
  message: string;
}

export interface IncomingNachaOrphan {
  id: string;
  ingestionId: string;
  fileName: string;
  receivedAtUtc: string;
  clearingHouseId?: number | null;
  achCycleId: string;
  operationalDate?: string | null;
  entryDetailId: number;
  addendaRecordId?: number | null;
  amount: number;
  traceNumber: string;
  originalTraceNumber: string;
  returnReasonCode: string;
  returnReasonDescription: string;
  accountNumberMasked: string;
  recipientNameMasked: string;
  originInstitution: string;
  destinationInstitution: string;
  linkType: string;
  candidateTransactionIds: number[];
  resolvedAchTransactionId?: number | null;
  resolutionStatus: string;
  resolvedBy: string;
  resolvedAtUtc?: string | null;
}

export interface IncomingNachaOrphanCandidate {
  achTransactionId: number;
  traceNumber: string;
  amount: number;
  effectiveEntryDate: string;
  state: string;
  accountNumberMasked: string;
  originInstitution: string;
  destinationInstitution: string;
  isCompatible: boolean;
  incompatibilityReasons: string[];
}

export interface IncomingNachaOrphanResolveRequest {
  achTransactionId: number;
  justification: string;
  comment: string;
  correlationId: string;
}

export interface IncomingNachaOrphanResolutionResult {
  isResolved: boolean;
  status: string;
  processingEventId?: string | null;
  achTransactionStateEventId?: number | null;
  message: string;
  isIdempotentReplay: boolean;
}

export interface IncomingNachaPipelineHealth {
  totalIngestions: number;
  totalQueueItems: number;
  backlogItems: number;
  blockedItems: number;
  retryPendingItems: number;
  waitingWindowItems: number;
  failedFinalItems: number;
  confirmedItems: number;
  averageQueueAgeMinutes: number;
  oldestQueueAgeMinutes: number;
}

export interface IncomingNachaKpiCount {
  key: string;
  count: number;
}

export interface IncomingNachaClearingCycleKpi {
  clearingHouseId: number;
  achCycleId: string;
  totalItems: number;
  blockedItems: number;
  retryPendingItems: number;
  waitingWindowItems: number;
  failedFinalItems: number;
  confirmedItems: number;
}

export interface IncomingNachaTopError {
  errorCode: string;
  count: number;
  lastSeenAtUtc?: string | null;
}

export interface IncomingNachaTimelinePoint {
  bucketAtUtc: string;
  totalEvents: number;
  manualApplied: number;
  manualRejected: number;
  retryPendingTransitions: number;
  failedFinalTransitions: number;
}

export interface IncomingNachaObservabilitySummary {
  generatedAtUtc: string;
  windowHours: number;
  pipelineHealth: IncomingNachaPipelineHealth;
  ingestionsByStatus: IncomingNachaKpiCount[];
  queueByStatus: IncomingNachaKpiCount[];
  byClearingHouseCycle: IncomingNachaClearingCycleKpi[];
  topErrors: IncomingNachaTopError[];
  timeline: IncomingNachaTimelinePoint[];
}
