import { AccountTypeEnum, FinancialInstitutionStatusEnum, TransactionTypeEnum } from './transactions.types';

export interface DestinationInstitution {
  id: number;
  name: string;
  routingNumber: string;
  transitCode: string;
  checkDigit: string;
  isDefaultSource: boolean;
  status: FinancialInstitutionStatusEnum;
}

export interface TransactionDraft {
  amount: number;
  transactionExternalId: string;
  type: TransactionTypeEnum;
  accountType: AccountTypeEnum;
  isPrenotification: boolean;
  destinationInstitutionId: number;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  recipientIdNumber?: string;
  recipientName?: string;
  requiresIdentityValidation?: boolean;
  companyName: string;
  companyIdentification: string;
  sourcePersonType?: 'PN' | 'PJ';
  recipientPersonType?: 'PN' | 'PJ';
  companyEntryDescriptionId: number;
  addendas: Array<{
    addendaType: string;
    information: string;
    collectorId?: string;
    receiverCustomerCode?: string;
    serviceDescription?: string;
    returnReasonCode?: string;
    originalTraceSequence?: string;
  }>;
}

export interface TransactionResponse {
  id: number;
  amount: number;
  transactionExternalId: string;
  reference: string;
  type: TransactionTypeEnum;
  traceNumber: string;
  createdAt: string;
}

export interface TransactionListItem {
  id: number;
  amount: number;
  transactionExternalId: string;
  reference: string;
  type: TransactionTypeEnum;
  traceNumber: string;
  effectiveEntryDate: string;
  createdAt: string;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  sourceInstitutionName: string;
  destinationInstitutionName: string;
  isPrenotification: boolean;
  transactionCode: string;
  achBatchId: number;
  batchSequenceNumber: number;
  batchCompanyName: string;
  batchEffectiveEntryDate: string;
  achCycleId: string;
  achCycleName: string;
  clearingHouseName: string;
}

export interface TransactionListFilter {
  achCycleId?: string | null;
  achCycleName?: string | null;
  effectiveDate?: string;
  clearingHouseId?: number | null;
}

export type IntegrationBusinessStatus = 'Success' | 'Rejected' | 'PendingCatalog' | 'ManualReview' | 'Unknown';

export interface TransactionIntegrationResultItem {
  catalogId?: number | null;
  method: string;
  transportStatus: string;
  businessStatus: IntegrationBusinessStatus;
  responseCode: string;
  responseDescription: string;
  processedAt?: string | null;
  attemptNumber: number;
  retryAllowed: boolean;
  requiresManualReview: boolean;
  transactionState: string;
}

export interface TransactionIntegrationResult {
  transactionId: number;
  latest?: TransactionIntegrationResultItem | null;
  history: TransactionIntegrationResultItem[];
}

export interface ReturnReason {
  id: number;
  code: string;
  description: string;
  category: string;
  isForReturn: boolean;
}


export interface ActiveThirdPartyAccount {
  id: number;
  destinationInstitutionId: number;
  destinationInstitutionName: string;
  destinationAccountNumber: string;
  recipientIdNumber: string;
}


export interface CompanyEntryDescriptionOption {
  id: number;
  term: string;
  description: string;
  standardEntryClassCode: string;
}


export interface ReturnEligibleTransaction {
  id: number;
  traceNumber: string;
  amount: number;
  transactionCode: string;
  reference: string;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  originatingDfi: string;
  receivingDfi: string;
  achCycleId: string;
  effectiveEntryDate: string;
  isPrenotification: boolean;
  isEligible: boolean;
  validationMessage?: string | null;
}

export interface ReturnSelectionItem {
  transactionId: number;
  returnReasonCode: string;
}

export interface GenerateReturnsFileRequest {
  cycleId: string;
  items: ReturnSelectionItem[];
}

export interface EvaluateReturnOfReturnRequest {
  sourceReturnTransactionId: number;
  newReturnReasonCode: string;
  requestedBy?: string;
  source?: string;
}

export interface AchReturnOfReturnEligibilityFailure {
  code: string;
  message: string;
  field?: string | null;
}

export interface AchReturnOfReturnEligibilityResult {
  isEligible: boolean;
  clearingHouseId?: number | null;
  sourceReturnTransactionId?: number | null;
  originalReturnReasonCode?: string | null;
  newReturnReasonCode?: string | null;
  isUniquePerTransaction: boolean;
  failures: AchReturnOfReturnEligibilityFailure[];
}

