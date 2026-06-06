import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AchReconciliationService } from '../services/ach-reconciliation.service';
import { AchReconciliationConsoleComponent } from './ach-reconciliation-console.component';

describe('AchReconciliationConsoleComponent', () => {
  let fixture: ComponentFixture<AchReconciliationConsoleComponent>;
  let service: jasmine.SpyObj<AchReconciliationService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<AchReconciliationService>('AchReconciliationService', ['getConsoleData', 'getItem']);
    service.getConsoleData.and.returnValue(of(data()));
    service.getItem.and.returnValue(of(detail()));

    await TestBed.configureTestingModule({
      imports: [AchReconciliationConsoleComponent],
      providers: [
        provideRouter([]),
        { provide: AchReconciliationService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchReconciliationConsoleComponent);
    fixture.detectChanges();
  });

  it('ReconciliationComponent_ShouldCreate', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('ReconciliationComponent_ShouldRenderNoGoBanner', () => {
    expect(text()).toContain('Productivo NO-GO');
  });

  it('ReconciliationComponent_ShouldRenderReadOnlyBanner', () => {
    expect(text()).toContain('Solo lectura sanitizada');
  });

  it('ReconciliationComponent_ShouldRenderNoMonetaryMovementBanner', () => {
    expect(text()).toContain('Sin movimientos monetarios');
  });

  it('ReconciliationComponent_ShouldRenderSummaryCards', () => {
    expect(text()).toContain('Respuestas');
    expect(text()).toContain('Diferenciales');
    expect(text()).toContain('ROR');
  });

  it('ReconciliationComponent_ShouldRenderItemsTable', () => {
    expect(text()).toContain('Ítems conciliación ACH');
    expect(fixture.componentInstance.filteredItems[0].reconciliationId).toBe('resp-1');
  });

  it('ReconciliationComponent_ShouldRenderStatusBadges', () => {
    expect(text()).toContain('CONCILIADO');
    expect(text()).toContain('REVISIÓN MANUAL');
    expect(text()).toContain('CANDIDATO MONETARIO');
  });

  it('ReconciliationComponent_ShouldRenderDetail', () => {
    expect(text()).toContain('Detalle de conciliación');
    expect(fixture.componentInstance.detail?.noSensitiveData).toBeTrue();
  });

  it('ReconciliationComponent_ShouldNotRenderDangerousActions', () => {
    const content = text();

    expect(content).not.toContain('Ejecutar SOAP');
    expect(content).not.toContain('Aprobar manualmente');
    expect(content).not.toContain('Reprocesar');
    expect(content).not.toContain('Mover dinero');
  });

  it('ReconciliationService_ShouldHandlePartialWarnings', () => {
    expect(text()).toContain('Advertencias parciales');
  });

  it('ReconciliationService_ShouldHandleErrorState', () => {
    service.getConsoleData.and.returnValue(throwError(() => new Error('fallo controlado')));
    const errorFixture = TestBed.createComponent(AchReconciliationConsoleComponent);

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
      totalResponses: 2,
      totalDifferentialResponses: 1,
      totalReturns: 1,
      totalRejections: 1,
      totalPrenotifications: 1,
      totalRor: 1,
      totalReconciled: 1,
      totalPending: 1,
      totalInconsistent: 1,
      totalManualReviewRequired: 1,
      totalNonMonetary: 3,
      totalMonetaryCandidates: 1,
      lastUpdatedAt: '2026-05-31T12:00:00Z',
      dataSource: 'parcial',
      isPartialData: true,
      warnings: ['Datos parciales solo lectura.']
    },
    items: [item()]
  };
}

function item() {
  return {
    reconciliationId: 'resp-1',
    correlationId: 'corr-1',
    fileName: 'entrada.ach',
    clearingHouseCode: 'ACH',
    flowType: 'DifferentialResponse',
    responseType: 'Respuesta diferencial',
    reasonCode: 'R01',
    traceNumberMasked: '***0001',
    originalTraceNumberMasked: '***9999',
    internalStatus: 'Notificada',
    reconciliationStatus: 'Conciliado',
    requiresManualReview: false,
    isReturnFile: false,
    isRor: false,
    isPrenotification: false,
    isNonMonetary: true,
    isMonetaryCandidate: false,
    soapOperationCandidate: 'RegistrarRespuestaTransaccion',
    createdAt: '2026-05-31T12:00:00Z',
    dataSource: 'backend read-only',
    isPersisted: true,
    isDerived: true
  };
}

function detail() {
  return {
    item: item(),
    nachaHeaderSummary: { headerId: 'N1' },
    batchSummary: { batchNumber: 1 },
    entrySummary: { traceNumberMasked: '***0001' },
    addendaSummary: { originalTraceNumberMasked: '***9999' },
    controlSummary: { entryAddendaCount: 2 },
    internalTransactionSummary: { transactionId: 1 },
    responseHistory: [],
    auditEvents: [],
    warnings: ['Detalle sanitizado.'],
    noSensitiveData: true
  };
}
