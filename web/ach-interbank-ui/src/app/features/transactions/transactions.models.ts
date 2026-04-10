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
  reference: string;
  type: TransactionTypeEnum;
  accountType: AccountTypeEnum;
  isPrenotification: boolean;
  transactionCode: string;
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
    returnReasonCode?: string;
    originalTraceSequence?: string;
  }>;
}

export interface TransactionResponse {
  id: number;
  amount: number;
  reference: string;
  type: TransactionTypeEnum;
  traceNumber: string;
  createdAt: string;
}

export interface TransactionListItem {
  id: number;
  amount: number;
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
  reference: string;
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
