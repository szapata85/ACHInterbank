import { FinancialInstitutionStatusEnum, TransactionTypeEnum } from './transactions.types';

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
  destinationInstitutionId: number;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  companyName: string;
  companyIdentification: string;
  companyEntryDescription: string;
  addendas: Array<{
    addendaType: string;
    information: string;
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
