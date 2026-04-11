import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionTrackingComponent } from './bulk-ingestion-tracking.component';

describe('BulkIngestionTrackingComponent', () => {
  let fixture: ComponentFixture<BulkIngestionTrackingComponent>;
  let component: BulkIngestionTrackingComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['getBatch', 'retry']);

    await TestBed.configureTestingModule({
      imports: [BulkIngestionTrackingComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'warning', 'error']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BulkIngestionTrackingComponent);
    component = fixture.componentInstance;
  });

  it('should add batch by id into tracking grid', () => {
    api.getBatch.and.returnValue(of({
      batchId: 'batch-1',
      batchReference: 'REF-1',
      status: 4,
      totalRecords: 10,
      totalValid: 9,
      totalInvalid: 1,
      totalProcessed: 4,
      totalSucceeded: 3,
      totalFailed: 1,
      progressPercent: 40,
      uploadedAtUtc: new Date().toISOString(),
      retryCount: 0,
      lastJobMessage: '',
      errorSummary: []
    }));

    component.batchIdInput.set('batch-1');
    component.searchByBatchId();

    expect(component.rows().length).toBe(1);
    expect(component.rows()[0].batchId).toBe('batch-1');
  });

  it('should request failed-only retry for a row', () => {
    api.retry.and.returnValue(of({ batchId: 'batch-1', attemptId: 1, attemptNumber: 2, jobId: 'job', status: 9 }));
    api.getBatch.and.returnValue(of({
      batchId: 'batch-1',
      batchReference: 'REF-1',
      status: 9,
      totalRecords: 10,
      totalValid: 9,
      totalInvalid: 1,
      totalProcessed: 4,
      totalSucceeded: 3,
      totalFailed: 1,
      progressPercent: 40,
      uploadedAtUtc: new Date().toISOString(),
      retryCount: 1,
      lastJobMessage: '',
      errorSummary: []
    }));

    component.retryFailed('batch-1');

    expect(api.retry).toHaveBeenCalled();
  });
});
