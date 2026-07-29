import { TransactionTypeEnum } from '../transactions/transactions.types';

export type PrenotificationMode = 'Mandatory' | 'Optional' | 'NotApplicable';

export interface TransactionPolicy {
  id: number;
  clearingHouseId: number;
  clearingHouseName: string;
  transactionType: TransactionTypeEnum;
  prenotificationMode: PrenotificationMode;
  prenotificationLeadBusinessDays: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  normativeSource: string;
  normativeReference: string;
  notes: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTransactionPolicyVersionRequest {
  transactionType: TransactionTypeEnum;
  prenotificationMode: PrenotificationMode;
  prenotificationLeadBusinessDays: number | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  normativeSource: string;
  normativeReference: string;
  notes: string | null;
}

export interface UpdateTransactionPolicyMetadataRequest {
  normativeSource: string;
  normativeReference: string;
  notes: string | null;
}

export interface TransactionPolicyPreview {
  ruleConfigured: boolean;
  requiresPrenotification: boolean;
  prenotificationMode: PrenotificationMode;
  normativeSource: string | null;
  normativeReference: string | null;
  decision: string;
  message: string;
}
