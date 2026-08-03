export interface OutgoingMonitoringQuery {
  fromUtc?: string;
  toUtc?: string;
  clearingHouseId?: number;
  cycleId?: string;
  destinationInstitutionId?: number;
  transactionExternalId?: string;
  traceNumber?: string;
  transactionType?: number;
  processStatus?: string;
  initialResult?: string;
  subsequentSituation?: string;
  hasReturn?: boolean;
  requiresAttention?: boolean;
  minimumAmount?: number;
  maximumAmount?: number;
  pageNumber: number;
  pageSize: 10 | 25 | 50 | 100;
  sortBy: 'createdAt' | 'amount' | 'identifier' | 'lastUpdatedAt';
  sortDirection: 'asc' | 'desc';
}

export interface OutgoingMonitoringOption {
  id: number;
  name: string;
  code?: string;
}

export interface OutgoingMonitoringPage<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface OutgoingMonitoringListItem {
  id: number;
  createdAtUtc: string;
  transactionExternalId: string;
  traceNumber: string;
  clearingHouseCode: string;
  clearingHouseDisplayName: string;
  cycleId: string;
  cycleDisplayName: string;
  destinationInstitutionDisplayName: string;
  transactionTypeCode: string;
  transactionTypeDisplayName: string;
  amount: number;
  maskedDestinationAccount: string;
  processStatusCode: string;
  processStatusDisplayName: string;
  initialResultCode: string;
  initialResultDisplayName: string;
  subsequentSituationCode: string;
  subsequentSituationDisplayName: string;
  hasReturn: boolean;
  returnCode?: string;
  returnDescription?: string;
  fileName?: string;
  fileVersion?: number;
  fileLifecycleStatusCode: string;
  fileLifecycleStatusDisplayName: string;
  lastUpdatedAtUtc: string;
  requiresAttention: boolean;
  attentionReason?: string;
}

export interface OutgoingMonitoringTimelineEvent {
  occurredAtUtc: string;
  stageCode: string;
  stageDisplayName: string;
  title: string;
  description: string;
  outcomeCode: string;
  outcomeDisplayName: string;
  severity: string;
  sourceType: string;
  isTechnical: boolean;
}

export interface OutgoingMonitoringFile {
  fileId: number;
  fileName: string;
  version?: number;
  fileSequence: number;
  includedAtUtc: string;
  lifecycleStatusCode: string;
  lifecycleStatusDisplayName: string;
  hasTransmissionEvidence: boolean;
  transmittedAtUtc?: string;
  hasAcknowledgementEvidence: boolean;
  acknowledgedAtUtc?: string;
  acknowledgementCode?: string;
}

export interface OutgoingMonitoringDetail {
  summary: OutgoingMonitoringListItem;
  classification: {
    directionDisplayName: string;
    originDisplayName: string;
    monetaryRouteDisplayName: string;
    classificationStatusDisplayName: string;
    classifiedAtUtc?: string;
    classificationVersion: number;
  };
  integration: {
    wasDispatched: boolean;
    attemptCount: number;
    resultDisplayName: string;
    responseCode?: string;
    responseDescription?: string;
    lastAttemptAtUtc?: string;
    lastSuccessAtUtc?: string;
  };
  files: OutgoingMonitoringFile[];
  responses: Array<{
    id: string;
    receivedAtUtc: string;
    responseTypeDisplayName: string;
    externalStatusCode: string;
    causeCode?: string;
    causeDescription?: string;
    correlationStatusDisplayName: string;
  }>;
  returns: Array<{
    occurredAtUtc: string;
    stateDisplayName: string;
    causeCode?: string;
    causeDescription?: string;
  }>;
  timeline: OutgoingMonitoringTimelineEvent[];
  warnings: string[];
  technicalDetail?: {
    transactionId: number;
    lastIntegrationMethod?: string;
    lastIntegrationMode?: string;
    lastIntegrationCode?: string;
    lastIntegrationDurationMs?: number;
    lastCorrelationId?: string;
  };
}
