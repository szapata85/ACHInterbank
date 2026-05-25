import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { NachaOperationalDashboardData } from '../models/nacha-operational.models';
import { NachaOperationalReadinessService } from '../services/nacha-operational-readiness.service';
import { NachaOperationalDashboardComponent } from './nacha-operational-dashboard.component';

describe('NachaOperationalDashboardComponent', () => {
  let fixture: ComponentFixture<NachaOperationalDashboardComponent>;
  let service: jasmine.SpyObj<NachaOperationalReadinessService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaOperationalReadinessService>('NachaOperationalReadinessService', ['getDashboardData']);
    service.getDashboardData.and.returnValue(of(data()));

    await TestBed.configureTestingModule({
      imports: [NachaOperationalDashboardComponent],
      providers: [
        provideRouter([]),
        { provide: NachaOperationalReadinessService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('Component_ShouldRenderNoGoBanner', () => {
    expect(text()).toContain('Productivo NO-GO');
    expect(text()).toContain('SOAP real deshabilitado');
  });

  it('Component_ShouldRenderOperationalSummary', () => {
    expect(text()).toContain('Fase backend');
    expect(text()).toContain('6B.5.6');
    expect(text()).toContain('NO-GO');
  });

  it('Component_ShouldRenderBackendReadStoreData', () => {
    expect(text()).toContain('Fuente: backend read-only');
    expect(text()).toContain('BACKEND READ-ONLY SANITIZADO');
  });

  it('Component_ShouldRenderDataSourceBadge', () => {
    expect(text()).toContain('Servicio: backend read-only');
  });

  it('Component_ShouldRenderPartialDataWarning', () => {
    service.getDashboardData.and.returnValue(of(data({ isPartialData: true, dataSource: 'parcial', warnings: ['No persisted SOAP readiness data found; using safe read-only placeholder.'] })));
    const partialFixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    partialFixture.detectChanges();

    expect(partialFixture.nativeElement.textContent).toContain('Fuente: parcial');
    expect(partialFixture.nativeElement.textContent).toContain('No persisted SOAP readiness data found');
  });

  it('Component_ShouldFallbackToDemoWhenApiFails', () => {
    service.getDashboardData.and.returnValue(of(data({ isDemoData: true, dataSource: 'demo seguro' })));
    const demoFixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    demoFixture.detectChanges();

    expect(demoFixture.nativeElement.textContent).toContain('Fuente: demo seguro');
    expect(demoFixture.nativeElement.textContent).toContain('DEMO READ-ONLY');
  });

  it('Component_ShouldKeepNoGoBannerWithReadStoreData', () => {
    expect(text()).toContain('Productivo NO-GO');
    expect(text()).toContain('NO-GO');
  });

  it('Component_ShouldRenderFilesTable', () => {
    expect(text()).toContain('Archivos NACHA-M');
  });

  it('Component_ShouldRenderDecisionsTable', () => {
    expect(text()).toContain('Decisiones funcionales');
  });

  it('Component_ShouldRenderReadinessTable', () => {
    expect(text()).toContain('Readiness SOAP/UAT');
  });

  it('Component_ShouldRenderAuditTable', () => {
    expect(text()).toContain('Auditoria Phase 6B.5');
  });

  it('Component_ShouldShowLoadingState', () => {
    service.getDashboardData.and.returnValue(new Subject<NachaOperationalDashboardData>().asObservable());
    const loadingFixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    loadingFixture.detectChanges();

    expect(loadingFixture.nativeElement.textContent).toContain('Cargando consulta operativa NACHA-M');
  });

  it('Component_ShouldShowErrorState', () => {
    service.getDashboardData.and.returnValue(throwError(() => new Error('fallo controlado')));
    const errorFixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    errorFixture.detectChanges();

    expect(errorFixture.nativeElement.textContent).toContain('fallo controlado');
  });

  it('Component_ShouldNotRenderExecutionButtons', () => {
    const content = text();

    expect(content).not.toContain('Ejecutar SOAP');
    expect(content).not.toContain('Mover dinero');
    expect(content).not.toContain('Habilitar productivo');
  });

  function text(): string {
    return fixture.nativeElement.textContent;
  }
});

function data(overrides: Partial<NachaOperationalDashboardData> = {}): NachaOperationalDashboardData {
  const isDemoData = overrides.isDemoData ?? false;
  const isPartialData = overrides.isPartialData ?? false;
  const dataSource = overrides.dataSource ?? (isPartialData ? 'parcial' : isDemoData ? 'demo seguro' : 'backend read-only');
  const warnings = overrides.warnings ?? (isPartialData ? ['Datos parciales read-only.'] : []);

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
      totalNoGoBlocks: 1,
      totalManualReview: 0,
      totalReadinessChecks: 1,
      lastUpdatedAt: '2026-05-24T23:00:00Z',
      isDemoData,
      isPartialData,
      dataSource,
      warnings
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
        entryCount: 1,
        addendaCount: 1,
        batchControlCount: 1,
        fileControlCount: 1,
        processingStatus: 'Processed',
        receivedAt: '2026-05-24T23:00:00Z',
        createdAt: '2026-05-24T23:00:00Z',
        correlationId: 'phase-6c1',
        hasErrors: false,
        warningCount: 0,
        errorCount: 0
      }
    ],
    decisions: [
      {
        correlationId: 'phase-6c1',
        fileName: 'ACH_COL_IN_001.ach',
        entryTraceNumber: '900000010000001',
        originalTraceNumber: null,
        decisionType: 'ApplyCreditMovement',
        soapOperationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        reasonCode: '00',
        reasonDescription: 'UAT',
        newInternalStatus: 'Accepted',
        manualReviewRequired: false,
        isBlocked: false,
        blockReason: null,
        createdAt: '2026-05-24T23:00:00Z'
      }
    ],
    readiness: [
      {
        correlationId: 'phase-6c1',
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
      }
    ],
    audit: [
      {
        correlationId: 'phase-6c1',
        phase: '6B.5',
        eventType: 'Completed',
        severity: 'Information',
        message: 'Readiness operacional UAT finalizado.',
        isBlocked: false,
        timestamp: '2026-05-24T23:00:00Z',
        sanitizedDetails: { Phase: '6B.5' }
      }
    ],
    generatedAt: '2026-05-24T23:00:00Z',
    isDemoData,
    isPartialData,
    dataSource,
    warnings,
    productiveStatus: 'NO-GO'
  };
}
