import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NachaSoapUatConsoleService } from '../services/nacha-soap-uat-console.service';
import { NachaSoapUatConsoleComponent } from './nacha-soap-uat-console.component';

describe('NachaSoapUatConsoleComponent', () => {
  let fixture: ComponentFixture<NachaSoapUatConsoleComponent>;
  let service: jasmine.SpyObj<NachaSoapUatConsoleService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<NachaSoapUatConsoleService>('NachaSoapUatConsoleService', ['getConsoleData']);
    service.getConsoleData.and.returnValue(of(data()));

    await TestBed.configureTestingModule({
      imports: [NachaSoapUatConsoleComponent],
      providers: [
        provideRouter([]),
        { provide: NachaSoapUatConsoleService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaSoapUatConsoleComponent);
    fixture.detectChanges();
  });

  it('ConsoleComponent_ShouldCreate', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('ConsoleComponent_ShouldRenderNoGoBanner', () => {
    expect(text()).toContain('Productivo NO-GO');
  });

  it('ConsoleComponent_ShouldRenderSoapDisabledBanner', () => {
    expect(text()).toContain('SOAP real deshabilitado');
  });

  it('ConsoleComponent_ShouldRenderReadOnlyBanner', () => {
    expect(text()).toContain('Solo lectura sanitizada');
  });

  it('ConsoleComponent_ShouldRenderCandidatesAndAudit', () => {
    expect(text()).toContain('Candidatos SOAP/UAT');
    expect(text()).toContain('Auditoría SOAP/UAT');
    expect(text()).toContain('ProcTransacciones');
  });

  it('ConsoleComponent_ShouldRenderBlockedAndManualReviewBadges', () => {
    expect(text()).toContain('BLOQUEADO');
    expect(text()).toContain('REVISIÓN MANUAL');
  });

  it('ConsoleComponent_ShouldNotRenderExecutionButtons', () => {
    const content = text();

    expect(content).not.toContain('Ejecutar SOAP');
    expect(content).not.toContain('Reintentar SOAP');
    expect(content).not.toContain('Enviar movimiento');
    expect(content).not.toContain('Invocar core');
  });

  it('ConsoleService_ShouldHandleErrorState', () => {
    service.getConsoleData.and.returnValue(throwError(() => new Error('fallo controlado')));
    const errorFixture = TestBed.createComponent(NachaSoapUatConsoleComponent);

    errorFixture.detectChanges();

    expect(errorFixture.nativeElement.textContent).toContain('fallo controlado');
  });

  function text(): string {
    return fixture.nativeElement.textContent;
  }
});

function data() {
  return {
    dashboard: {
      productiveStatus: 'NO-GO',
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      totalCandidates: 2,
      totalReadyForUat: 0,
      totalBlocked: 1,
      totalManualReview: 1,
      totalRegistrarRespuesta: 1,
      totalProcTransacciones: 1,
      totalProcContrapartidas: 0,
      totalNone: 0,
      totalSimulationPassed: 1,
      totalSimulationFailed: 1,
      totalResilienceWarnings: 1,
      totalDuplicateOrIdempotent: 2,
      lastUpdatedAt: '2026-05-31T12:00:00Z',
      dataSource: 'parcial',
      isPartialData: true,
      warnings: ['Consola parcial solo lectura.']
    },
    candidates: [
      {
        correlationId: 'corr-proc',
        fileName: 'entrada.ach',
        entryTraceNumber: '***0001',
        decisionType: 'CreditoEntrante',
        operationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        productiveExecution: false,
        wouldInvokeRealSoap: false,
        isReadyForUat: false,
        isBlocked: true,
        blockReasons: ['NO-GO'],
        manualReviewRequired: false,
        readinessStatus: 'BlockedByNoGo',
        simulationStatus: 'Passed',
        resilienceStatus: 'Warning',
        idempotencyStatus: 'Idempotent',
        lastAttemptAt: '2026-05-31T12:00:00Z',
        attemptCount: 1,
        dataSource: 'backend read-only',
        isPersisted: true,
        isDerived: true
      },
      {
        correlationId: 'corr-manual',
        fileName: 'retorno.RET',
        entryTraceNumber: '***0002',
        decisionType: 'ManualReviewRequired',
        operationCandidate: 'None',
        requiresMonetaryMovement: false,
        productiveExecution: false,
        wouldInvokeRealSoap: false,
        isReadyForUat: false,
        isBlocked: true,
        blockReasons: ['ManualReviewRequired'],
        manualReviewRequired: true,
        readinessStatus: 'BlockedByNoGo',
        simulationStatus: 'NotPersisted',
        resilienceStatus: 'NotPersisted',
        idempotencyStatus: 'Idempotent',
        attemptCount: 0,
        dataSource: 'backend read-only',
        isPersisted: true,
        isDerived: true
      }
    ],
    audit: [{ correlationId: 'corr-proc', phase: '6B.5', eventType: 'Audit', severity: 'Information', message: 'Sanitized', isBlocked: false, timestamp: '2026-05-31T12:00:00Z', sanitizedDetails: { Payload: 'Sanitized' }, dataSource: 'backend read-only', isPersisted: true }]
  };
}
