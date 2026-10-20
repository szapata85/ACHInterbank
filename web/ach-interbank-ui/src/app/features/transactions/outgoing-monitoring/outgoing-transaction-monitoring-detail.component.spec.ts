import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';
import { OutgoingTransactionMonitoringDetailComponent } from './outgoing-transaction-monitoring-detail.component';

const DETAIL = {
  summary: {
    transactionExternalId: 'TX-OUT-42', processStatusDisplayName: 'Procesada', createdAtUtc: '2026-08-02T10:00:00Z',
    lastUpdatedAtUtc: '2026-08-02T11:00:00Z', clearingHouseDisplayName: 'CENIT', cycleDisplayName: 'Ciclo 2',
    cycleProcessingDate: '2026-08-03T00:00:00Z', nextExpectedStepDisplayName: 'Sin pasos pendientes.',
    destinationInstitutionDisplayName: 'Entidad destino', maskedDestinationAccount: '******1234', amount: 100,
    initialResultDisplayName: 'Aceptada', subsequentSituationDisplayName: 'Devuelta posteriormente', requiresAttention: true,
    attentionReason: 'Revisar causal contextual.'
  },
  classification: {
    directionDisplayName: 'Salida', originDisplayName: 'Originada por CFA', monetaryRouteDisplayName: 'Integración de contrapartidas',
    classificationStatusDisplayName: 'Determinada', classifiedAtUtc: '2026-08-02T10:00:01Z', classificationVersion: 1
  },
  integration: {
    wasDispatched: true, attemptCount: 1, resultDisplayName: 'Aceptada', responseCode: '00', responseDescription: 'Aceptada',
    lastAttemptAtUtc: '2026-08-02T10:01:00Z', lastSuccessAtUtc: '2026-08-02T10:01:01Z'
  },
  files: [{
    fileId: 12, fileName: '0001122.001.2.ENV', operationDisplayName: 'Devolución / Return Out', artifactTypeDisplayName: 'Sobre digital preparado',
    version: 3, fileSequence: 1, includedAtUtc: '2026-08-20T10:00:00Z', generatedAtUtc: '2026-08-20T10:01:00Z', contentSha256: 'ABC123',
    lifecycleStatusCode: 'Acknowledged', lifecycleStatusDisplayName: 'Acuse comprobado', hasTransmissionEvidence: true,
    transmissionReference: 'CFA-MFT-HANDOFF:ABC123', transmittedAtUtc: '2026-08-20T10:03:00Z', hasAcknowledgementEvidence: true,
    acknowledgedAtUtc: '2026-08-20T10:04:00Z', acknowledgementCode: 'ACCEPTED',
    transportAttempts: [{
      attemptNumber: 1, startedAtUtc: '2026-08-20T10:02:00Z', completedAtUtc: '2026-08-20T10:03:00Z', statusCode: 'Succeeded',
      statusDisplayName: 'Entregada', retryable: false, resultCode: 'HANDOFF_COMMITTED', resultDescription: 'Entregado.',
      transmissionReference: 'CFA-MFT-HANDOFF:ABC123'
    }],
    transportResults: [{
      id: '00000000-0000-0000-0000-000000000001', occurredAtUtc: '2026-08-20T10:04:00Z', receivedAtUtc: '2026-08-20T10:04:01Z',
      processedAtUtc: '2026-08-20T10:04:02Z', outcomeCode: 'Accepted', outcomeDisplayName: 'Aceptada', resultCode: 'ACCEPTED',
      resultDescription: 'Resultado aceptado.', correlationStatusDisplayName: 'Correlacionada', applied: true, requiresManualReview: false
    }]
  }],
  responses: [{
    id: 'response-1', receivedAtUtc: '2026-08-20T10:05:00Z', responseTypeDisplayName: 'Respuesta de cámara', externalStatusCode: '00',
    causeCode: 'R01', causeDescription: 'Causal contextual', correlationStatusDisplayName: 'Correlacionada'
  }],
  returns: [{
    occurredAtUtc: '2026-08-20T10:06:00Z', stateDisplayName: 'Devolución recibida', causeCode: 'R01', causeDescription: 'Fondos insuficientes'
  }],
  timeline: [{
    occurredAtUtc: '2026-08-20T10:04:00Z', stageCode: 'Acknowledgement', stageDisplayName: 'Acuse o resultado',
    title: 'Resultado de transporte recibido', description: 'Resultado aceptado.', outcomeCode: 'Accepted', outcomeDisplayName: 'Aceptada',
    severity: 'success', sourceType: 'AchFileTransportResult', isTechnical: false
  }],
  warnings: ['Sin evidencia adicional de certificación.'],
  technicalDetail: {
    transactionId: 42, lastIntegrationMethod: 'Proc_Contrapartidas', lastIntegrationMode: 'Live', lastIntegrationCode: 'OK',
    lastIntegrationDurationMs: 25, lastCorrelationId: 'correlation-42'
  }
};

