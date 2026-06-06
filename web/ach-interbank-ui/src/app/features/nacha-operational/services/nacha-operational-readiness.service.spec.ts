import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NachaOperationalDashboardData } from '../models/nacha-operational.models';
import { NachaOperationalReadinessService } from './nacha-operational-readiness.service';

describe('NachaOperationalReadinessService', () => {
  let service: NachaOperationalReadinessService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    api.get.and.returnValue(of(apiDashboard()));

    TestBed.configureTestingModule({
      providers: [{ provide: ApiService, useValue: api }]
    });

    service = TestBed.inject(NachaOperationalReadinessService);
  });

  it('Service_ShouldCallDashboardEndpoint', async () => {
    await firstValueFrom(service.getDashboardData());

    expect(api.get).toHaveBeenCalledWith('api/ach/nacha/operational/dashboard');
  });

  it('Service_ShouldCallFileDetailEndpoint', async () => {
    api.get.and.returnValue(of(fileDetail()));

    await firstValueFrom(service.getFileDetail('nacha-N1'));

    expect(api.get).toHaveBeenCalledWith('api/ach/nacha/operational/files/nacha-N1');
  });

  it('Service_ShouldHandle404WithControlledMessage', async () => {
    api.get.and.returnValue(throwError(() => ({ status: 404 })));

    await expectAsync(firstValueFrom(service.getFileDetail('nacha-missing')))
      .toBeRejectedWithError('Archivo NACHA-M no encontrado o no persistido.');
  });

  it('Dashboard_ShouldNotCallLegacyLayoutsOrDefinitions', async () => {
    await firstValueFrom(service.getDashboardData());

    const calledPaths = api.get.calls.allArgs().map((args) => args[0]);
    expect(calledPaths).not.toContain('nacha-layouts');
    expect(calledPaths).not.toContain('nacha-record-definitions');
  });

  it('Service_ShouldFallbackToDemoDataWhenBackendFails', async () => {
    api.get.and.returnValue(throwError(() => new Error('api down')));

    const dashboard = await firstValueFrom(service.getDashboardData());

    expect(dashboard.summary.productiveStatus).toBe('NO-GO');
    expect(dashboard.summary.isDemoData).toBeTrue();
    expect(dashboard.summary.wouldInvokeRealSoap).toBeFalse();
  });

  it('Service_ShouldMapReadStoreDashboardResponse', async () => {
    const dashboard = await firstValueFrom(service.getDashboardData());

    expect(dashboard.isDemoData).toBeFalse();
    expect(dashboard.isPartialData).toBeTrue();
    expect(dashboard.dataSource).toBe('parcial');
    expect(dashboard.summary.dataSource).toBe('parcial');
    expect(dashboard.files[0].dataSource).toBe('backend read-only');
  });

  it('Service_ShouldPreserveFallbackDemoBehavior', async () => {
    api.get.and.returnValue(throwError(() => new Error('api down')));

    const dashboard = await firstValueFrom(service.getDashboardData());

    expect(dashboard.isDemoData).toBeTrue();
    expect(dashboard.dataSource).toBe('demo seguro');
    expect(dashboard.summary.warnings.join(' ')).toContain('respaldo solo lectura');
  });

  it('Service_ShouldReturnOperationalSummaryNoGo', async () => {
    const summary = await firstValueFrom(service.getOperationalSummary());

    expect(summary.productiveStatus).toBe('NO-GO');
    expect(summary.productiveExecution).toBeFalse();
    expect(summary.wouldInvokeRealSoap).toBeFalse();
    expect(summary.isDemoData).toBeFalse();
  });

  it('Service_ShouldReturnFilesReadOnlyDemoData', async () => {
    const files = await firstValueFrom(service.getFiles());

    expect(files.length).toBeGreaterThan(0);
    expect(files.every((file) => !!file.fileName)).toBeTrue();
  });

  it('Service_ShouldReturnDecisionsWithoutRealSoapExecution', async () => {
    const decisions = await firstValueFrom(service.getDecisions());

    expect(decisions.some((decision) => decision.soapOperationCandidate === 'ProcTransacciones')).toBeTrue();
    expect(decisions.every((decision) => decision.soapOperationCandidate !== 'RealSoapInvocation')).toBeTrue();
  });

  it('Service_ShouldReturnSoapReadinessWithRealSoapDisabled', async () => {
    const readiness = await firstValueFrom(service.getSoapReadiness());

    expect(readiness.every((item) => item.productiveExecution === false)).toBeTrue();
    expect(readiness.every((item) => item.wouldInvokeRealSoap === false)).toBeTrue();
  });

  it('Service_ShouldReturnSanitizedAudit', async () => {
    const audit = await firstValueFrom(service.getAudit());
    const joined = JSON.stringify(audit);

    expect(joined).not.toContain('password');
    expect(joined).not.toContain('token');
    expect(joined).not.toContain('1234567890123456');
  });
});

