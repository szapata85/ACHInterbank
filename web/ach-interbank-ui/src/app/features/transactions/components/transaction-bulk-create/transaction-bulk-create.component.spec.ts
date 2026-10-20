import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Subject, of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  BulkAchTransactionResponse,
  BulkIngestionProcessingMode,
  BulkIngestionResponse,
  BulkIngestionSourceType
} from '../../transactions.models';
import { BulkIngestionApiService } from '../../services/bulk-ingestion-api.service';
import { TransactionBulkCreateComponent } from './transaction-bulk-create.component';

describe('TransactionBulkCreateComponent', () => {
  let fixture: ComponentFixture<TransactionBulkCreateComponent>;
  let component: TransactionBulkCreateComponent;
  let api: jasmine.SpyObj<BulkIngestionApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const transaction = (overrides: Record<string, unknown> = {}) => ({
    amount: 1000,
    transactionExternalId: ' EXT-001 ',
    reference: ' REF-001 ',
    type: 1,
    accountType: 1,
    isPrenotification: false,
    destinationInstitutionId: 2,
    sourceAccountNumber: '1234567890',
    destinationAccountNumber: '9876543210',
    companyName: 'EMPRESA',
    companyIdentification: '900123456',
    companyEntryDescriptionId: 1,
    addendas: [{ addendaType: '05', information: 'Información adicional' }],
    ...overrides
  });

  const setValidPayload = (overrides: Record<string, unknown> = {}): void => {
    component.onJsonInput(JSON.stringify({
      batchReference: ' TEST-BATCH ',
      chunkSize: 120,
      transactions: [transaction()],
      ...overrides
    }));
    component.validateJson();
  };

  const response = (overrides: Partial<BulkAchTransactionResponse> = {}): BulkAchTransactionResponse => ({
    batchReference: 'TEST-BATCH',
    totalReceived: 1,
    totalProcessed: 1,
    totalSucceeded: 1,
    totalFailed: 0,
    createdTransactionIds: [10],
    itemResults: [
      {
        index: 0,
        reference: 'REF-001',
        succeeded: true,
        transactionId: 10,
        transactionExternalId: 'EXT-001'
      }
    ],
    ...overrides
  });

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionApiService>('BulkIngestionApiService', ['submit']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [TransactionBulkCreateComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionApiService, useValue: api },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionBulkCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('starts locally without loading catalogs or calling the submit API', () => {
    expect(component.payload()).toBeNull();
    expect(component.response()).toBeNull();
    expect(component.chunkSize()).toBe(200);
    expect(component.batchReference()).toMatch(/^BATCH-\d{8}-001$/);
    expect(api.submit).not.toHaveBeenCalled();
  });

  it('validates invalid json and exposes the existing validation error', () => {
    component.onJsonInput('{invalid json');
    component.validateJson();
    fixture.detectChanges();

    expect(component.payload()).toBeNull();
    expect(component.validationError()).toContain('JSON válido');
    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();
  });

  it('rejects a structurally invalid transaction before submit', () => {
    setValidPayload({ transactions: [transaction({ sourceAccountNumber: undefined })] });

    expect(component.payload()).toBeNull();
    expect(component.validationError()).toContain('estructura inválida');

    component.submit();
    expect(api.submit).not.toHaveBeenCalled();
  });

  it('normalizes operational ids, preserves addendas and applies the chunk fallback', () => {
    setValidPayload({
      batchReference: ' BATCH-NORMALIZED ',
      chunkSize: 10,
      transactions: [transaction({ transactionExternalId: ' EXT-900 ', reference: ' LEGACY-900 ' })]
    });

    const payload = component.payload();
    expect(payload?.batchReference).toBe('BATCH-NORMALIZED');
    expect(payload?.chunkSize).toBe(200);
    expect(payload?.transactions[0].transactionExternalId).toBe('EXT-900');
    expect(payload?.transactions[0].reference).toBe('LEGACY-900');
    expect(payload?.transactions[0].addendas).toEqual([{ addendaType: '05', information: 'Información adicional' }]);
    expect(component.parseWarnings()).toContain('chunkSize fuera de rango recomendado (50-1000). Se usará 200.');
  });

  it('uses legacy reference as the transitional key and keeps the warning', () => {
    setValidPayload({ transactions: [transaction({ transactionExternalId: '   ', reference: ' LEGACY-ONLY ' })] });

    expect(component.payload()?.transactions[0].transactionExternalId).toBeUndefined();
    expect(component.payload()?.transactions[0].reference).toBe('LEGACY-ONLY');
    expect(component.parseWarnings()[0]).toContain('referencia legado');
  });

  it('does not call the API when the payload has not been validated', () => {
    component.submit();

    expect(api.submit).not.toHaveBeenCalled();
    expect(component.validationError()).toBe('Primero valida el JSON para poder enviar el lote.');
  });

  it('submits the exact synchronous inline-ingestion request', () => {
    api.submit.and.returnValue(of({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      status: 'COMPLETED',
      immediateResult: response()
    }));
    setValidPayload();

    component.submit();

    expect(api.submit).toHaveBeenCalledTimes(1);
    expect(api.submit).toHaveBeenCalledWith({
      batchReference: 'TEST-BATCH',
      sourceType: BulkIngestionSourceType.InlineTransactions,
      processingMode: BulkIngestionProcessingMode.Synchronous,
      chunkSize: 120,
      transactions: [
        {
          ...transaction(),
          transactionExternalId: 'EXT-001',
          reference: 'REF-001'
        }
      ]
    });
  });

  it('prevents a second submit while the first request is in flight', () => {
    const pending = new Subject<BulkIngestionResponse>();
    api.submit.and.returnValue(pending.asObservable());
    setValidPayload();

    component.submit();
    component.submit();

    expect(api.submit).toHaveBeenCalledTimes(1);
    expect(component.isSubmitting()).toBeTrue();

    pending.next({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      immediateResult: response()
    });
    pending.complete();

    expect(component.isSubmitting()).toBeFalse();
  });

  it('preserves success result totals, ordering and success notification', () => {
    const result = response({
      totalReceived: 2,
      totalProcessed: 2,
      totalSucceeded: 2,
      createdTransactionIds: [10, 11],
      itemResults: [
        { index: 1, reference: 'REF-002', succeeded: true, transactionId: 11, transactionExternalId: 'EXT-002' },
        { index: 0, reference: 'REF-001', succeeded: true, transactionId: 10, transactionExternalId: 'EXT-001' }
      ]
    });
    api.submit.and.returnValue(of({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      status: 'COMPLETED',
      immediateResult: result
    }));
    setValidPayload({ transactions: [transaction(), transaction({ transactionExternalId: 'EXT-002', reference: 'REF-002' })] });

    component.submit();
    fixture.detectChanges();

    expect(component.response()?.totalSucceeded).toBe(2);
    expect(component.resultRows().map((item) => item.index)).toEqual([0, 1]);
    expect(notifications.success).toHaveBeenCalledWith('Lote procesado con éxito. 2 transacciones exitosas.');
    expect(fixture.nativeElement.textContent).toContain('Resultado de procesamiento');
  });

  it('preserves partial-success handling without hiding item failures', () => {
    api.submit.and.returnValue(of({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      immediateResult: response({
        totalReceived: 2,
        totalProcessed: 2,
        totalSucceeded: 1,
        totalFailed: 1,
        itemResults: [
          { index: 0, reference: 'REF-001', succeeded: true, transactionId: 10, transactionExternalId: 'EXT-001' },
          { index: 1, reference: 'REF-002', succeeded: false, transactionExternalId: 'EXT-002', errorCode: 'ERR', errorMessage: 'Detalle backend' }
        ]
      })
    }));
    setValidPayload();

    component.submit();

    expect(component.summary()?.hasPartialSuccess).toBeTrue();
    expect(component.formatError(component.response()!.itemResults[1])).toBe('Detalle backend');
    expect(notifications.warning).toHaveBeenCalledWith('Lote procesado con éxito parcial. Éxitos: 1, fallos: 1.');
  });

  it('keeps the synchronous-result guard when the backend accepts without immediate result', () => {
    api.submit.and.returnValue(of({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      status: 'ACCEPTED'
    }));
    setValidPayload();

    component.submit();

    expect(component.response()).toBeNull();
    expect(component.isSubmitting()).toBeFalse();
    expect(component.validationError()).toContain('no entregó resultado síncrono');
    expect(notifications.warning).toHaveBeenCalled();
  });

  it('preserves API errors and clears the submitting state', () => {
    api.submit.and.returnValue(throwError(() => new Error('API error')));
    setValidPayload();

    component.submit();

    expect(component.isSubmitting()).toBeFalse();
    expect(component.validationError()).toContain('API error');
    expect(notifications.error).toHaveBeenCalledWith('API error');
  });

  it('uses foundation primitives and contains no ui-boton', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('app-page-header')).not.toBeNull();
    expect(element.querySelector('mat-form-field')).not.toBeNull();
    expect(element.querySelector('ui-estado-vacio')).not.toBeNull();
    expect(element.querySelector('ui-boton')).toBeNull();
    expect(element.querySelector('ui-grilla-empresarial')).toBeNull();

    setValidPayload();
    api.submit.and.returnValue(of({
      processingMode: BulkIngestionProcessingMode.Synchronous,
      immediateResult: response()
    }));
    component.submit();
    fixture.detectChanges();

    expect(element.querySelector('ui-grilla-empresarial')).not.toBeNull();
  });
});
