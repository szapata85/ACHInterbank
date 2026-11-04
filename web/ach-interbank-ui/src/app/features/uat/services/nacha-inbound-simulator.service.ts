import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../../core/services/api.service';

export type NachaSimulationMode = 'IncomingTransactions' | 'DifferentialResponses';

export type NachaInboundSimulationType =
  | 'IncomingCredit'
  | 'IncomingDebit'
  | 'IncomingPrenotificationResponse'
  | 'IncomingPrenotificationReturn'
  | 'IncomingCreditConfirmation'
  | 'IncomingCreditRejection'
  | 'IncomingCreditReturn'
  | 'IncomingDebitConfirmation'
  | 'IncomingDebitRejection'
  | 'IncomingDebitReturn';

export type InboundResponseMode = 'Approved' | 'Rejected' | 'Confirmed' | 'Returned' | 'Failed';

export interface GenerateNachaInboundSimulationRequest {
  simulationMode: NachaSimulationMode;
  clearingHouseCode: string;
  scenarioType: NachaInboundSimulationType;
  originFinancialInstitutionId: number;
  originFinancialInstitutionCode?: string;
  entriesCount: number;
  amount: number;
  referencePrefix: string;
  businessDate: string;
  cycleCode: string;
  pendingPrenotificationReferences: string[];
  transactionReferences: string[];
  responseMode?: InboundResponseMode | null;
  reasonCode?: string | null;
  notes?: string | null;
}

export interface AchReturnCodeOption {
  id: number;
  clearingHouseId: number;
  code: string;
  flowType: string;
  description: string;
  appliesToDebit: boolean;
  appliesToCredit: boolean;
  appliesToPrenotification: boolean;
  appliesToReturn: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
}

export interface DifferentialResponseEligibleTransaction {
  id: number;
  identifier: string;
  traceNumber: string;
  clearingHouse: string;
  destinationFinancialInstitutionId: number;
  destinationFinancialInstitution: string;
  transactionType: string;
  effectiveDate: string;
  cycle: string;
  accountNumberMasked: string;
  amount: number;
  state: string;
  hasPriorResponse: boolean;
  eligible: boolean;
  ineligibilityReason?: string | null;
}

export interface DifferentialResponseTransactionPage {
  items: DifferentialResponseEligibleTransaction[];
  page: number;
  pageSize: number;
  total: number;
}

export interface AvailableInboundCycle {
  cycleId: string;
  cycleCode: string;
  cycleName: string;
  clearingHouseId: number;
  clearingHouseCode: string;
  clearingHouseName: string;
  processingDate: string;
  transactionCount: number;
  status: string;
}

export interface InboundSimulationEligibilityPreview {
  eligible: boolean;
  decision: string;
  message: string;
  functionalCode?: string | null;
  simulationMode: NachaSimulationMode;
}

export interface NachaInboundSimulationResult {
  id: number;
  simulationId: string;
  fileName: string;
  downloadUrl: string;
  evidenceUrl: string;
  sha256: string;
  fileSizeBytes: number;
  generatedOnly: boolean;
  autoImported: boolean;
  uploadRequired: boolean;
  externalTransmission: boolean;
  message: string;
}

export interface NachaInboundSimulationItem {
  id: number;
  simulationId: string;
  clearingHouseName: string;
  scenarioType: string;
  responseMode?: string | null;
  reasonCode?: string | null;
  originFinancialInstitution: string;
  destinationFinancialInstitution: string;
  originFinancialInstitutionId: number;
  destinationFinancialInstitutionId: number;
  fileName: string;
  sha256: string;
  fileSizeBytes: number;
  generatedOnly: boolean;
  autoImported: boolean;
  uploadRequired: boolean;
  externalTransmission: boolean;
  createdAt: string;
}

export type PrenotificationOutcome = 'Returned' | 'NoReturnByDeadline';

export interface PrenotificationViolation {
  code: string;
  field: string;
  message: string;
}

export interface PrenotificationCandidate {
  originalPrenotificationId: number;
  originalCycleId: string;
  clearingHouseCode: string;
  respondingFinancialInstitutionId: number;
  respondingFinancialInstitution: string;
  traceNumber: string;
  transactionExternalId: string;
  destinationAccountMasked: string;
  originalBusinessDate: string;
  transactionStatus: string;
  validationAttemptId: number | null;
  validationAttemptStatus: string | null;
  responseDeadlineAtUtc: string | null;
  transmittedOrAcknowledged: boolean;
  hasPriorResponse: boolean;
  eligibleForReturn: boolean;
  eligibleForNoReturnByDeadline: boolean;
  violations: PrenotificationViolation[];
}

export interface PrenotificationCandidatePage {
  items: PrenotificationCandidate[];
  page: number;
  pageSize: number;
  total: number;
  executionEnabled: boolean;
}

