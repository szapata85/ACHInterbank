import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import {
  NachaOperationalAudit,
  NachaOperationalDashboardData,
  NachaOperationalDecision,
  NachaOperationalFile,
  NachaOperationalSummary,
  NachaSoapReadiness
} from '../models/nacha-operational.models';

@Injectable({ providedIn: 'root' })
export class NachaOperationalReadinessService {
  getOperationalSummary(): Observable<NachaOperationalSummary> {
    return of(DEMO_DATA.summary);
  }

  getFiles(): Observable<NachaOperationalFile[]> {
    return of(DEMO_DATA.files);
  }

  getDecisions(): Observable<NachaOperationalDecision[]> {
    return of(DEMO_DATA.decisions);
  }

  getSoapReadiness(): Observable<NachaSoapReadiness[]> {
    return of(DEMO_DATA.readiness);
  }

  getAudit(): Observable<NachaOperationalAudit[]> {
    return of(DEMO_DATA.audit);
  }

  getDashboardData(): Observable<NachaOperationalDashboardData> {
    return of(DEMO_DATA);
  }
}

const DEMO_DATA: NachaOperationalDashboardData = {
  summary: {
    productiveStatus: 'NO-GO',
    backendPhase: '6B.5.6',
    soapMode: 'Simulated',
    productiveExecution: false,
    wouldInvokeRealSoap: false,
    totalFiles: 6,
    totalDecisions: 6,
    totalSoapCandidates: 4,
    totalNoGoBlocks: 3,
    totalManualReview: 1,
    lastUpdatedAt: '2026-05-24T23:00:00Z',
    isDemoData: true
  },
  files: [
    {
      fileName: 'ACH_COL_IN_001.ach',
      clearingHouseCode: 'ACH',
      profileCode: 'OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0',
      flowType: 'IncomingCreditFromExternalOriginator',
      isReturnFile: false,
      validationPassed: true,
      batchCount: 1,
      entryCount: 2,
      addendaCount: 1,
      processingStatus: 'Processed',
      receivedAt: '2026-05-24T14:30:00Z'
    },
    {
      fileName: 'CENIT_IN_001.ach',
      clearingHouseCode: 'CENIT',
      profileCode: 'OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0',
      flowType: 'DifferentialResponse',
      isReturnFile: false,
      validationPassed: true,
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      processingStatus: 'Processed',
      receivedAt: '2026-05-24T15:10:00Z'
    },
    {
      fileName: 'ACH_COL_RET_001.RET',
      clearingHouseCode: 'ACH',
      profileCode: 'OFFICIAL_ACH_DEVOLUCION_V1_0',
      flowType: 'ReturnFile',
      isReturnFile: true,
      validationPassed: true,
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      processingStatus: 'ManualReviewRequired',
      receivedAt: '2026-05-24T16:40:00Z'
    }
  ],
  decisions: [
    {
      correlationId: 'phase-6b5-uat-orch',
      decisionType: 'ApplyCreditMovement',
      soapOperationCandidate: 'ProcTransacciones',
      requiresMonetaryMovement: true,
      reasonCode: '00',
      reasonDescription: 'Simulacion UAT aprobada',
      newInternalStatus: 'Accepted',
      manualReviewRequired: false
    },
    {
      correlationId: 'phase-6b5-response',
      decisionType: 'RegisterDifferentialResponse',
      soapOperationCandidate: 'RegistrarRespuestaTransaccion',
      requiresMonetaryMovement: false,
      reasonCode: 'R01',
      reasonDescription: 'Respuesta diferencial sin movimiento monetario',
      newInternalStatus: 'Rejected',
      manualReviewRequired: false
    },
    {
      correlationId: 'phase-6b5-manual',
      decisionType: 'ManualReviewRequired',
      soapOperationCandidate: 'None',
      requiresMonetaryMovement: false,
      reasonCode: 'MR',
      reasonDescription: 'Correlacion ambigua',
      newInternalStatus: 'ManualReviewRequired',
      manualReviewRequired: true
    }
  ],
  readiness: [
    {
      correlationId: 'phase-6b5-uat-orch',
      isReadyForUat: true,
      isBlocked: false,
      blockReasons: [],
      operationalGatePassed: true,
      readinessCheckPassed: true,
      simulationPassed: true,
      resiliencePassed: true,
      productiveExecution: false,
      wouldInvokeRealSoap: false
    },
    {
      correlationId: 'phase-6b5-nogo',
      isReadyForUat: false,
      isBlocked: true,
      blockReasons: ['Invocacion SOAP real bloqueada por NO-GO en Fase 6B.5.6.'],
      operationalGatePassed: false,
      readinessCheckPassed: false,
      simulationPassed: false,
      resiliencePassed: false,
      productiveExecution: false,
      wouldInvokeRealSoap: false
    }
  ],
  audit: [
    {
      correlationId: 'phase-6b5-uat-orch',
      phase: '6B.5',
      eventType: 'PayloadMappingCompleted',
      severity: 'Information',
      message: 'Payload SOAP interno mapeado.',
      isBlocked: false,
      timestamp: '2026-05-24T23:00:01Z',
      sanitizedDetails: {
        OperationCandidate: 'ProcTransacciones',
        RequiresMonetaryMovement: 'True',
        Phase: '6B.5'
      }
    },
    {
      correlationId: 'phase-6b5-nogo',
      phase: '6B.5',
      eventType: 'BlockedByNoGo',
      severity: 'Warning',
      message: 'Flujo bloqueado por NO-GO.',
      isBlocked: true,
      timestamp: '2026-05-24T23:00:03Z',
      sanitizedDetails: {
        Productivo: 'NO-GO',
        WouldInvokeRealSoap: 'false',
        Phase: '6B.5'
      }
    }
  ]
};
