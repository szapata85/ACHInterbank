export interface ClearingHouseCycleConfigItem {
  id: number;
  clearingHouseId: number;
  clearingHouseName?: string | null;
  policyVersion: string;
  cycleName: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  outputReleaseTime: string;
  compensationStatementStartTime?: string | null;
  compensationStatementEndTime?: string | null;
  compensationPaymentStartTime?: string | null;
  compensationPaymentEndTime?: string | null;
  outputReleaseStartTime?: string | null;
  outputReleaseEndTime?: string | null;
  allowsMonetaryCredit: boolean;
  allowsMonetaryDebit: boolean;
  allowsCreditPrenotification: boolean;
  allowsDebitPrenotification: boolean;
  allowsReturn: boolean;
  allowsReturnOfReturn: boolean;
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
  policyVersion?: string | null;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  outputReleaseTime: string;
  compensationStatementStartTime?: string | null;
  compensationStatementEndTime?: string | null;
  compensationPaymentStartTime?: string | null;
  compensationPaymentEndTime?: string | null;
  outputReleaseStartTime?: string | null;
  outputReleaseEndTime?: string | null;
  allowsMonetaryCredit: boolean;
  allowsMonetaryDebit: boolean;
  allowsCreditPrenotification: boolean;
  allowsDebitPrenotification: boolean;
  allowsReturn: boolean;
  allowsReturnOfReturn: boolean;
  effectiveFrom: string;
}

export interface InactivateCycleConfigRequest {
  effectiveTo: string;
}