export interface GenerateReturnOfReturnAuditFileRequest {
  flowIds: number[];
  requestedBy?: string;
  source?: string;
}

export interface GenerateReturnOfReturnNachaFileRequest {
  flowIds: number[];
  cycleId?: string | null;
  valueDate?: string | null;
  requestedBy?: string;
  source?: string;
}

export interface TransactionPolicyPreview {
  canSubmit: boolean;
  message?: string | null;
  cycleId?: string | null;
  cycleName?: string | null;
  processingDate?: string | null;
  clearingHouseName?: string | null;
  clearingHouseId?: number | null;
  windowLabel?: string | null;
  isWithinProcessingWindow: boolean;
  maxAmountPerTransaction?: number | null;
  remainingAmountForCycle?: number | null;
  remainingTransactionsForCycle?: number | null;
  idempotencyKey?: string | null;
  wouldDuplicate: boolean;
}


export interface BulkAchTransactionItemRequest {
  amount: number;
  transactionExternalId?: string;
  reference?: string;
  type: TransactionTypeEnum;
  accountType: AccountTypeEnum;
  isPrenotification: boolean;
  destinationInstitutionId: number;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  companyName: string;
  companyIdentification: string;
  companyEntryDescriptionId: number;
  sourcePersonType?: 'PN' | 'PJ';
  recipientPersonType?: 'PN' | 'PJ';
  recipientIdNumber?: string;
  recipientName?: string;
  requiresIdentityValidation?: boolean;
  addendas?: Array<{
    addendaType: string;
    information: string;
  }>;
}

export interface BulkAchTransactionRequest {
  batchReference: string;
  chunkSize?: number;
  transactions: BulkAchTransactionItemRequest[];
}

export interface BulkAchTransactionItemResult {
  index: number;
  transactionExternalId: string;
  reference: string;
  succeeded: boolean;
  transactionId?: number;
  errorCode?: string;
  errorMessage?: string;
}

export interface BulkAchTransactionResponse {
  batchReference: string;
  totalReceived: number;
  totalProcessed: number;
  totalSucceeded: number;
  totalFailed: number;
  createdTransactionIds: number[];
  itemResults: BulkAchTransactionItemResult[];
}


export enum BulkIngestionSourceType {
  InlineTransactions = 1,
  JsonFile = 2,
  CsvFile = 3,
  ExcelFile = 4
}

export enum BulkIngestionProcessingMode {
  Synchronous = 1,
  AsynchronousJob = 2
}

export interface BulkIngestionRequest {
  batchReference: string;
  sourceType: BulkIngestionSourceType;
  processingMode: BulkIngestionProcessingMode;
  chunkSize?: number;
  transactions?: BulkAchTransactionItemRequest[];
  fileName?: string;
  contentType?: string;
  contentBase64?: string;
  clientRequestId?: string;
  retryCount?: number;
}

export interface BulkIngestionResponse {
  processingMode: BulkIngestionProcessingMode;
  jobId?: string;
  status?: string;
  immediateResult?: BulkAchTransactionResponse;
}


export enum BulkIngestionBatchStatus {
  Uploaded = 1,
  Parsed = 2,
  Validated = 3,
  Queued = 4,
  Processing = 5,
  PartiallyProcessed = 6,
  Completed = 7,
  Failed = 8,
  Retrying = 9,
  Cancelled = 10
}

export enum BulkIngestionItemStatus {
  Ready = 1,
  StructuralError = 2,
  ProcessingError = 3,
  Processed = 4
}

export enum BulkIngestionRetryScope {
  Full = 1,
  FailedOnly = 2
}

export interface BulkFileUploadResponse {
  batchId: string;
  batchReference: string;
  status: BulkIngestionBatchStatus;
  fileType: number;
  totalRecords: number;
  totalValid: number;
  totalInvalid: number;
  uploadedAtUtc: string;
  message: string;
}

export interface BulkBatchStatusDto {
  batchId: string;
  batchReference: string;
  status: BulkIngestionBatchStatus;
  totalRecords: number;
  totalValid: number;
  totalInvalid: number;
  totalProcessed: number;
  totalSucceeded: number;
  totalFailed: number;
  progressPercent: number;
  uploadedAtUtc: string;
  processingStartedAtUtc?: string | null;
  processingFinishedAtUtc?: string | null;
  retryCount: number;
  lastJobId?: string | null;
  lastJobMessage: string;
  errorSummary: string[];
}

