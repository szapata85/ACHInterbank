import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  BulkBatchProcessingSummaryDto,
  BulkIngestionBatchStatus,
  BulkIngestionItemStatus,
  BulkIngestionRetryScope
} from '../../transactions.models';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';
import { BulkIngestionDetailComponent } from './bulk-ingestion-detail.component';

describe('BulkIngestionDetailComponent', () => {
  let fixture: ComponentFixture<BulkIngestionDetailComponent>;
  let component: BulkIngestionDetailComponent;
  let api: jasmine.SpyObj<BulkIngestionTrackingApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const summaryFor = (status: BulkIngestionBatchStatus): BulkBatchProcessingSummaryDto => ({
    batchId: 'batch-1',
    status: {
      batchId: 'batch-1',
      batchReference: 'REF-1',
      status,
      totalRecords: 10,
      totalValid: 8,
      totalInvalid: 2,
      totalProcessed: 8,
      totalSucceeded: 6,
      totalFailed: 2,
      progressPercent: 100,
      uploadedAtUtc: '2026-10-02T12:00:00.000Z',
      retryCount: 1,
      lastJobMessage: 'done',
      errorSummary: ['error']
    },
    attempts: [{
      attemptId: 1,
      attemptNumber: 1,
      triggerType: 'Initial',
      scope: 'Full',
      triggeredBy: 'tester',
      triggeredAtUtc: '2026-10-02T12:00:00.000Z',
      status: 'Completed',
      totalProcessed: 8,
      totalSucceeded: 6,
      totalFailed: 2,
      resultMessage: 'done'
    }]
  });

  beforeEach(async () => {
    api = jasmine.createSpyObj<BulkIngestionTrackingApiService>('BulkIngestionTrackingApiService', ['getSummary', 'getBatchItems', 'retry', 'cancel']);
    api.getSummary.and.returnValue(of(summaryFor(BulkIngestionBatchStatus.PartiallyProcessed)));
    api.getBatchItems.and.returnValue(of({
      page: 1,
      pageSize: 25,
      total: 60,
      items: [{ itemId: 1, itemIndex: 1, reference: 'REF-1', status: BulkIngestionItemStatus.ProcessingError, message: 'duplicado', transactionId: null }]
    }));
    api.retry.and.returnValue(of({ batchId: 'batch-1', attemptId: 11, attemptNumber: 2, jobId: 'job-1', status: BulkIngestionBatchStatus.Retrying }));
    api.cancel.and.returnValue(of({ batchId: 'batch-1', cancelled: true, message: 'ok' }));
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    await TestBed.configureTestingModule({
      imports: [BulkIngestionDetailComponent, RouterTestingModule],
      providers: [
        { provide: BulkIngestionTrackingApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications },
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

  it('loads summary and first items page for the batch id', () => {
    expect(api.getSummary).toHaveBeenCalledOnceWith('batch-1');
    expect(api.getBatchItems).toHaveBeenCalledOnceWith('batch-1', 1, 25, null);
    expect(component.summary()?.batchId).toBe('batch-1');
    expect(component.page()).toBe(1);
    expect(component.pageSize()).toBe(25);
  });

  it('resets to page one when the item status filter changes', () => {
    component.page.set(2);
    api.getBatchItems.calls.reset();

    component.applyStatusFilter(String(BulkIngestionItemStatus.ProcessingError));

    expect(component.page()).toBe(1);
    expect(component.itemStatusFilter()).toBe(BulkIngestionItemStatus.ProcessingError);
    expect(api.getBatchItems).toHaveBeenCalledOnceWith('batch-1', 1, 25, BulkIngestionItemStatus.ProcessingError);
  });

  it('paginates within the backend item page boundaries', () => {
    api.getBatchItems.calls.reset();

    component.changePage(1);
    expect(component.page()).toBe(2);
    expect(api.getBatchItems).toHaveBeenCalledOnceWith('batch-1', 2, 25, null);

    api.getBatchItems.calls.reset();
    component.page.set(component.totalPages());
    component.changePage(1);
    expect(api.getBatchItems).not.toHaveBeenCalled();
  });

  it('preserves statusLabel mappings', () => {
    expect(component.statusLabel(BulkIngestionBatchStatus.Uploaded)).toBe('Cargado / Listo');
    expect(component.statusLabel(BulkIngestionBatchStatus.PartiallyProcessed)).toBe('Parcialmente procesado');
    expect(component.statusLabel(BulkIngestionBatchStatus.Completed)).toBe('Completado');
    expect(component.statusLabel(BulkIngestionBatchStatus.Failed)).toBe('Fallido');
    expect(component.statusLabel(BulkIngestionItemStatus.Processed)).toBe('En cola / Procesado');
  });

  it('allows retry only for partially processed and failed batches', () => {
    component.summary.set(summaryFor(BulkIngestionBatchStatus.PartiallyProcessed));
    expect(component.canRetry()).toBeTrue();

    component.summary.set(summaryFor(BulkIngestionBatchStatus.Failed));
    expect(component.canRetry()).toBeTrue();

    for (const status of [
      BulkIngestionBatchStatus.Uploaded,
      BulkIngestionBatchStatus.Parsed,
      BulkIngestionBatchStatus.Validated,
      BulkIngestionBatchStatus.Queued,
      BulkIngestionBatchStatus.Processing,
      BulkIngestionBatchStatus.Completed,
      BulkIngestionBatchStatus.Retrying,
      BulkIngestionBatchStatus.Cancelled
    ]) {
      component.summary.set(summaryFor(status));
      expect(component.canRetry()).withContext(`status ${status}`).toBeFalse();
    }
  });

  it('sends the Full retry scope and reloads after success', () => {
    const reload = spyOn(component, 'loadAll');

    component.retry(BulkIngestionRetryScope.Full);

    expect(api.retry).toHaveBeenCalledOnceWith('batch-1', { scope: BulkIngestionRetryScope.Full });
    expect(notifications.success).toHaveBeenCalledWith('Reintento solicitado correctamente.');
    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('sends the FailedOnly retry scope and reloads after success', () => {
    const reload = spyOn(component, 'loadAll');

    component.retry(BulkIngestionRetryScope.FailedOnly);

    expect(api.retry).toHaveBeenCalledOnceWith('batch-1', { scope: BulkIngestionRetryScope.FailedOnly });
    expect(notifications.success).toHaveBeenCalledWith('Reintento solicitado correctamente.');
    expect(reload).toHaveBeenCalledTimes(1);
  });

  it('allows cancellation only in the current cancellable states', () => {
    for (const status of [
      BulkIngestionBatchStatus.Uploaded,
      BulkIngestionBatchStatus.Parsed,
      BulkIngestionBatchStatus.Validated,
      BulkIngestionBatchStatus.Queued,
      BulkIngestionBatchStatus.Processing,
      BulkIngestionBatchStatus.Retrying
    ]) {
      component.summary.set(summaryFor(status));
      expect(component.canCancel()).withContext(`status ${status}`).toBeTrue();
    }

    for (const status of [
      BulkIngestionBatchStatus.PartiallyProcessed,
      BulkIngestionBatchStatus.Completed,
      BulkIngestionBatchStatus.Failed,
      BulkIngestionBatchStatus.Cancelled
    ]) {
      component.summary.set(summaryFor(status));
      expect(component.canCancel()).withContext(`status ${status}`).toBeFalse();
    }
  });

  it('cancels the batch and reloads after success', () => {
    const reload = spyOn(component, 'loadAll');

    component.cancelBatch();

    expect(api.cancel).toHaveBeenCalledOnceWith('batch-1');
    expect(notifications.success).toHaveBeenCalledWith('Cancelación del lote registrada.');
    expect(reload).toHaveBeenCalledTimes(1);
  });


  it('fails closed when the user lacks the retry capability', () => {
    auth.hasPermission.and.callFake((permissions: string | string[]) => !Array.isArray(permissions) || !permissions.includes('BulkIngestion.Retry'));
    api.retry.calls.reset();

    component.retry(BulkIngestionRetryScope.FailedOnly);

    expect(api.retry).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para reintentar lotes de carga.');
  });

  it('fails closed when the user lacks the cancel capability', () => {
    auth.hasPermission.and.callFake((permissions: string | string[]) => !Array.isArray(permissions) || !permissions.includes('BulkIngestion.Cancel'));
    api.cancel.calls.reset();

    component.cancelBatch();

    expect(api.cancel).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No tiene permisos para cancelar lotes de carga.');
  });

  it('hides retry and cancel controls when mutable permissions are absent', () => {
    auth.hasPermission.and.returnValue(false);
    component.summary.set(summaryFor(BulkIngestionBatchStatus.PartiallyProcessed));
    fixture.detectChanges();

    let text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('Reintentar fallidos');
    expect(text).not.toContain('Reintentar lote completo');

    component.summary.set(summaryFor(BulkIngestionBatchStatus.Processing));
    fixture.detectChanges();
    text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('Cancelar lote');
  });

  it('uses Material controls without ui-boton and keeps both enterprise grids', () => {
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('ui-boton')).toBeNull();
    expect(root.querySelector('mat-form-field')).not.toBeNull();
    expect(root.querySelector('mat-select')).not.toBeNull();
    expect(root.querySelectorAll('ui-grilla-empresarial').length).toBe(2);
  });
});
