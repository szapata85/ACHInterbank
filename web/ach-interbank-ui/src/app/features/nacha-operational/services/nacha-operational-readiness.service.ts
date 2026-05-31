import { Injectable } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  NachaOperationalAudit,
  NachaOperationalDashboardData,
  NachaOperationalDecision,
  NachaOperationalFile,
  NachaOperationalFileDetail,
  NachaOperationalSummary,
  NachaSoapReadiness
} from '../models/nacha-operational.models';

@Injectable({ providedIn: 'root' })
export class NachaOperationalReadinessService {
  private readonly basePath = 'api/ach/nacha/operational';

  constructor(private readonly api: ApiService) {}

  getDashboard(): Observable<NachaOperationalDashboardData> {
    return this.getDashboardData();
  }

  getOperationalSummary(): Observable<NachaOperationalSummary> {
    return this.getDashboardData().pipe(map((dashboard) => dashboard.summary));
  }

  getFiles(): Observable<NachaOperationalFile[]> {
    return this.getDashboardData().pipe(map((dashboard) => dashboard.files));
  }

  getFileDetail(fileId: string): Observable<NachaOperationalFileDetail> {
    const safeFileId = encodeURIComponent(fileId);
    return this.api.get<NachaOperationalFileDetail>(`${this.basePath}/files/${safeFileId}`).pipe(
      catchError((error) => {
        const status = error?.status;
        const message = status === 404
          ? 'Archivo NACHA-M no encontrado o no persistido.'
          : 'No fue posible cargar el detalle operativo NACHA-M.';
        return throwError(() => new Error(message));
      })
    );
  }

  getDecisions(): Observable<NachaOperationalDecision[]> {
    return this.getDashboardData().pipe(map((dashboard) => dashboard.decisions));
  }

  getSoapReadiness(): Observable<NachaSoapReadiness[]> {
    return this.getDashboardData().pipe(map((dashboard) => dashboard.readiness));
  }

  getAudit(): Observable<NachaOperationalAudit[]> {
    return this.getDashboardData().pipe(map((dashboard) => dashboard.audit));
  }

