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
      summary: { transactionExternalId: 'TX-1', processStatusDisplayName: 'Procesada', createdAtUtc: '2026-08-02T10:00:00Z', lastUpdatedAtUtc: '2026-08-02T11:00:00Z', clearingHouseDisplayName: 'Cámara', cycleDisplayName: 'Ciclo', cycleProcessingDate: '2026-08-03T00:00:00Z', nextExpectedStepDisplayName: 'Sin pasos pendientes.', destinationInstitutionDisplayName: 'Entidad', maskedDestinationAccount: '******1234', amount: 100, initialResultDisplayName: 'Aceptada', subsequentSituationDisplayName: 'Devuelta posteriormente', requiresAttention: false },
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

  it('muestra el transporte y acuse Return Out CENIT con etiquetas operacionales', () => {
    fixture.detectChanges();
    fixture.componentInstance.detail.set({
      summary: { transactionExternalId: 'CENIT-RET-001', processStatusDisplayName: 'Procesada', createdAtUtc: '2026-08-20T10:00:00Z', lastUpdatedAtUtc: '2026-08-20T10:05:00Z', clearingHouseDisplayName: 'CENIT', cycleDisplayName: 'Ciclo 2', cycleProcessingDate: '2026-08-20T00:00:00Z', nextExpectedStepDisplayName: 'Sin pasos pendientes.', destinationInstitutionDisplayName: 'Entidad', maskedDestinationAccount: '******1234', amount: 100, initialResultDisplayName: 'Aceptada', subsequentSituationDisplayName: 'Devuelta posteriormente', requiresAttention: false },
      classification: { directionDisplayName: 'Salida', originDisplayName: 'Originada por CFA', monetaryRouteDisplayName: 'No aplica', classificationStatusDisplayName: 'Determinada', classificationVersion: 1 },
      integration: { wasDispatched: false, attemptCount: 0, resultDisplayName: 'No aplica' },
      files: [{
        fileId: 12, fileName: '0001122.001.2.ENV', operationDisplayName: 'Devolución / Return Out', artifactTypeDisplayName: 'Sobre digital preparado',
        fileSequence: 1, includedAtUtc: '2026-08-20T10:00:00Z', generatedAtUtc: '2026-08-20T10:01:00Z', contentSha256: 'ABC123',
        lifecycleStatusCode: 'Acknowledged', lifecycleStatusDisplayName: 'Acuse comprobado', hasTransmissionEvidence: true,
        transmissionReference: 'CFA-MFT-HANDOFF:ABC123', transmittedAtUtc: '2026-08-20T10:03:00Z', hasAcknowledgementEvidence: true,
        acknowledgedAtUtc: '2026-08-20T10:04:00Z', acknowledgementCode: 'ACCEPTED',
        transportAttempts: [{ attemptNumber: 1, startedAtUtc: '2026-08-20T10:02:00Z', completedAtUtc: '2026-08-20T10:03:00Z', statusCode: 'Succeeded', statusDisplayName: 'Entregada', retryable: false, resultCode: 'HANDOFF_COMMITTED', resultDescription: 'Entregado.', transmissionReference: 'CFA-MFT-HANDOFF:ABC123' }],
        transportResults: [{ id: '00000000-0000-0000-0000-000000000001', occurredAtUtc: '2026-08-20T10:04:00Z', receivedAtUtc: '2026-08-20T10:04:01Z', processedAtUtc: '2026-08-20T10:04:02Z', outcomeCode: 'Accepted', outcomeDisplayName: 'Aceptada', resultCode: 'ACCEPTED', resultDescription: 'Resultado aceptado.', correlationStatusDisplayName: 'Correlacionada', applied: true, requiresManualReview: false }]
      }],
      responses: [], returns: [], warnings: [],
      timeline: [{ occurredAtUtc: '2026-08-20T10:04:00Z', stageCode: 'Acknowledgement', stageDisplayName: 'Acuse o resultado', title: 'Resultado de transporte recibido', description: 'Resultado aceptado.', outcomeCode: 'Accepted', outcomeDisplayName: 'Aceptada', severity: 'success', sourceType: 'AchFileTransportResult', isTechnical: false }]
    } as never);
    fixture.componentInstance.loading.set(false);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('CENIT');
    expect(text).toContain('Devolución / Return Out');
    expect(text).toContain('CFA-MFT-HANDOFF:ABC123');
    expect(text).toContain('Número de intentos');
    expect(text).toContain('HANDOFF_COMMITTED');
    expect(text).toContain('Resultado aceptado');
    expect(text).toContain('Resultado de transporte recibido');
  });
});