describe('OutgoingTransactionMonitoringDetailComponent', () => {
  let fixture: ComponentFixture<OutgoingTransactionMonitoringDetailComponent>;
  let api: jasmine.SpyObj<OutgoingTransactionMonitoringApiService>;
  let router: jasmine.SpyObj<Router>;
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let routeStub: { paramMap: Observable<ParamMap>; snapshot: { paramMap: ParamMap } };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OutgoingTransactionMonitoringApiService>('OutgoingTransactionMonitoringApiService', ['getDetail']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.returnValue(Promise.resolve(true));
    routeParams = new BehaviorSubject(convertToParamMap({ id: '42' }));
    routeStub = {
      paramMap: routeParams.asObservable(),
      snapshot: { paramMap: convertToParamMap({ id: '42' }) }
    };
    api.getDetail.and.returnValue(of(DETAIL as never));

    await TestBed.configureTestingModule({
      imports: [OutgoingTransactionMonitoringDetailComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: routeStub },
        { provide: Router, useValue: router },
        { provide: OutgoingTransactionMonitoringApiService, useValue: api }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OutgoingTransactionMonitoringDetailComponent);
  });

  it('carga por id válido usando el API actual', () => {
    fixture.detectChanges();

    expect(api.getDetail).toHaveBeenCalledOnceWith(42);
    expect(fixture.componentInstance.detail()).toBe(DETAIL as never);
    expect(fixture.nativeElement.textContent).toContain('TX-OUT-42');
  });

  it('mantiene 404 como Transacción no encontrada', () => {
    api.getDetail.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    fixture.detectChanges();

    expect(fixture.componentInstance.notFound()).toBeTrue();
    expect(fixture.componentInstance.forbidden()).toBeFalse();
    expect(fixture.componentInstance.error()).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Transacción no encontrada');
  });

  [401, 403].forEach(status => {
    it(`mantiene ${status} como Acceso no autorizado`, () => {
      api.getDetail.and.returnValue(throwError(() => new HttpErrorResponse({ status })));

      fixture.detectChanges();

      expect(fixture.componentInstance.forbidden()).toBeTrue();
      expect(fixture.componentInstance.notFound()).toBeFalse();
      expect(fixture.componentInstance.error()).toBeFalse();
      expect(fixture.nativeElement.textContent).toContain('Acceso no autorizado');
    });
  });

  it('mantiene error general y permite recargar el mismo id', () => {
    api.getDetail.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 500 })),
      of(DETAIL as never)
    );

    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBeTrue();
    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();

    fixture.componentInstance.reload();
    fixture.detectChanges();

    expect(api.getDetail.calls.allArgs()).toEqual([[42], [42]]);
    expect(fixture.componentInstance.error()).toBeFalse();
    expect(fixture.componentInstance.detail()).toBe(DETAIL as never);
  });

  it('vuelve al listado de monitoreo', () => {
    fixture.componentInstance.back();

    expect(router.navigate).toHaveBeenCalledWith(['/transactions/outgoing-monitoring']);
  });

  it('preserva summary, archivos, transporte, respuestas, devoluciones y timeline', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Resumen');
    expect(text).toContain('CENIT');
    expect(text).toContain('0001122.001.2.ENV');
    expect(text).toContain('Intentos de transporte');
    expect(text).toContain('HANDOFF_COMMITTED');
    expect(text).toContain('Respuesta de cámara');
    expect(text).toContain('Devolución recibida');
    expect(text).toContain('Resultado de transporte recibido');
    expect(fixture.nativeElement.querySelectorAll('[data-testid="outgoing-timeline"] li').length).toBe(1);
  });

  it('mantiene Información técnica autorizada como read-only', () => {
    fixture.detectChanges();

    const technical = fixture.nativeElement.querySelector('[data-testid="outgoing-technical-detail"]');
    expect(technical).not.toBeNull();
    expect(technical.textContent).toContain('Información técnica autorizada');
    expect(technical.textContent).toContain('Proc_Contrapartidas');
    expect(technical.querySelector('input, textarea, select, [contenteditable="true"]')).toBeNull();
  });

  it('no expone información técnica cuando el backend no la autoriza', () => {
    api.getDetail.and.returnValue(of({ ...DETAIL, technicalDetail: undefined } as never));

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="outgoing-technical-detail"]')).toBeNull();
  });

  it('adopta page header y estados compartidos sin convertir el detalle en editable', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-page-header')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="outgoing-monitoring-detail"] input')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="outgoing-monitoring-detail"] textarea')).toBeNull();
  });
});
