export interface NachaOperationalSummary {
  productiveStatus: string;
  backendPhase: string;
  soapMode: string;
  productiveExecution: boolean;
  wouldInvokeRealSoap: boolean;
  totalFiles: number;
  totalDecisions: number;
  totalSoapCandidates: number;
  totalNoGoBlocks: number;
  totalManualReview: number;
  lastUpdatedAt: string;
  isDemoData: boolean;
}

export interface NachaOperationalFile {
  fileName: string;
  clearingHouseCode: string;
  profileCode: string;
  flowType: string;
  isReturnFile: boolean;
  validationPassed: boolean;
  batchCount: number;
  entryCount: number;
  addendaCount: number;
  processingStatus: string;
  receivedAt: string;
}

export interface NachaOperationalDecision {
  correlationId: string;
  decisionType: string;
  soapOperationCandidate: string;
  requiresMonetaryMovement: boolean;
  reasonCode: string;
  reasonDescription: string;
  newInternalStatus: string;
  manualReviewRequired: boolean;
}

export interface NachaSoapReadiness {
  correlationId: string;
  isReadyForUat: boolean;
  isBlocked: boolean;
  blockReasons: string[];
  operationalGatePassed: boolean;
  readinessCheckPassed: boolean;
  simulationPassed: boolean;
  resiliencePassed: boolean;
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
}
