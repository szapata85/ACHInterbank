import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';

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
    return this.api.post<unknown>(`${this.basePath}/eligibility-preview`, payload);
  }

  generate(payload: GenerateNachaInboundSimulationRequest) {
    return this.api.post<NachaInboundSimulationResult>(`${this.basePath}/generate`, payload);
  }

  downloadUrl(id: number): string {
    return this.api.resolveUrl(`${this.basePath}/${id}/file`);
  }
}
