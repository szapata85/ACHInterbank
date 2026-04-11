import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionDetailComponent } from './bulk-ingestion-detail.component';

describe('BulkIngestionDetailComponent', () => {
  let fixture: ComponentFixture<BulkIngestionDetailComponent>;
  let component: BulkIngestionDetailComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['getSummary', 'getBatchItems', 'retry', 'cancel']);
    api.getSummary.and.returnValue(of({
      batchId: 'batch-1',
      status: {
        batchId: 'batch-1',
        batchReference: 'REF-1',
        status: 6,
        totalRecords: 10,
        totalValid: 8,
        totalInvalid: 2,
        totalProcessed: 8,
        totalSucceeded: 6,
        totalFailed: 2,
        progressPercent: 100,
        uploadedAtUtc: new Date().toISOString(),
        retryCount: 1,
        lastJobMessage: 'done',
        errorSummary: ['error']
      },
      attempts: []
    }));

    api.getBatchItems.and.returnValue(of({
      page: 1,
      pageSize: 25,
      total: 1,
      items: [{ itemId: 1, itemIndex: 1, reference: 'REF-1', status: 3, message: 'duplicado', transactionId: null }]
    }));

    api.retry.and.returnValue(of({ batchId: 'batch-1', attemptId: 11, attemptNumber: 2, jobId: 'job-1', status: 9 }));
    api.cancel.and.returnValue(of({ batchId: 'batch-1', cancelled: true, message: 'ok' }));

    await TestBed.configureTestingModule({
      imports: [BulkIngestionDetailComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'warning', 'error']) },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ batchId: 'batch-1' }) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BulkIngestionDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load summary and items for batch id', () => {
    expect(api.getSummary).toHaveBeenCalledWith('batch-1');
    expect(api.getBatchItems).toHaveBeenCalled();
    expect(component.summary()?.batchId).toBe('batch-1');
  });

  it('should trigger retry and reload data', () => {
    component.retry(1);

    expect(api.retry).toHaveBeenCalled();
  });

  it('should trigger cancel for cancellable states', () => {
    component.cancelBatch();

    expect(api.cancel).toHaveBeenCalledWith('batch-1');
  });
});
