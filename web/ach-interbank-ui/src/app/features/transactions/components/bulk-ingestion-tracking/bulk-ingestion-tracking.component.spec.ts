import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { BulkIngestionBatchStatus, BulkIngestionRetryScope, BulkBatchStatusDto } from '../../transactions.models';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionTrackingComponent } from './bulk-ingestion-tracking.component';

describe('BulkIngestionTrackingComponent', () => {
  let fixture: ComponentFixture<BulkIngestionTrackingComponent>;
  let component: BulkIngestionTrackingComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let router: Router;

  const batch = (
    batchId: string,
    status: BulkIngestionBatchStatus = BulkIngestionBatchStatus.Queued,
    uploadedAtUtc = '2026-10-02T12:00:00.000Z',
    overrides: Partial<BulkBatchStatusDto> = {}
  ): BulkBatchStatusDto => ({
    batchId,
    batchReference: `REF-${batchId}`,
    status,
    totalRecords: 10,
    totalValid: 9,
    totalInvalid: 1,
    totalProcessed: 4,
    totalSucceeded: 3,
    totalFailed: 1,
    progressPercent: 40,
    uploadedAtUtc,
    retryCount: 0,
    lastJobMessage: '',
    errorSummary: [],
    ...overrides
  });

  beforeEach(async () => {
    localStorage.removeItem('ach.bulk.recentBatchIds');

    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['getBatch', 'retry']);
    api.getBatch.and.returnValue(of(batch('batch-default')));
    api.retry.and.returnValue(of({ batchId: 'batch-default', attemptId: 1, attemptNumber: 1, jobId: 'job', status: 9 }));
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [BulkIngestionTrackingComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BulkIngestionTrackingComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    localStorage.removeItem('ach.bulk.recentBatchIds');
  });

  it('does not call the API when the manual search is empty', () => {
    component.batchIdInput.set('   ');
    component.searchByBatchId();

    expect(api.getBatch).not.toHaveBeenCalled();
    expect(notifications.warning).toHaveBeenCalledWith('Ingrese un identificador de lote para buscar.');
  });

  it('adds and updates a batch from a valid manual search', () => {
    api.getBatch.and.returnValues(
      of(batch('batch-1', BulkIngestionBatchStatus.Queued, '2026-10-02T10:00:00.000Z')),
      of(batch('batch-1', BulkIngestionBatchStatus.Processing, '2026-10-02T10:00:00.000Z', { totalProcessed: 8 }))
    );

    component.batchIdInput.set(' batch-1 ');
    component.searchByBatchId();
    component.batchIdInput.set('batch-1');
    component.searchByBatchId();

    expect(api.getBatch.calls.allArgs()).toEqual([['batch-1'], ['batch-1']]);
    expect(component.rows().length).toBe(1);
    expect(component.rows()[0].status).toBe(BulkIngestionBatchStatus.Processing);
    expect(component.rows()[0].totalProcessed).toBe(8);
  });

  it('loads recent batch ids tolerantly and orders rows by uploadedAtUtc descending', () => {
    localStorage.setItem('ach.bulk.recentBatchIds', JSON.stringify(['batch-old', 'batch-new']));
    api.getBatch.and.callFake((id: string) => of(
      id === 'batch-old'
        ? batch(id, BulkIngestionBatchStatus.Completed, '2026-10-01T10:00:00.000Z')
        : batch(id, BulkIngestionBatchStatus.Failed, '2026-10-02T10:00:00.000Z')
    ));

    component.refreshFromRecent();

    expect(api.getBatch.calls.allArgs()).toEqual([['batch-old'], ['batch-new']]);
    expect(component.rows().map((item) => item.batchId)).toEqual(['batch-new', 'batch-old']);
  });

  it('treats malformed recent batch storage as empty', () => {
    localStorage.setItem('ach.bulk.recentBatchIds', '{not-json');

    component.refreshFromRecent();

    expect(api.getBatch).not.toHaveBeenCalled();
    expect(component.rows()).toEqual([]);
  });

  it('filters tracked rows locally by status', () => {
    component.rows.set([
      batch('batch-failed', BulkIngestionBatchStatus.Failed),
      batch('batch-completed', BulkIngestionBatchStatus.Completed)
    ]);

    component.filtrosForm.controls.status.setValue(String(BulkIngestionBatchStatus.Failed));

    expect(component.filteredRows().map((item) => item.batchId)).toEqual(['batch-failed']);
  });

  it('allows retry only for partially processed and failed batches', () => {
    expect(component.canRetry(BulkIngestionBatchStatus.PartiallyProcessed)).toBeTrue();
    expect(component.canRetry(BulkIngestionBatchStatus.Failed)).toBeTrue();
    expect(component.canRetry(BulkIngestionBatchStatus.Uploaded)).toBeFalse();
    expect(component.canRetry(BulkIngestionBatchStatus.Processing)).toBeFalse();
    expect(component.canRetry(BulkIngestionBatchStatus.Completed)).toBeFalse();
    expect(component.canRetry(BulkIngestionBatchStatus.Retrying)).toBeFalse();
    expect(component.canRetry(BulkIngestionBatchStatus.Cancelled)).toBeFalse();
  });

  it('requests failed-only retry and refreshes the batch after success', () => {
    api.retry.and.returnValue(of({ batchId: 'batch-1', attemptId: 1, attemptNumber: 2, jobId: 'job', status: 9 }));
    api.getBatch.and.returnValue(of(batch('batch-1', BulkIngestionBatchStatus.Retrying, '2026-10-02T10:00:00.000Z', { retryCount: 1 })));

    component.retryFailed('batch-1');

    expect(api.retry).toHaveBeenCalledOnceWith('batch-1', { scope: BulkIngestionRetryScope.FailedOnly });
    expect(notifications.success).toHaveBeenCalledWith('Reintento del lote solicitado.');
    expect(api.getBatch).toHaveBeenCalledOnceWith('batch-1');
    expect(component.rows()[0].status).toBe(BulkIngestionBatchStatus.Retrying);
  });


  it('fails closed when the user lacks the retry capability', () => {
    auth.hasPermission.and.returnValue(false);
    api.retry.calls.reset();

    component.retryFailed('batch-1');

    expect(auth.hasPermission).toHaveBeenCalledWith(['BulkIngestion.Retry', 'CanManageAch']);
    expect(api.retry).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para reintentar lotes de carga.');
  });

  it('navigates to the bulk ingestion detail', () => {
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);

    component.openDetail('batch-42');

    expect(navigate).toHaveBeenCalledOnceWith(['/transactions/bulk-ingestion', 'batch-42']);
  });

  it('persists at most 25 recent batch ids', () => {
    component.rows.set(Array.from({ length: 26 }, (_, index) =>
      batch(`batch-${index}`, BulkIngestionBatchStatus.Completed, `2026-10-${String(index + 1).padStart(2, '0')}T10:00:00.000Z`)
    ));
    api.getBatch.and.returnValue(of(batch('batch-new', BulkIngestionBatchStatus.Queued, '2026-11-01T10:00:00.000Z')));
    component.batchIdInput.set('batch-new');

    component.searchByBatchId();

    const persisted = JSON.parse(localStorage.getItem('ach.bulk.recentBatchIds') ?? '[]') as string[];
    expect(persisted.length).toBe(25);
    expect(persisted[0]).toBe('batch-new');
  });

  it('uses Material controls without ui-boton and keeps the enterprise grid', () => {
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('ui-boton')).toBeNull();
    expect(root.querySelector('mat-form-field')).not.toBeNull();
    expect(root.querySelector('mat-select')).not.toBeNull();
    expect(root.querySelector('ui-grilla-empresarial')).not.toBeNull();
  });
});
