export interface NachaOperationalSummary {
  productiveStatus: string;
  backendPhase: string;
  soapMode: string;
  productiveExecution: boolean;
  wouldInvokeRealSoap: boolean;
  totalFiles: number;
  totalIncomingFiles: number;
  totalOutgoingFiles: number;
  totalReturnFiles: number;
  totalDecisions: number;
  totalSoapCandidates: number;
  totalNoGoBlocks: number;
  totalManualReview: number;
  totalReadinessChecks: number;
  lastUpdatedAt: string;
  isDemoData: boolean;
  warnings: string[];
}

export interface NachaOperationalFile {
  fileId: string;
  fileName: string;
  clearingHouseCode: string;
  profileCode: string;
  flowType: string;
  isReturnFile: boolean;
  validationPassed: boolean;
  batchCount: number;
  entryCount: number;
  addendaCount: number;
  batchControlCount: number;
  fileControlCount: number;
  processingStatus: string;
  receivedAt: string | null;
  createdAt: string;
  correlationId: string;
  hasErrors: boolean;
  warningCount: number;
  errorCount: number;
}

export interface NachaOperationalDecision {
  correlationId: string;
  fileName: string;
  entryTraceNumber: string;
  originalTraceNumber: string | null;
  decisionType: string;
  soapOperationCandidate: string;
  requiresMonetaryMovement: boolean;
  reasonCode: string;
  reasonDescription: string;
  newInternalStatus: string;
  manualReviewRequired: boolean;
  isBlocked: boolean;
  blockReason: string | null;
  createdAt: string;
}

export interface NachaSoapReadiness {
  correlationId: string;
  operationCandidate: string;
  isReadyForUat: boolean;
  isBlocked: boolean;
  blockReasons: string[];
  payloadMappingPassed: boolean;
  requestMappingPassed: boolean;
  operationalGatePassed: boolean;
  readinessCheckPassed: boolean;
  simulationPassed: boolean;
  resiliencePassed: boolean;
  requiresMonetaryMovement: boolean;
  phase: string;
  lastCheckedAt: string;
  productiveExecution: boolean;
  wouldInvokeRealSoap: boolean;
}

export interface NachaOperationalAudit {
  correlationId: string;
  phase: string;
  eventType: string;
  severity: string;
  message: string;
  isBlocked: boolean;
  timestamp: string;
  sanitizedDetails: Record<string, string>;
}

export interface NachaOperationalDashboardData {
  summary: NachaOperationalSummary;
  files: NachaOperationalFile[];
  decisions: NachaOperationalDecision[];
  readiness: NachaSoapReadiness[];
  audit: NachaOperationalAudit[];
  generatedAt: string;
  isDemoData: boolean;
  productiveStatus: string;
}
