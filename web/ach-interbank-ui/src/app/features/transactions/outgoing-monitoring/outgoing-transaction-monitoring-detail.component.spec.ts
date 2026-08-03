import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';
import { OutgoingTransactionMonitoringDetailComponent } from './outgoing-transaction-monitoring-detail.component';

describe('OutgoingTransactionMonitoringDetailComponent', () => {
  let fixture: ComponentFixture<OutgoingTransactionMonitoringDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OutgoingTransactionMonitoringDetailComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([{ path: 'transactions/outgoing-monitoring/:id', component: OutgoingTransactionMonitoringDetailComponent }]),
        { provide: OutgoingTransactionMonitoringApiService, useValue: { getDetail: () => of(null) } }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(OutgoingTransactionMonitoringDetailComponent);
  });

  it('mantiene la información técnica ausente cuando el backend no la autoriza', () => {
    fixture.componentInstance.detail.set({ technicalDetail: undefined } as never);
    fixture.componentInstance.loading.set(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="outgoing-technical-detail"]')).toBeNull();
  });

  it('renderiza los hechos comprobables de la línea de tiempo', () => {
    fixture.detectChanges();
    fixture.componentInstance.detail.set({
      summary: { transactionExternalId: 'TX-1', processStatusDisplayName: 'Procesada', createdAtUtc: '2026-08-02T10:00:00Z', lastUpdatedAtUtc: '2026-08-02T11:00:00Z', clearingHouseDisplayName: 'Cámara', cycleDisplayName: 'Ciclo', destinationInstitutionDisplayName: 'Entidad', maskedDestinationAccount: '******1234', amount: 100, initialResultDisplayName: 'Aceptada', subsequentSituationDisplayName: 'Devuelta posteriormente', requiresAttention: false },
      classification: { directionDisplayName: 'Salida', originDisplayName: 'Originada por CFA', monetaryRouteDisplayName: 'Integración de contrapartidas', classificationStatusDisplayName: 'Determinada', classificationVersion: 1 },
      integration: { wasDispatched: true, attemptCount: 1, resultDisplayName: 'Aceptada' },
      files: [], responses: [], returns: [], warnings: ['Sin evidencia de transmisión.'],
      timeline: [{ occurredAtUtc: '2026-08-02T10:00:00Z', stageCode: 'Creation', stageDisplayName: 'Creación', title: 'Transacción creada', description: 'Registro comprobable.', outcomeCode: 'Recorded', outcomeDisplayName: 'Registrada', severity: 'info', sourceType: 'AchTransaction', isTechnical: false }]
    } as never);
    fixture.componentInstance.loading.set(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="outgoing-timeline"] li').length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Transacción creada');
    expect(fixture.nativeElement.textContent).toContain('Aceptada');
    expect(fixture.nativeElement.textContent).toContain('Devuelta posteriormente');
    expect(fixture.nativeElement.textContent).toContain('Esta transacción todavía no tiene un archivo NACHA-M asociado.');
    expect(fixture.nativeElement.textContent).toContain('Pendiente de información externa.');
  });

  it('presenta acceso denegado sin renderizar datos', () => {
    fixture.detectChanges();
    fixture.componentInstance.loading.set(false);
    fixture.componentInstance.forbidden.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Acceso no autorizado');
    expect(fixture.nativeElement.querySelector('[data-testid="outgoing-timeline"]')).toBeNull();
  });
});
