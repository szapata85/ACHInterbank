import { PagedResponse } from '../../../core/models/paged-response.model';

export enum ThirdPartyAdministrativeStatus {
  Draft = 1,
  Active = 2,
  Inactive = 3
}

export enum ThirdPartyAccountType {
  Checking = 1,
  Savings = 2,
  ElectronicDeposits = 3
}

export enum ThirdPartyTransactionNature {
  Credit = 1,
  Debit = 2
}

export enum ThirdPartyAccountValidationStatus {
  NotStarted = 1,
  PendingSend = 2,
  PendingResponse = 3,
  Accepted = 4,
  TacitlyAccepted = 5,
  Rejected = 6,
  RequiresCorrection = 7,
  TechnicalReview = 8,
  Expired = 9,
  Suspended = 10
}

export enum PrenotificationValidationAttemptStatus {
  Created = 1,
  Queued = 2,
  Sent = 3,
  AwaitingResponse = 4,
  Accepted = 5,
  TacitlyAccepted = 6,
  Rejected = 7,
  RequiresCorrection = 8,
  TechnicalReview = 9,
  Failed = 10,
  Cancelled = 11
}

export enum PrenotificationValidationResolution {
  None = 0,
  Accepted = 1,
  TacitlyAccepted = 2,
  Rejected = 3,
  RequiresCorrection = 4,
  TechnicalReview = 5
}

export interface ThirdPartyAdministrationDto {
  thirdPartyId: number;
  customerId: number;
  personType: string;
  documentType: string;
  documentNumberMasked: string;
  displayName: string;
  status: ThirdPartyAdministrativeStatus;
  accountCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ThirdPartyAccountValidationReadDto {
  validationId: number;
  clearingHouseId: number;
  clearingHouseCode: string;
  clearingHouseName: string;
  transactionNature: ThirdPartyTransactionNature;
  serviceType: string;
  validationType: number;
  status: ThirdPartyAccountValidationStatus;
  policyRuleId?: number | null;
  policyEffectiveFrom?: string | null;
  effectiveDate: string;
  expiresAtUtc?: string | null;
  lastMonetaryUseAtUtc?: string | null;
  latestAttempt?: PrenotificationValidationAttemptReadDto | null;
}

export interface PrenotificationValidationAttemptReadDto {
  attemptId: number;
  attemptNumber: number;
  achTransactionId?: number | null;
  status: PrenotificationValidationAttemptStatus;
  resolution: PrenotificationValidationResolution;
  sentAtUtc: string;
  responseDeadlineAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  returnCode?: string | null;
  sourceCycleId?: string | null;
}

export interface ThirdPartyAccountReadDto {
  thirdPartyAccountId: number;
  thirdPartyId: number;
  customerId: number;
  customerDisplayName: string;
  thirdPartyDisplayName: string;
  personType: string;
  documentType: string;
  documentNumberMasked: string;
  thirdPartyStatus: ThirdPartyAdministrativeStatus;
  financialInstitutionId: number;
  financialInstitutionName: string;
  accountType: ThirdPartyAccountType;
  accountNumberMasked: string;
  accountStatus: ThirdPartyAdministrativeStatus;
  originSource?: number | null;
  validations: ThirdPartyAccountValidationReadDto[];
}

export interface ThirdPartyAccountValidationHistoryDto {
  validationId: number;
  thirdPartyAccountId: number;
  clearingHouseId: number;
  clearingHouseCode: string;
  clearingHouseName: string;
  transactionNature: ThirdPartyTransactionNature;
  serviceType: string;
  validationType: number;
  status: ThirdPartyAccountValidationStatus;
  policyRuleId?: number | null;
  policyEffectiveFrom?: string | null;
  effectiveDate: string;
  expiresAtUtc?: string | null;
  lastMonetaryUseAtUtc?: string | null;
  attempts: PrenotificationValidationAttemptHistoryDto[];
}

export interface PrenotificationValidationAttemptHistoryDto {
  attemptId: number;
  attemptNumber: number;
  achTransactionId?: number | null;
  status: PrenotificationValidationAttemptStatus;
  resolution: PrenotificationValidationResolution;
  sentAtUtc: string;
  responseDeadlineAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  returnCode?: string | null;
  sourceCycleId?: string | null;
}

export interface CreateThirdPartyCommand {
  documentType: string;
  documentNumber: string;
  displayName: string;
  personType: string;
}

export interface UpdateThirdPartyCommand {
  displayName: string;
}

export interface CreateThirdPartyAccountCommand {
  financialInstitutionId: number;
  accountType: ThirdPartyAccountType;
  accountNumber: string;
}

export interface UpdateThirdPartyAccountCommand {
  status: ThirdPartyAdministrativeStatus;
}

export interface SetThirdPartyAdministrativeStatusCommand {
  status: ThirdPartyAdministrativeStatus;
}

export interface RequestThirdPartyAccountRevalidationCommand {
  clearingHouseId: number;
  transactionNature: ThirdPartyTransactionNature;
  effectiveDate: string;
  serviceType: string;
  companyEntryDescriptionId: number;
  sourceAccountNumber: string;
  idempotencyKey: string;
}

export interface ThirdPartyAccountRevalidationResult {
  achTransactionId: number;
  validationId: number;
  attemptId: number;
  attemptNumber: number;
  validationStatus: ThirdPartyAccountValidationStatus;
  attemptStatus: PrenotificationValidationAttemptStatus;
  responseDeadlineAtUtc?: string | null;
  reusedExistingAttempt: boolean;
}

export interface ThirdPartyAdministrationQuery {
  search?: string;
  status?: ThirdPartyAdministrativeStatus | null;
  page?: number;
  pageSize?: number;
}

export interface ThirdPartyAccountQuery {
  status?: ThirdPartyAdministrativeStatus | null;
  page?: number;
  pageSize?: number;
}

export type ThirdPartyPage = PagedResponse<ThirdPartyAdministrationDto>;
export type ThirdPartyAccountPage = PagedResponse<ThirdPartyAccountReadDto>;
