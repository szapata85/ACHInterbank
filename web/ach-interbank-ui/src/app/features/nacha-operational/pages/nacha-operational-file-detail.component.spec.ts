import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { IncomingNachaCommandCenterService } from '../services/incoming-nacha-command-center.service';
import { NachaOperationalFileDetailComponent } from './nacha-operational-file-detail.component';

describe('NachaOperationalFileDetailComponent', () => {
  let fixture: ComponentFixture<NachaOperationalFileDetailComponent>;
  let api: jasmine.SpyObj<IncomingNachaCommandCenterService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<IncomingNachaCommandCenterService>('IncomingNachaCommandCenterService', ['getFile', 'getValidations', 'getBatches', 'getTransactions', 'getAddendas', 'getQueueDetail']);
    api.getFile.and.returnValue(of(fileDetail()));
    api.getValidations.and.returnValue(of([validation()]));
    api.getBatches.and.returnValue(of({ items: [batch()], page: 1, pageSize: 10, totalItems: 1 }));
    api.getTransactions.and.returnValue(of({ items: [transaction()], page: 1, pageSize: 10, totalItems: 1 }));
    api.getAddendas.and.returnValue(of([]));
    api.getQueueDetail.and.returnValue(of(queueDetail()));
    const route = {
      snapshot: {
        queryParamMap: convertToParamMap({ seccion: 'resumen', retorno: '/incoming-nacha-command-center?page=2' })
      },
      paramMap: of(convertToParamMap({ fileId: '11111111-1111-1111-1111-111111111111' }))
    };

    await TestBed.configureTestingModule({
      imports: [NachaOperationalFileDetailComponent, NoopAnimationsModule],
      providers: [provideRouter([]), { provide: IncomingNachaCommandCenterService, useValue: api }, { provide: ActivatedRoute, useValue: route }]
    }).compileComponents();
    fixture = TestBed.createComponent(NachaOperationalFileDetailComponent);
    fixture.detectChanges();
  });

  it('muestra resumen, progreso y navegación secundaria en español', () => {
    const content = text();
    expect(content).toContain('Carga completada');
    expect(content).toContain('Progreso del archivo');
    expect(content).toContain('Validaciones');
    expect(content).toContain('Lotes');
    expect(content).toContain('Transacciones');
    expect(content).toContain('Procesamiento');
  });

  it('consulta detalle, validaciones, lotes y transacciones mediante endpoints progresivos', () => {
    expect(api.getFile).toHaveBeenCalled();
    expect(api.getValidations).toHaveBeenCalled();
    expect(api.getBatches).toHaveBeenCalled();
    expect(api.getTransactions).toHaveBeenCalled();
  });

  it('selecciona un lote y aplica paginación de transacciones desde servidor', () => {
    fixture.componentInstance.filterByBatch(batch());
    expect(fixture.componentInstance.transactionFilters.controls.batchId.value).toBe(7);
    expect(api.getTransactions.calls.mostRecent().args[1]).toEqual(jasmine.objectContaining({ batchId: 7, page: 1 }));
  });

  it('abre la transacción, consulta addendas e intentos y no inventa código ACH', () => {
    fixture.componentInstance.selectedTabIndex = 3;
    fixture.detectChanges();
    fixture.componentInstance.openTransaction(transaction());
    fixture.detectChanges();
    expect(api.getAddendas).toHaveBeenCalledWith('11111111-1111-1111-1111-111111111111', 9);
    expect(api.getQueueDetail).toHaveBeenCalled();
    expect(text()).toContain('Código ACH');
    expect(text()).toContain('No disponible');
    expect(text()).toContain('Error técnico');
    expect(text()).toContain('No procesado');
  });

  it('presenta la ausencia de addendas con un mensaje operativo', () => {
    fixture.componentInstance.selectedTabIndex = 3;
    fixture.detectChanges();
    fixture.componentInstance.openTransaction(transaction());
    fixture.detectChanges();
    expect(fixture.componentInstance.addendas).toEqual([]);
    expect(fixture.componentInstance.selectedTransaction?.addendaCount).toBe(0);
  });

  it('presenta errores de consulta recuperables sin exponer respuestas HTTP', () => {
    fixture.componentInstance.selectedTabIndex = 1;
    fixture.detectChanges();
    api.getValidations.and.returnValue(throwError(() => ({ status: 500, message: 'HTTP 500' })));
    fixture.componentInstance.retryValidations();
    fixture.detectChanges();
    expect(fixture.componentInstance.validationsError).toContain('No fue posible consultar las validaciones');
    expect(fixture.componentInstance.loadingValidations).toBeFalse();
    expect(text()).not.toContain('HTTP 500');
  });

  function text(): string { return fixture.nativeElement.textContent; }
});