  getDashboardData(): Observable<NachaOperationalDashboardData> {
    return this.api.get<NachaOperationalDashboardData>(`${this.basePath}/dashboard`).pipe(
      catchError(() => of(DEMO_DATA)),
      shareReplay({ bufferSize: 1, refCount: true })
    );
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
    totalIncomingFiles: 2,
    totalOutgoingFiles: 2,
    totalReturnFiles: 2,
    totalDecisions: 6,
    totalSoapCandidates: 4,
    totalNoGoBlocks: 3,
    totalManualReview: 1,
    totalReadinessChecks: 2,
    lastUpdatedAt: '2026-05-24T23:00:00Z',
    isDemoData: true,
    isPartialData: false,
    dataSource: 'demo seguro',
    warnings: ['Datos demo seguros locales usados como fallback read-only.']
  },
  files: [
    {
      fileId: 'demo-ach-in-001',
      fileName: 'ACH_COL_IN_001.ach',
      clearingHouseCode: 'ACH',
      profileCode: 'OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0',
      flowType: 'IncomingCreditFromExternalOriginator',
      isReturnFile: false,
      validationPassed: true,
      batchCount: 1,
      entryCount: 2,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      processingStatus: 'Processed',
      receivedAt: '2026-05-24T14:30:00Z',
      createdAt: '2026-05-24T14:30:00Z',
      correlationId: 'phase-6b5-uat-orch',
      hasErrors: false,
      warningCount: 0,
      errorCount: 0
    },
    {
      fileId: 'demo-cenit-in-001',
      fileName: 'CENIT_IN_001.ach',
      clearingHouseCode: 'CENIT',
      profileCode: 'OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0',
      flowType: 'DifferentialResponse',
      isReturnFile: false,
      validationPassed: true,
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      processingStatus: 'Processed',
      receivedAt: '2026-05-24T15:10:00Z',
      createdAt: '2026-05-24T15:10:00Z',
      correlationId: 'phase-6b5-response',
      hasErrors: false,
      warningCount: 1,
      errorCount: 0
    },
    {
      fileId: 'demo-ach-ret-001',
      fileName: 'ACH_COL_RET_001.RET',
      clearingHouseCode: 'ACH',
      profileCode: 'OFFICIAL_ACH_DEVOLUCION_V1_0',
      flowType: 'ReturnFile',
      isReturnFile: true,
      validationPassed: true,
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      processingStatus: 'ManualReviewRequired',
      receivedAt: '2026-05-24T16:40:00Z',
      createdAt: '2026-05-24T16:40:00Z',
      correlationId: 'phase-6b5-manual',
      hasErrors: false,
      warningCount: 1,
      errorCount: 0
    }
  ],
  decisions: [
    {
      correlationId: 'phase-6b5-uat-orch',
      fileName: 'ACH_COL_IN_001.ach',
      entryTraceNumber: '900000010000001',
      originalTraceNumber: null,
      decisionType: 'ApplyCreditMovement',
      soapOperationCandidate: 'ProcTransacciones',
      requiresMonetaryMovement: true,
      reasonCode: '00',
      reasonDescription: 'Simulacion UAT aprobada',
      newInternalStatus: 'Accepted',
      manualReviewRequired: false,
      isBlocked: false,
      blockReason: null,
      createdAt: '2026-05-24T23:00:00Z'
    },
    {
      correlationId: 'phase-6b5-response',
      fileName: 'CENIT_IN_001.ach',
      entryTraceNumber: '900000020000001',
      originalTraceNumber: '800000020000001',
      decisionType: 'RegisterDifferentialResponse',
      soapOperationCandidate: 'RegistrarRespuestaTransaccion',
      requiresMonetaryMovement: false,
      reasonCode: 'R01',
      reasonDescription: 'Respuesta diferencial sin movimiento monetario',
      newInternalStatus: 'Rejected',
      manualReviewRequired: false,
      isBlocked: false,
      blockReason: null,
      createdAt: '2026-05-24T23:00:00Z'
    },
    {
      correlationId: 'phase-6b5-manual',
      fileName: 'ACH_COL_RET_001.RET',
      entryTraceNumber: '900000030000001',
      originalTraceNumber: '800000030000001',
      decisionType: 'ManualReviewRequired',
      soapOperationCandidate: 'None',
      requiresMonetaryMovement: false,
      reasonCode: 'MR',
      reasonDescription: 'Correlacion ambigua',
      newInternalStatus: 'ManualReviewRequired',
      manualReviewRequired: true,
      isBlocked: true,
      blockReason: 'Manual review requerido; no se ejecuta SOAP.',
      createdAt: '2026-05-24T23:00:00Z'
    }
  ],
  readiness: [
    {
      correlationId: 'phase-6b5-uat-orch',
      operationCandidate: 'ProcTransacciones',
      isReadyForUat: true,
      isBlocked: false,
      blockReasons: [],
      payloadMappingPassed: true,
      requestMappingPassed: true,
      operationalGatePassed: true,
      readinessCheckPassed: true,
      simulationPassed: true,
      resiliencePassed: true,
      requiresMonetaryMovement: true,
      phase: '6B.5',
      lastCheckedAt: '2026-05-24T23:00:00Z',
      productiveExecution: false,
      wouldInvokeRealSoap: false
    },
    {
      correlationId: 'phase-6b5-nogo',
      operationCandidate: 'ProcContrapartidas',
      isReadyForUat: false,
      isBlocked: true,
      blockReasons: ['Invocacion SOAP real bloqueada por NO-GO en Fase 6B.5.6.'],
      payloadMappingPassed: true,
      requestMappingPassed: true,
      operationalGatePassed: false,
      readinessCheckPassed: false,
      simulationPassed: false,
      resiliencePassed: false,
      requiresMonetaryMovement: true,
      phase: '6B.5',
      lastCheckedAt: '2026-05-24T23:00:00Z',
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
  ],
  generatedAt: '2026-05-24T23:00:00Z',
  isDemoData: true,
  isPartialData: false,
  dataSource: 'demo seguro',
  warnings: ['Datos demo seguros locales usados como fallback read-only.'],
  productiveStatus: 'NO-GO'
};
