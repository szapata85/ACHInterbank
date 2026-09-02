import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
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
  const detail: TransferDetail = { ...row, fileSize: 128, contentSha256: 'hash', createdAtUtc: '2026-09-02T11:00:00Z', history: [] };

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