function fileDetail() {
  return {
    id: '11111111-1111-1111-1111-111111111111', fileName: '0001283.001.20260731.1', correlationId: 'corr-archivo-123456',
    ingestionStatus: 'Completado' as const, ingestionStatusText: 'Completado', stageCode: 'Persisted', stageText: 'Carga completada',
    cycleResolutionStatus: 'ResueltoConfirmado', parsingStatus: 'Exitoso', resolvedClearingHouseId: 1, clearingHouseName: 'CENIT',
    resolvedAchCycleId: 'CICLO-01', operationalDate: '2026-07-31', notes: '', uploadedBy: 'operador', uploadedAtUtc: '2026-08-01T14:00:00Z',
    receivedAtUtc: '2026-08-01T14:00:00Z', overallResultText: 'Con errores técnicos', pendingTransactions: 1,
    summary: { totalBatches: 1, totalTransactions: 1, totalAddendas: 0, totalDebit: 0, totalCredit: 100, successfulTransactions: 0, rejectedTransactions: 0, returnedTransactions: 0, technicalFailures: 1 },
    admissionIssue: null,
    queue: [{ id: 'queue-1', ingestionId: '11111111-1111-1111-1111-111111111111', entryDetailId: 9, queueStatus: 'RetryPending', queueStatusText: 'Pendiente de reintento', attemptCount: 1, maxAttempts: 3, nextAttemptAtUtc: '2026-08-01T15:00:00Z', scheduledAtUtc: '2026-08-01T14:05:00Z', soapOperation: 'Proc_Transacciones', lastErrorCode: 'SOAP_TIMEOUT', lastErrorMessage: 'timeout', lastResponseCode: '' }],
    events: []
  };
}

function validation() {
  return { code: 'ADMISSION_ACCEPTED', title: 'Fecha del encabezado', message: 'La fecha corresponde a la operación.', expectedValue: '2026-07-31', foundValue: '2026-07-31', suggestedAction: 'Continúe con el seguimiento.', errorType: 'Functional', severity: 'Information', isSuccessful: true, occurredAtUtc: '2026-08-01T14:00:00Z' };
}

function batch() {
  return { id: 7, batchNumber: 1, companyName: 'Empresa', serviceClassCode: '220', standardEntryClassCode: 'PPD', companyEntryDescription: 'PAGOS', effectiveEntryDate: '260731', totalTransactions: 1, totalAmount: 100, totalDebit: 0, totalCredit: 100 };
}

function transaction() {
  return {
    id: 9, batchId: 7, batchNumber: 1, traceNumber: '123456789012345', transactionCode: '22', transactionCodeDescription: 'Crédito a cuenta corriente', amount: 100,
    addendaCount: 0, classificationCode: 'CreditoEntrante', classificationText: 'Crédito entrante', dispatchQueueId: 'queue-1', dispatchStatusCode: 'RetryPending', dispatchStatusText: 'Pendiente de reintento',
    attemptCount: 1, maxAttempts: 3, processingStatus: 'TechnicalFailed' as const, processingStatusText: 'Error técnico', businessOutcome: 'NotProcessed' as const, businessOutcomeText: 'No procesado',
    resultCode: '', resultDescription: '', correlationId: 'corr-transaccion', clearingHouseId: 1, achCycleId: 'CICLO-01', soapOperation: 'Proc_Transacciones', externalTransactionId: '', technicalErrorCode: 'SOAP_TIMEOUT', technicalErrorMessage: 'timeout',
    accountNumberMasked: '****1234', originInstitution: '****0001', destinationInstitution: '****0002', recipientNameMasked: 'P***', effectiveEntryDate: '260731'
  };
}

function queueDetail() {
  return {
    queue: fileDetail().queue[0],
    classification: { functionalClass: 'CreditoEntrante', eligibilityStatus: 'Elegible', requiresManualResolution: false, prenoteStatus: 'NoAplica', businessMeaning: 'Crédito entrante elegible' },
    executions: [{ id: 'execution-1', attemptNumber: 1, methodName: 'Proc_Transacciones', correlationId: 'corr-transaccion', processingStatusText: 'Error técnico', businessOutcomeText: 'No procesado', resultCode: '', resultDescription: '', isSuccess: false, isRetryable: true, startedAtUtc: '2026-08-01T14:05:00Z', logicalEndpoint: 'WSCFAACH', durationMs: 30000, transportStatusText: 'Tiempo de espera agotado', technicalErrorCode: 'SOAP_TIMEOUT', technicalErrorMessage: 'timeout', externalTransactionId: '' }],
    events: []
  };
}