function apiDashboard(): NachaOperationalDashboardData {
  return {
    summary: {
      productiveStatus: 'NO-GO',
      backendPhase: '6B.5.6',
      soapMode: 'Simulated',
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      totalFiles: 1,
      totalIncomingFiles: 1,
      totalOutgoingFiles: 0,
      totalReturnFiles: 0,
      totalDecisions: 1,
      totalSoapCandidates: 1,
      totalNoGoBlocks: 0,
      totalManualReview: 0,
      totalReadinessChecks: 1,
      lastUpdatedAt: '2026-05-24T23:00:00Z',
      isDemoData: false,
      isPartialData: true,
      dataSource: 'parcial',
      warnings: ['No se encontraron datos persistidos de preparación SOAP; se usa un marcador seguro solo lectura.']
    },
    files: [
      {
        fileId: 'backend-ach-in-001',
        fileName: 'ACH_COL_IN_001.ach',
        dataSource: 'backend read-only',
        headerId: 'N1',
        persistedRecordCount: 5,
        lastParsedAt: '2026-05-24T23:00:00Z',
        noSensitiveData: true,
        clearingHouseCode: 'ACH',
        profileCode: 'OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0',
        flowType: 'IncomingCreditFromExternalOriginator',
        isReturnFile: false,
        validationPassed: true,
        batchCount: 1,
        entryCount: 1,
        addendaCount: 1,
        batchControlCount: 1,
        fileControlCount: 1,
        processingStatus: 'Processed',
        receivedAt: '2026-05-24T23:00:00Z',
        createdAt: '2026-05-24T23:00:00Z',
        correlationId: 'phase-6c2',
        hasErrors: false,
        warningCount: 0,
        errorCount: 0
      }
    ],
    decisions: [
      {
        correlationId: 'phase-6c2',
        fileName: 'ACH_COL_IN_001.ach',
        entryTraceNumber: '900000010000001',
        originalTraceNumber: null,
        decisionType: 'ApplyCreditMovement',
        soapOperationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        reasonCode: '00',
        reasonDescription: 'Backend solo lectura',
        newInternalStatus: 'Accepted',
        manualReviewRequired: false,
        isBlocked: false,
        blockReason: null,
        dataSource: 'backend read-only',
        isDerived: true,
        isPersisted: true,
        warning: null,
        createdAt: '2026-05-24T23:00:00Z'
      }
    ],
    readiness: [
      {
        correlationId: 'phase-6c2',
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
        wouldInvokeRealSoap: false,
        dataSource: 'backend read-only',
        isDerived: true,
        isPersisted: true,
        warning: null
      }
    ],
    audit: [
      {
        correlationId: 'phase-6c2',
        phase: '6B.5',
        eventType: 'Projected',
        severity: 'Information',
        message: 'Backend solo lectura sanitizado.',
        isBlocked: false,
        dataSource: 'backend read-only',
        isDerived: false,
        isPersisted: true,
        warning: null,
        timestamp: '2026-05-24T23:00:00Z',
        sanitizedDetails: { Productivo: 'NO-GO' }
      }
    ],
    generatedAt: '2026-05-24T23:00:00Z',
    isDemoData: false,
    isPartialData: true,
    dataSource: 'parcial',
    warnings: ['No se encontraron datos persistidos de preparación SOAP; se usa un marcador seguro solo lectura.'],
    productiveStatus: 'NO-GO'
  };
}

function fileDetail() {
  return {
    fileId: 'nacha-N1',
    headerId: 'N1',
    fileName: 'entrada.ach',
    clearingHouseCode: 'ACH',
    profileCode: 'nacha-config profiles',
    flowType: 'IncomingPersisted',
    isReturnFile: false,
    processingStatus: 'Persisted',
    validationPassed: true,
    receivedAt: '2026-05-24T23:00:00Z',
    createdAt: '2026-05-24T23:00:00Z',
    correlationId: 'corr-N1',
    dataSource: 'backend read-only',
    isPartialData: false,
    warnings: [],
    header: { headerId: 'N1', cycleNumber: 1 },
    batches: [],
    entries: [],
    addendas: [],
    batchControls: [],
    fileControls: [],
    totalsSummary: {
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      persistedRecordCount: 5,
      totalDebitAmount: 0,
      totalCreditAmount: 0,
      validationPassed: true
    },
    noSensitiveData: true
  };
}
