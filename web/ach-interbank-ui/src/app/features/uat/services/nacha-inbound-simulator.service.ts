import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';

export type NachaSimulationMode = 'IncomingTransactions' | 'DifferentialResponses';

export type NachaInboundSimulationType =
  | 'IncomingCredit'
  | 'IncomingDebit'
  | 'IncomingPrenotificationResponse'
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

@Injectable({ providedIn: 'root' })
export class NachaInboundSimulatorService {
  private readonly api = inject(ApiService);
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

  availableCycles(params: {
    clearingHouseCode: string;
    processingDate: string;
    scenarioType: NachaInboundSimulationType;
  }) {
    return this.api.get<AvailableInboundCycle[]>(`${this.basePath}/available-cycles`, { params });
  }

  downloadUrl(id: number): string {
    return this.api.resolveUrl(`${this.basePath}/${id}/file`);
  }
}
