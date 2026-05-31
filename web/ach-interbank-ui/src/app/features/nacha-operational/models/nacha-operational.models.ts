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
  isPartialData?: boolean;
  dataSource?: string;
  warnings: string[];
}

export interface NachaOperationalFile {
  fileId: string;
  fileName: string;
  dataSource?: string;
  headerId?: string | null;
  persistedRecordCount?: number;
  lastParsedAt?: string | null;
  noSensitiveData?: boolean;
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

export interface NachaOperationalFileDetail {
  fileId: string;
  headerId?: string | null;
  fileName: string;
  clearingHouseCode: string;
  profileCode: string;
  flowType: string;
  isReturnFile: boolean;
  processingStatus: string;
  validationPassed: boolean;
  receivedAt: string | null;
  createdAt: string;
  correlationId: string;
  dataSource: string;
  isPartialData: boolean;
  warnings: string[];
  header: NachaOperationalHeader | null;
  batches: NachaOperationalBatchHeader[];
  entries: NachaOperationalEntryDetail[];
  addendas: NachaOperationalAddendaRecord[];
  batchControls: NachaOperationalBatchControl[];
  fileControls: NachaOperationalFileControl[];
  totalsSummary: NachaOperationalTotalsSummary;
  noSensitiveData: boolean;
}

export interface NachaOperationalHeader {
  headerId?: string | null;
  priorityCode?: string | null;
  immediateDestination?: string | null;
  immediateOrigin?: string | null;
  fileCreationDate?: string | null;
  fileCreationTime?: string | null;
  fileIdModifier?: string | null;
  recordSize?: string | null;
  blockingFactor?: string | null;
  formatCode?: string | null;
  referenceCode?: string | null;
  cycleNumber: number;
}

export interface NachaOperationalBatchHeader {
  batchId: number;
  serviceClassCode?: string | null;
  companyName?: string | null;
  standardEntryClassCode?: string | null;
  companyEntryDescription?: string | null;
  effectiveEntryDate?: string | null;
  batchNumber: number;
}

export interface NachaOperationalEntryDetail {
  entryDetailId: number;
  transactionCode?: string | null;
  receivingParticipantEntityCode?: string | null;
  checkDigit?: string | null;
  accountNumberMasked?: string | null;
  amount?: number | null;
  recipIdNumberMasked?: string | null;
  recipUserNameMasked?: string | null;
  addendumIndicator?: string | null;
  sequenceNumberMasked?: string | null;
}

export interface NachaOperationalAddendaRecord {
  addendaId: number;
  codeTypeAddendumRecord?: string | null;
  businessType?: string | null;
  purposeOfTransaction?: string | null;
  invoiceOrAccountNumberMasked?: string | null;
  infoFromOriginator?: string | null;
  returnReasonCode?: string | null;
  originalTraceNumberMasked?: string | null;
  newTraceNumberMasked?: string | null;
  addendumSequence?: string | null;
  entryDetailSequenceNumberMasked?: string | null;
}

export interface NachaOperationalBatchControl {
  batchControlId: number;
  batchTranClassCode?: string | null;
  entryAddendaCount?: number | null;
  entryHash?: number | null;
  totalDebitAmount: number;
  totalCreditAmount: number;
  batchNumber?: string | null;
}

export interface NachaOperationalFileControl {
  fileControlId: number;
  batchCount: number;
  blockCount: number;
  entryAddendaCount: number;
  entryHash: number;
  totalDebitAmount: number;
  totalCreditAmount: number;
}

export interface NachaOperationalTotalsSummary {
  batchCount: number;
  entryCount: number;
  addendaCount: number;
  batchControlCount: number;
  fileControlCount: number;
  persistedRecordCount: number;
  totalDebitAmount: number;
  totalCreditAmount: number;
  validationPassed: boolean;
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
  dataSource?: string;
  isDerived?: boolean;
  isPersisted?: boolean;
  warning?: string | null;
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
  dataSource?: string;
  isDerived?: boolean;
  isPersisted?: boolean;
  warning?: string | null;
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
  dataSource?: string;
  isDerived?: boolean;
  isPersisted?: boolean;
  warning?: string | null;
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
  isPartialData?: boolean;
  dataSource?: string;
  warnings?: string[];
  productiveStatus: string;
}
