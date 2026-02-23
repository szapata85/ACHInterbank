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
  requiresIdentityValidation?: boolean;
  companyName: string;
  companyIdentification: string;
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
