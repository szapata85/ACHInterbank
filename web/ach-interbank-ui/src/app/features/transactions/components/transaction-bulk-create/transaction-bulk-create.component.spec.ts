import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkAchTransactionResponse } from '../../transactions.models';
import { BulkIngestionApiService } from '../../services/bulk-ingestion-api.service';
import { TransactionBulkCreateComponent } from './transaction-bulk-create.component';

describe('TransactionBulkCreateComponent', () => {
  let fixture: ComponentFixture<TransactionBulkCreateComponent>;
  let component: TransactionBulkCreateComponent;
  let api: jasmine.SpyObj<BulkIngestionApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

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

  it('should validate invalid json and show error', () => {
    component.onJsonInput('{invalid json');
    component.validateJson();

    expect(component.payload()).toBeNull();
    expect(component.validationError()).toContain('JSON válido');
  });

  it('should parse valid json and expose summary count', () => {
    component.onJsonInput(JSON.stringify({
      batchReference: 'TEST-BATCH',
      chunkSize: 120,
      transactions: [
        {
          amount: 1000,
          reference: 'REF-001',
          type: 1,
          accountType: 1,
          isPrenotification: false,
          destinationInstitutionId: 2,
          sourceAccountNumber: '1234567890',
          destinationAccountNumber: '9876543210',
          companyName: 'EMPRESA',
          companyIdentification: '900123456',
          companyEntryDescriptionId: 1
        }
      ]
    }));

    component.validateJson();

    expect(component.payload()).not.toBeNull();
    expect(component.transactionCount()).toBe(1);
  });

  it('should submit and render success totals', () => {
    const result: BulkAchTransactionResponse = {
      batchReference: 'TEST-BATCH',
      totalReceived: 2,
      totalProcessed: 2,
      totalSucceeded: 2,
      totalFailed: 0,
      createdTransactionIds: [10, 11],
      itemResults: [
        { index: 0, reference: 'REF-001', succeeded: true, transactionId: 10 },
        { index: 1, reference: 'REF-002', succeeded: true, transactionId: 11 }
      ]
    };

    api.submit.and.returnValue(of({ processingMode: 1, status: "COMPLETED", immediateResult: result }));
    component.onJsonInput(JSON.stringify({
      batchReference: 'TEST-BATCH',
      transactions: [
        {
          amount: 1000,
          reference: 'REF-001',
          type: 1,
          accountType: 1,
          isPrenotification: false,
          destinationInstitutionId: 2,
          sourceAccountNumber: '1234567890',
          destinationAccountNumber: '9876543210',
          companyName: 'EMPRESA',
          companyIdentification: '900123456',
          companyEntryDescriptionId: 1
        },
        {
          amount: 1200,
          reference: 'REF-002',
          type: 1,
          accountType: 1,
          isPrenotification: false,
          destinationInstitutionId: 2,
          sourceAccountNumber: '1234567891',
          destinationAccountNumber: '9876543211',
          companyName: 'EMPRESA',
          companyIdentification: '900123456',
          companyEntryDescriptionId: 1
        }
      ]
    }));

    component.validateJson();
    component.submit();
    fixture.detectChanges();

    expect(api.submit).toHaveBeenCalled();
    expect(component.response()?.totalSucceeded).toBe(2);
    expect(notifications.success).toHaveBeenCalled();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Resultado de procesamiento');
    expect(text).toContain('2');
  });

  it('should handle submit error', () => {
    api.submit.and.returnValue(throwError(() => new Error('API error')));

    component.onJsonInput(JSON.stringify({
      batchReference: 'TEST-BATCH',
      transactions: [
        {
          amount: 1000,
          reference: 'REF-001',
          type: 1,
          accountType: 1,
          isPrenotification: false,
          destinationInstitutionId: 2,
          sourceAccountNumber: '1234567890',
          destinationAccountNumber: '9876543210',
          companyName: 'EMPRESA',
          companyIdentification: '900123456',
          companyEntryDescriptionId: 1
        }
      ]
    }));

    component.validateJson();
    component.submit();

    expect(component.validationError()).toContain('API error');
    expect(notifications.error).toHaveBeenCalled();
  });
});
