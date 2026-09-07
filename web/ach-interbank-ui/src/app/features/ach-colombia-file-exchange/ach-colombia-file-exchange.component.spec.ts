import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeComponent } from './ach-colombia-file-exchange.component';
import { AchColombiaFileExchangeService } from './ach-colombia-file-exchange.service';
import { TransferDetail, TransferSummary } from './ach-colombia-file-exchange.models';

describe('AchColombiaFileExchangeComponent', () => {
  let fixture: ComponentFixture<AchColombiaFileExchangeComponent>;
  let component: AchColombiaFileExchangeComponent;
  let api: jasmine.SpyObj<AchColombiaFileExchangeService>;
  let auth: jasmine.SpyObj<AuthService>;
  const row: TransferSummary = {
    id: '7a4c3c48-92bd-4fd2-a8f1-3d16e2745d71', fileName: 'transfer.out', direction: 'Outbound', operationalDate: '2026-09-02',
    status: 'Failed', executionOrigin: 'Manual', attemptCount: 1, updatedAtUtc: '2026-09-02T12:00:00Z', archived: false, retired: false
  };
  const detail: TransferDetail = { ...row, status: 'RetryPending', fileSize: 128, contentSha256: 'hash', createdAtUtc: '2026-09-02T11:00:00Z', history: [],
    transactionIds: [], contentAvailable: true, canRetry: true, canReprocess: false, canArchive: true, canRetire: true };

  beforeEach(async () => {
    api = jasmine.createSpyObj<AchColombiaFileExchangeService>('AchColombiaFileExchangeService',
      ['list', 'detail', 'executeOutbound', 'executeInbound', 'retry', 'reprocess', 'archive', 'retire', 'download']);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    api.list.and.returnValue(of([row]));
    api.detail.and.returnValue(of(detail));
    api.retry.and.returnValue(of(detail));

    await TestBed.configureTestingModule({
      imports: [AchColombiaFileExchangeComponent],
      providers: [
        provideRouter([]),
        { provide: AchColombiaFileExchangeService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) }
      ]
    }).compileComponents();
  });

  it('loads transfers for an ACH reader', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalled();
    expect(component.rows).toEqual([row]);
  });

  it('shows durable correlation, all lifecycle filters and links to existing lineage', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = { ...detail, correlationId: 'correlation-123', transactionIds: [42],
      lastErrorCode: 'ACHCOL_MFT_IO_UNCERTAIN', lastAttemptAtUtc: '2026-09-02T12:00:00Z',
      history: [{ id: 1, occurredAtUtc: '2026-09-02T12:00:00Z', eventType: 'OutboundAttempt', result: 'Uncertain',
        message: 'Entrega sin confirmar', executionOrigin: 'Automatic', actor: 'task:AchColombiaManagedMftOutbound' }] };
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain(detail.id);
    expect(text).toContain('correlation-123');
    expect(text).toContain('ACHCOL_MFT_IO_UNCERTAIN');
    expect(text).toContain('task:AchColombiaManagedMftOutbound');
    expect(fixture.nativeElement.querySelector('a[href="/transactions/outgoing-monitoring/42"]')).not.toBeNull();
    expect(component.statuses.length).toBe(11);
    expect(component.statuses).toContain('Uncertain');
    expect(component.statuses).toContain('Failed');
  });

  it('prevents an ineligible retry and a missing-content download', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = { ...detail, canRetry: false, contentAvailable: false };
    fixture.detectChanges();
    component.retry();
    component.download();
    expect(api.retry).not.toHaveBeenCalled();
    expect(api.download).not.toHaveBeenCalled();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.find(x => x.textContent === 'Reintentar')!.disabled).toBeTrue();
    expect(buttons.find(x => x.textContent === 'Descargar')!.disabled).toBeTrue();
  });

  it('does not render or invoke management actions without CanManageAch', () => {
    auth.hasPermission.and.returnValue(false);
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();

    component.retry();

    expect(fixture.nativeElement.textContent).not.toContain('Reintentar');
    expect(api.retry).not.toHaveBeenCalled();
  });

  it('retries through the API and refreshes the transfer list', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();

    component.retry();

    expect(api.retry).toHaveBeenCalledWith(detail.id);
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('releases the busy state when an operation fails', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    api.retry.and.returnValue(throwError(() => ({ error: { detail: 'Rejected by server' } })));
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;

    component.retry();

    expect(component.busy).toBeFalse();
    expect(component.error).toBe('Rejected by server');
  });
});
