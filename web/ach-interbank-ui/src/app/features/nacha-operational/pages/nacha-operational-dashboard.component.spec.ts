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

function data(): NachaOperationalDashboardData {
  return {
    summary: {
      productiveStatus: 'NO-GO',
      backendPhase: '6B.5.6',
      soapMode: 'Simulated',
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      totalFiles: 1,
      totalDecisions: 1,
      totalSoapCandidates: 1,
      totalNoGoBlocks: 1,
      totalManualReview: 0,
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
        entryCount: 1,
        addendaCount: 1,
        processingStatus: 'Processed',
        receivedAt: '2026-05-24T23:00:00Z'
      }
    ],
    decisions: [
      {
        correlationId: 'phase-6c1',
        decisionType: 'ApplyCreditMovement',
        soapOperationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        reasonCode: '00',
        reasonDescription: 'UAT',
        newInternalStatus: 'Accepted',
        manualReviewRequired: false
      }
    ],
    readiness: [
      {
        correlationId: 'phase-6c1',
        isReadyForUat: true,
        isBlocked: false,
        blockReasons: [],
        operationalGatePassed: true,
        readinessCheckPassed: true,
        simulationPassed: true,
        resiliencePassed: true,
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
    ]
  };
}
