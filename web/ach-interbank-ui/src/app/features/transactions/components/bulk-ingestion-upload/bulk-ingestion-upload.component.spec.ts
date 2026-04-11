import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionUploadComponent } from './bulk-ingestion-upload.component';

describe('BulkIngestionUploadComponent', () => {
  let fixture: ComponentFixture<BulkIngestionUploadComponent>;
  let component: BulkIngestionUploadComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['upload']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [BulkIngestionUploadComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BulkIngestionUploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should block upload when no file selected', () => {
    component.upload();

    expect(component.errorMessage()).toContain('Debe seleccionar un archivo');
    expect(api.upload).not.toHaveBeenCalled();
  });

  it('should upload valid file and show success batch id', () => {
    const file = new File(['{"transactions":[]}'], 'batch.json', { type: 'application/json' });
    component.selectedFile.set(file);
    api.upload.and.returnValue(of({
      batchId: 'abc-123',
      batchReference: 'SEED',
      status: 1,
      fileType: 1,
      totalRecords: 1,
      totalValid: 1,
      totalInvalid: 0,
      uploadedAtUtc: new Date().toISOString(),
      message: 'ok'
    }));

    component.upload();

    expect(api.upload).toHaveBeenCalled();
    expect(component.successBatchId()).toBe('abc-123');
    expect(notifications.success).toHaveBeenCalled();
  });

  it('should show error when api upload fails', () => {
    const file = new File(['id,name'], 'batch.csv', { type: 'text/csv' });
    component.selectedFile.set(file);
    api.upload.and.returnValue(throwError(() => new Error('upload failed')));

    component.upload();

    expect(component.errorMessage()).toContain('upload failed');
    expect(notifications.error).toHaveBeenCalled();
  });
});
