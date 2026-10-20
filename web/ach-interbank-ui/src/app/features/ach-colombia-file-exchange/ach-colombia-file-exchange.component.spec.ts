import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeComponent } from './ach-colombia-file-exchange.component';
import { AchColombiaFileExchangeService } from '../../core/services/ach-colombia-file-exchange.service';
import { TransferDetail, TransferSummary } from '../../core/models/ach-colombia-file-exchange.model';

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
    expect(text).not.toContain('ACHCOL_MFT_IO_UNCERTAIN');
    expect(text).toContain('Automatización ACH Colombia');
    expect(text).not.toContain('task:AchColombiaManagedMftOutbound');
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
    expect(component.error).toBe('No fue posible completar la operación. Revise el estado y vuelva a intentar.');
  });

  it('presents Transferred as published for sending and never as ACH Colombia acceptance', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = {
      ...detail,
      status: 'Transferred',
      statusLabel: 'Publicado para envío',
      externalConfirmationStatus: 'NotConfirmed',
      externalConfirmationMessage: 'Recepción por ACH Colombia no confirmada.',
      contentSha256: 'AABBCCDD',
      fileSize: 128,
      cycleId: 'ACH-1',
      operationalDate: '2026-09-02',
      transferredAtUtc: '2026-09-02T12:00:00Z',
      sanitizedActiveReference: 'Publicado en CFA: transfer.out',
      sanitizedArchiveReference: 'Contenido retenido'
    };

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Publicado para envío');
    expect(text).toContain('Recepción por ACH Colombia');
    expect(text).toContain('Recepción por ACH Colombia no confirmada.');
    expect(text).toContain('AABBCCDD');
    expect(text).toContain('128 bytes');
    expect(text).toContain('ACH-1');
    expect(text).toContain('Publicado en CFA: transfer.out');
    expect(text).not.toContain('Aceptado por ACH Colombia');
    expect(text).not.toContain('Recibido por ACH Colombia');
  });

  it('uses simple labels for uncertain retry duplicate and failure states', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;

    expect(component.status('Uncertain')).toBe('Pendiente de confirmación');
    expect(component.status('RetryPending')).toBe('Reintento pendiente');
    expect(component.status('Duplicate')).toBe('Duplicado');
    expect(component.status('Failed')).toBe('Fallido');
  });

  it('renders ordered event presentation fields without exposing internal task actor names', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = {
      ...detail,
      history: [
        { id: 1, occurredAtUtc: '2026-09-02T11:00:00Z', eventType: 'PublicationStarted', result: 'Started', message: 'raw', executionOrigin: 'Automatic', actor: 'task:AchColombiaManagedMftOutbound', displayName: 'Publicación iniciada', displayResult: 'Iniciado', displayMessage: 'Publicación en la frontera CFA iniciada.', actorDisplay: 'Automatización ACH Colombia' },
        { id: 2, occurredAtUtc: '2026-09-02T11:01:00Z', eventType: 'PublishedToMftBoundary', result: 'Succeeded', message: 'raw', executionOrigin: 'Automatic', actor: 'task:AchColombiaManagedMftOutbound', displayName: 'Publicado para envío', displayResult: 'Correcto', displayMessage: 'Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada.', actorDisplay: 'Automatización ACH Colombia' }
      ]
    };

    fixture.detectChanges();
    const history = fixture.nativeElement.querySelector('.history') as HTMLElement;
    const text = history.textContent ?? '';
    expect(text.indexOf('Publicación iniciada')).toBeLessThan(text.indexOf('Publicado para envío'));
    expect(text).toContain('Automatización ACH Colombia');
    expect(text).not.toContain('task:AchColombiaManagedMftOutbound');
  });

  it('does not render raw secrets, internal diagnostics or sensitive paths in transfer observability', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = {
      ...detail,
      lastError: 'super-secret at /srv/internal/outbound',
      lastErrorCode: 'ACHCOL_INTERNAL_SECRET_DIAGNOSTIC',
      sanitizedActiveReference: 'Referencia administrada: file.env',
      history: [{
        id: 1, occurredAtUtc: '2026-09-02T12:00:00Z', eventType: 'PublicationFailed', result: 'Failed',
        message: 'super-secret at /srv/internal/outbound', executionOrigin: 'Automatic', actor: 'task:internal',
        displayName: 'No se pudo publicar', displayResult: 'Fallido', displayMessage: 'No se pudo publicar en la frontera CFA.',
        actorDisplay: 'Automatización ACH Colombia'
      }]
    };
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('super-secret');
    expect(text).not.toContain('/srv/internal');
    expect(text).not.toContain('ACHCOL_INTERNAL_SECRET_DIAGNOSTIC');
    expect(text).not.toContain('task:internal');
    expect(text).toContain('No se pudo publicar en la frontera CFA.');
  });
});