export interface PrenotificationContractRequest {
  outcome: PrenotificationOutcome;
  clearingHouseCode: string;
  originalPrenotificationIds: number[];
  originalCycleId: string;
  responseCycleId: string | null;
  businessDate: string;
  respondingFinancialInstitutionId: number;
  reasonCode: string | null;
  idempotencyKey: string | null;
}

export interface PrenotificationEligibilityPreview {
  shapeValid: boolean;
  businessEligible: boolean;
  executionEnabled: boolean;
  contractVersion: string;
  violations: PrenotificationViolation[];
  candidates: PrenotificationCandidate[];
}

export interface PrenotificationTacitPreview {
  evaluationTimeUtc: string;
  realTimeUtc: string;
  isVirtualTime: boolean;
  readOnly: boolean;
  executionEnabled: boolean;
  items: Array<{
    originalPrenotificationId: number;
    deadlineAtUtc: string | null;
    wouldApplyAtEvaluationTime: boolean;
    eligibleAtRealTime: boolean;
    decision: string;
    violations: PrenotificationViolation[];
  }>;
  violations: PrenotificationViolation[];
}

export interface PrenotificationTacitApplyResult {
  realTimeUtc: string;
  applied: number;
  alreadyApplied: number;
  items: PrenotificationTacitPreview['items'];
  createsNachaFile: boolean;
  externalTransmission: boolean;
}

@Injectable({ providedIn: 'root' })
export class NachaInboundSimulatorService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);
  private readonly basePath = 'api/uat/nacha-inbound-simulator';

  list() {
    return this.api.get<NachaInboundSimulationItem[]>(this.basePath);
  }

  preview(payload: GenerateNachaInboundSimulationRequest) {
    return this.api.post<InboundSimulationEligibilityPreview>(`${this.basePath}/eligibility-preview`, payload);
  }

  generate(payload: GenerateNachaInboundSimulationRequest) {
    return this.api.post<NachaInboundSimulationResult>(`${this.basePath}/generate`, payload);
  }

  eligibleDifferentialTransactions(params: {
    clearingHouseCode: string;
    destinationFinancialInstitutionId?: number;
    fromDate?: string;
    toDate?: string;
    state?: string;
    transactionType?: string;
    traceNumber?: string;
    search?: string;
    page: number;
    pageSize: number;
  }) {
    const query = Object.fromEntries(
      Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== '')
    ) as Record<string, string | number>;
    return this.api.get<DifferentialResponseTransactionPage>(
      `${this.basePath}/eligible-differential-transactions`,
      { params: query }
    );
  }

  returnCodes(clearingHouseCode: string) {
    return this.api.get<AchReturnCodeOption[]>('api/regulatory-catalogs/return-codes', {
      params: { clearingHouseCode }
    });
  }

  availableCycles(params: {
    clearingHouseCode: string;
    processingDate: string;
    scenarioType: NachaInboundSimulationType;
  }) {
    return this.api.get<AvailableInboundCycle[]>(`${this.basePath}/available-cycles`, { params });
  }

  listOriginatedPrenotifications(query: {
    clearingHouseCode: string;
    search?: string;
    page: number;
    pageSize: number;
  }) {
    const params = Object.fromEntries(
      Object.entries(query).filter(([, value]) => value !== undefined && value !== null && value !== '')
    ) as Record<string, string | number>;
    return this.api.get<PrenotificationCandidatePage>(`${this.basePath}/prenotifications/originated`, { params });
  }

  previewPrenotificationReturn(payload: PrenotificationContractRequest) {
    return this.api.post<PrenotificationEligibilityPreview>(
      `${this.basePath}/prenotifications/eligibility-preview`, payload);
  }

  generatePrenotificationReturn(payload: PrenotificationContractRequest) {
    return this.api.post<NachaInboundSimulationResult>(
      `${this.basePath}/prenotifications/generate-return`, payload);
  }

  previewTacitAcceptance(payload: PrenotificationContractRequest, evaluationTimeUtc: string | null) {
    return this.api.post<PrenotificationTacitPreview>(
      `${this.basePath}/prenotifications/tacit-acceptance/preview`,
      { contract: payload, evaluationTimeUtc });
  }

  applyDueTacitAcceptance(payload: PrenotificationContractRequest) {
    return this.api.post<PrenotificationTacitApplyResult>(
      `${this.basePath}/prenotifications/tacit-acceptance/apply-due`, payload);
  }

  // Descarga autenticada: HttpClient atraviesa los interceptores de autorización.
  downloadFile(id: number) {
    return this.http.get(this.api.resolveUrl(`${this.basePath}/${id}/file`), { responseType: 'blob' });
  }

  downloadUrl(id: number): string {
    return this.api.resolveUrl(`${this.basePath}/${id}/file`);
  }
}