export interface BulkBatchItemDto {
  itemId: number;
  itemIndex: number;
  reference: string;
  status: BulkIngestionItemStatus;
  message: string;
  transactionId?: number | null;
}

export interface BulkBatchItemsPageDto {
  page: number;
  pageSize: number;
  total: number;
  items: BulkBatchItemDto[];
}

export interface BulkBatchAttemptDto {
  attemptId: number;
  attemptNumber: number;
  triggerType: string;
  scope: string;
  triggeredBy: string;
  triggeredAtUtc: string;
  status: string;
  jobId?: string | null;
  startedAtUtc?: string | null;
  finishedAtUtc?: string | null;
  totalProcessed: number;
  totalSucceeded: number;
  totalFailed: number;
  resultMessage: string;
}

export interface BulkBatchProcessingSummaryDto {
  batchId: string;
  status: BulkBatchStatusDto;
  attempts: BulkBatchAttemptDto[];
}

export interface RetryBatchRequest {
  scope: BulkIngestionRetryScope;
}

export interface RetryBatchResponse {
  batchId: string;
  attemptId: number;
  attemptNumber: number;
  jobId: string;
  status: BulkIngestionBatchStatus;
}


export interface BulkBatchProgressEvent {
  batchId: string;
  progressPercent: number;
  message?: string | null;
  status?: BulkIngestionBatchStatus;
  updatedAtUtc?: string;
}

export interface CancelBatchResponse {
  batchId: string;
  cancelled: boolean;
  message: string;
}

export type CycleValidityFilter = 'all' | 'current' | 'future' | 'expired';
export type CycleStatusFilter = 'all' | 'active' | 'inactive';

export interface ClearingHouseCycleConfigItem {
  id: number;
  clearingHouseId: number;
  clearingHouseName?: string | null;
  cycleName: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isCurrent: boolean;
  updatedAt?: string | null;
}

export interface ClearingHouseCycleConfigFilters {
  clearingHouseId: number;
  effectiveAt?: string | null;
}

export interface UpsertCycleConfigRequest {
  clearingHouseId: number;
  cycleName: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  effectiveFrom: string;
}

export interface InactivateCycleConfigRequest {
  effectiveTo: string;
}

export type TransactionNature = 'Credit' | 'Debit';
export type PrenotificationRequirementMode = 'Mandatory' | 'Optional' | 'NotApplicable';
export type ValidationRequirementMode = 'Mandatory' | 'Optional' | 'NotApplicable';

export interface ClearingHouseTransactionRuleItem {
  id: number;
  clearingHouseId: number;
  clearingHouseName: string;
  transactionNature: TransactionNature;
  transactionType: TransactionTypeEnum;
  requiresPrenotification: boolean;
  prenotificationMode: PrenotificationRequirementMode;
  requiresReceiverIdentificationValidation: boolean;
  receiverIdentificationValidationMode: ValidationRequirementMode;
  appliesToNachaExport: boolean;
  appliesToMonetaryTransactions: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  normativeSource: string;
  normativeReference: string;
  notes: string;
  createdAt: string;
  updatedAt: string;
}

export interface SaveClearingHouseTransactionRuleRequest {
  clearingHouseId: number;
  transactionNature: TransactionNature;
  transactionType: TransactionTypeEnum;
  requiresPrenotification: boolean;
  prenotificationMode: PrenotificationRequirementMode;
  requiresReceiverIdentificationValidation: boolean;
  receiverIdentificationValidationMode: ValidationRequirementMode;
  appliesToNachaExport: boolean;
  appliesToMonetaryTransactions: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  normativeSource: string;
  normativeReference: string;
  notes?: string | null;
}

export interface TransactionPrerequisitePreviewRequest {
  clearingHouseId: number;
  transactionType: TransactionTypeEnum;
  effectiveEntryDate: string;
  appliesToNachaExport: boolean;
}

export interface TransactionPrerequisitePreviewResponse {
  ruleConfigured: boolean;
  requiresPrenotification: boolean;
  prenotificationMode: PrenotificationRequirementMode;
  requiresReceiverIdentificationValidation: boolean;
  receiverIdentificationValidationMode: ValidationRequirementMode;
  normativeSource?: string | null;
  normativeReference?: string | null;
  decision: string;
  message: string;
}
