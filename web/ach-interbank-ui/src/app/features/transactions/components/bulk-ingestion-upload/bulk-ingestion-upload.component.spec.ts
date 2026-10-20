import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkFileUploadResponse, BulkIngestionBatchStatus } from '../../transactions.models';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionUploadComponent } from './bulk-ingestion-upload.component';

describe('BulkIngestionUploadComponent', () => {
  let fixture: ComponentFixture<BulkIngestionUploadComponent>;
  let component: BulkIngestionUploadComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let router: Router;

  const uploadResponse: BulkFileUploadResponse = {
    batchId: 'abc-123',
    batchReference: 'SEED',
    status: BulkIngestionBatchStatus.Uploaded,
    fileType: 1,
    totalRecords: 1,
    totalValid: 1,
    totalInvalid: 0,
    uploadedAtUtc: new Date().toISOString(),
    message: 'ok'
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['upload']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [BulkIngestionUploadComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BulkIngestionUploadComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    localStorage.removeItem('ach.bulk.recentBatchIds');
    fixture.detectChanges();
  });

  afterEach(() => {
    localStorage.removeItem('ach.bulk.recentBatchIds');
  });

  function selectFile(file: File | null): void {
    const event = {
      target: {
        files: {
          item: () => file
        }
      }
    } as unknown as Event;

    component.onFileChange(event);
  }

  it('should preserve accepted file formats and select the chosen file', () => {
    const file = new File(['{"transactions":[]}'], 'batch.JSON', { type: 'application/json' });
    component.errorMessage.set('previous error');
    component.successBatchId.set('previous-batch');

    selectFile(file);

    expect(component.acceptedFormats).toBe('.json,.csv,.xlsx,.xls');
    expect(component.selectedFile()).toBe(file);
    expect(component.fileExtension()).toBe('json');
    expect(component.isValidFile()).toBeTrue();
    expect(component.errorMessage()).toBeNull();
    expect(component.successBatchId()).toBeNull();
  });

  it('should fail closed and hide upload action without BulkIngestion.Upload or CanManageAch', () => {
    auth.hasPermission.and.returnValue(false);
    const file = new File(['id,name'], 'batch.csv', { type: 'text/csv' });
    component.selectedFile.set(file);
    fixture.detectChanges();

    component.upload();

    expect(auth.hasPermission).toHaveBeenCalledWith(['BulkIngestion.Upload', 'CanManageAch']);
    expect(api.upload).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para cargar transacciones desde archivo.');
    expect(fixture.nativeElement.querySelector('button[mat-flat-button]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No tiene permisos para cargar transacciones desde archivo.');
  });

  it('should block upload when no file is selected', () => {
    component.upload();

    expect(component.errorMessage()).toContain('Debe seleccionar un archivo');
    expect(api.upload).not.toHaveBeenCalled();
  });

  it('should reject unsupported, empty, and oversized files without calling the api', () => {
    const unsupported = new File(['content'], 'batch.txt', { type: 'text/plain' });
    component.selectedFile.set(unsupported);
    component.upload();

    expect(component.errorMessage()).toContain('Archivo inválido');
    expect(api.upload).not.toHaveBeenCalled();

    const empty = new File([], 'batch.csv', { type: 'text/csv' });
    component.selectedFile.set(empty);
    component.upload();

    expect(component.isValidFile()).toBeFalse();
    expect(api.upload).not.toHaveBeenCalled();

    const oversized = new File(['x'], 'batch.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    Object.defineProperty(oversized, 'size', { value: 20 * 1024 * 1024 + 1 });
    component.selectedFile.set(oversized);
    component.upload();

    expect(component.isValidFile()).toBeFalse();
    expect(api.upload).not.toHaveBeenCalled();
  });

  it('should submit the valid file with the current metadata fields and preserve the response', () => {
    const file = new File(['id,name'], 'batch.csv', { type: 'text/csv' });
    component.selectedFile.set(file);
    component.metadataForm.setValue({
      batchReference: '  LOTE-001  ',
      clientRequestId: '  REQ-001  '
    });
    api.upload.and.returnValue(of(uploadResponse));

    component.upload();

    expect(api.upload).toHaveBeenCalledOnceWith(file, '  LOTE-001  ', '  REQ-001  ');
    expect(component.successBatchId()).toBe('abc-123');
    expect(component.errorMessage()).toBeNull();
    expect(notifications.success).toHaveBeenCalledWith('Lote SEED registrado. Estado inicial: 1.');
    expect(JSON.parse(localStorage.getItem('ach.bulk.recentBatchIds') ?? '[]')).toEqual(['abc-123']);
  });

  it('should prevent a double submit while the upload is in progress', () => {
    const file = new File(['{"transactions":[]}'], 'batch.json', { type: 'application/json' });
    const pending = new Subject<BulkFileUploadResponse>();
    component.selectedFile.set(file);
    api.upload.and.returnValue(pending.asObservable());

    component.upload();
    component.upload();

    expect(component.isSubmitting()).toBeTrue();
    expect(api.upload).toHaveBeenCalledTimes(1);

    pending.next(uploadResponse);
    pending.complete();

    expect(component.isSubmitting()).toBeFalse();
  });

  it('should show the api error and keep the upload surface available', () => {
    const file = new File(['id,name'], 'batch.csv', { type: 'text/csv' });
    component.selectedFile.set(file);
    api.upload.and.returnValue(throwError(() => new Error('upload failed')));

    component.upload();

    expect(component.errorMessage()).toBe('upload failed');
    expect(component.successBatchId()).toBeNull();
    expect(component.isSubmitting()).toBeFalse();
    expect(notifications.error).toHaveBeenCalledWith('upload failed');
  });

  it('should navigate to tracking and to the successful batch detail using the existing routes', () => {
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);

    component.goToTracking();
    expect(navigate).toHaveBeenCalledWith(['/transactions/bulk-ingestion/tracking']);

    component.goToDetail();
    expect(navigate).toHaveBeenCalledTimes(1);

    component.successBatchId.set('batch-42');
    component.goToDetail();
    expect(navigate).toHaveBeenCalledWith(['/transactions/bulk-ingestion', 'batch-42']);
  });

  it('should use foundation controls and no ui-boton', () => {
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelector('app-page-header')).not.toBeNull();
    expect(root.querySelectorAll('mat-form-field').length).toBe(2);
    expect(root.querySelectorAll('button[mat-flat-button], button[mat-stroked-button]').length).toBeGreaterThan(0);
    expect(root.querySelector('ui-boton')).toBeNull();
  });
});
