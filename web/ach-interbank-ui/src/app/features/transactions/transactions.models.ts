import { TransactionTypeEnum } from './transactions.types';

export interface DestinationInstitution {
  id: number;
  name: string;
  routingNumber: string;
  status: number;
}

export enum FinancialInstitutionStatus {
  Active = 1,
  Inactive = 2
}

export interface FinancialInstitution {
  id: number;
  name: string;
  isDefaultSource: boolean;
  routingNumber: string;
  transitCode: string;
  checkDigit: string;
  status: FinancialInstitutionStatus | number;
}

export interface CreateTransactionRequest {
  amount: number;
  reference: string;
  type: TransactionTypeEnum;
  destinationInstitutionId: number;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
}

export interface TransactionResponse {
  id: number;
  amount: number;
  reference: string;
  type: TransactionTypeEnum;
  traceNumber: string;
  createdAt: string;
}
