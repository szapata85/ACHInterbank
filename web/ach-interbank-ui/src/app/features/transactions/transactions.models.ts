import { TransactionTypeEnum } from './transactions.types';

export interface DestinationInstitution {
  id: number;
  name: string;
  routingNumber: string;
  status: number;
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
