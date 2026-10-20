import { AccountTypeEnum, TransactionTypeEnum } from '../../core/models/transaction-types.model';

export interface CfaOperationalTransactionRestriction {
  id: number;
  clearingHouseId?: number | null;
  clearingHouseCode?: string | null;
  clearingHouseName?: string | null;
  clearingHouseCycleConfigId?: number | null;
  policyVersion?: string | null;
  cycleName?: string | null;
  transactionType?: TransactionTypeEnum | null;
  isPrenotification?: boolean | null;
  maxAmountPerTransaction?: number | null;
  maxAmountPerCycle?: number | null;
  maxTransactionsPerCycle?: number | null;
  allowedAccountTypes?: AccountTypeEnum[] | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface CfaOperationalTransactionRestrictionFilter {
  clearingHouseCode?: string | null;
  policyVersion?: string | null;
  clearingHouseCycleConfigId?: number | null;
  transactionType?: TransactionTypeEnum | null;
  isPrenotification?: boolean | null;
  isActive?: boolean | null;
  effectiveAt?: string | null;
}

export interface SaveCfaOperationalTransactionRestrictionRequest {
  clearingHouseCode?: string | null;
  clearingHouseCycleConfigId?: number | null;
  transactionType?: TransactionTypeEnum | null;
  isPrenotification?: boolean | null;
  maxAmountPerTransaction?: number | null;
  maxAmountPerCycle?: number | null;
  maxTransactionsPerCycle?: number | null;
  allowedAccountTypes?: AccountTypeEnum[] | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
}

export interface UpdateCfaOperationalTransactionRestrictionRequest extends SaveCfaOperationalTransactionRestrictionRequest {
  expectedRowVersion: string;
}

export interface ChangeCfaOperationalTransactionRestrictionStatusRequest {
  expectedRowVersion: string;
  effectiveTo?: string | null;
}

export interface CfaOperationalTransactionRestrictionApiError {
  code?: string;
  message?: string;
  currentRowVersion?: string | null;
}
