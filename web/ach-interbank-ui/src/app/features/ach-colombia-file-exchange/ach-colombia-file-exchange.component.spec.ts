import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { OperationalConfirmDialogComponent } from '../../shared/components/operational-confirm-dialog.component';
import { RetireTransferDialogComponent } from './retire-transfer-dialog.component';
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
  let dialog: jasmine.SpyObj<MatDialog>;
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
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as any);
    auth.hasPermission.and.returnValue(true);
    api.list.and.returnValue(of([row]));
    api.detail.and.returnValue(of(detail));
    api.retry.and.returnValue(of(detail));

    await TestBed.configureTestingModule({
      imports: [AchColombiaFileExchangeComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: AchColombiaFileExchangeService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: MatDialog, useValue: dialog },
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
    expect(buttons.find(x => x.textContent?.trim() === 'Reintentar')!.disabled).toBeTrue();
    expect(buttons.find(x => x.textContent?.trim() === 'Descargar')!.disabled).toBeTrue();
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
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();

    component.retry();

    expect(dialog.open).toHaveBeenCalled();
    expect(api.retry).toHaveBeenCalledWith(detail.id);
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('releases the busy state when an operation fails', () => {
    api.retry.and.returnValue(throwError(() => ({ error: { detail: 'Rejected by server' } })));
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;

    component.retry();

    expect(component.busy).toBeFalse();
    expect(component.error).toBe('No fue posible completar la operación. Revisa el estado y vuelve a intentar.');
  });

  it('muestra un aviso útil y enlace de configuración para el conflicto MFT deshabilitado', () => {
    api.executeInbound.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 409, error: { status: 409, code: 'ACHCOL_MFT_DISABLED', detail: 'La integración está deshabilitada.' }
    })));
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.executeInbound();

    expect(api.executeInbound).toHaveBeenCalledTimes(1);
    expect(component.busy).toBeFalse();
    expect(component.mftDisabled).toBeTrue();
    fixture.detectChanges();
    const alert = fixture.nativeElement.querySelector('.mft-alert') as HTMLElement;
    expect(alert.textContent).toContain('Intercambio de archivos no disponible');
    expect(alert.textContent).not.toContain('ACHCOL_MFT_DISABLED');
    expect(fixture.nativeElement.querySelector('a[href="/administracion/mft-ach"]')).not.toBeNull();
  });

  it('no inventa mensajes de configuración ante otros errores 409 o fallas 500', () => {
    api.executeInbound.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 409, error: { code: 'OTHER_CONFLICT' } })),
      throwError(() => new HttpErrorResponse({ status: 500, error: { message: 'System.Exception: secreto' } }))
    );
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.executeInbound();
    expect(component.mftDisabled).toBeFalse();
    expect(component.error).toBe('No fue posible completar la operación. Revisa el estado y vuelve a intentar.');
    component.executeInbound();
    expect(component.mftDisabled).toBeFalse();
    expect(component.error).not.toContain('secreto');
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

  it('keeps advanced filters available while showing only the essential ones initially', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Más filtros');
    expect(fixture.nativeElement.textContent).toContain('Buscar archivos');
    expect(fixture.nativeElement.querySelectorAll('mat-form-field').length).toBeGreaterThan(4);
    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(1);
    expect(fixture.nativeElement.querySelectorAll('thead th').length).toBe(6);
  });

  it('prevents invalid date ranges from triggering a request', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.filter.from = '2026-11-30';
    component.filter.to = '2026-11-01';
    component.search();

    expect(component.invalidDateRange).toBeTrue();
    expect(api.list).toHaveBeenCalledTimes(1);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('fecha inicial no puede ser posterior');
  });

  it('clears all filters and preserves the server-side page contract', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.filter.fileName = 'pago.out';
    component.filter.status = 'Failed';
    component.filter.pageNumber = 3;
    component.clearFilters();

    expect(component.filter.fileName).toBe('');
    expect(component.filter.status).toBe('');
    expect(component.filter.pageNumber).toBe(1);
    expect(component.filter.pageSize).toBe(50);
    expect(api.list).toHaveBeenCalledTimes(2);
  });

  it('requires a cycle before requesting outbound confirmation', () => {
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.cycleId = '  ';
    component.executeOutbound();

    expect(component.manualCycleError).toContain('identificador del ciclo');
    expect(dialog.open).not.toHaveBeenCalled();
    expect(api.executeOutbound).not.toHaveBeenCalled();
  });

  it('cancels outbound execution when the confirmation dialog is declined', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(false) } as any);
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.cycleId = 'C-2';
    component.executeOutbound();

    expect(dialog.open.calls.mostRecent().args[0]).toBe(OperationalConfirmDialogComponent);
    expect(api.executeOutbound).not.toHaveBeenCalled();
  });

  it('confirms outbound execution with the unchanged cycle identifier', () => {
    api.executeOutbound.and.returnValue(of({}));
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.cycleId = ' C-3 ';
    component.executeOutbound();

    expect(api.executeOutbound).toHaveBeenCalledOnceWith('C-3');
  });

  it('requires an explicit retirement reason and preserves it in the API call', () => {
    dialog.open.and.returnValue({ afterClosed: () => of('  duplicado confirmado  ') } as any);
    api.retire.and.returnValue(of({ ...detail, retired: true }));
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();
    component.retire();

    expect(dialog.open.calls.mostRecent().args[0]).toBe(RetireTransferDialogComponent);
    expect(api.retire).toHaveBeenCalledOnceWith(detail.id, 'duplicado confirmado');
  });

  it('does not retire when the reason dialog is dismissed', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(undefined) } as any);
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();
    component.retire();
    expect(api.retire).not.toHaveBeenCalled();
  });

  it('keeps management actions hidden for read-only users and permits download', () => {
    auth.hasPermission.and.returnValue(false);
    fixture = TestBed.createComponent(AchColombiaFileExchangeComponent);
    component = fixture.componentInstance;
    component.selected = detail;
    fixture.detectChanges();
    const text: string = fixture.nativeElement.textContent;

    expect(text).toContain('Descargar');
    expect(text).not.toContain('Operaciones manuales');
    expect(text).not.toContain('Retirar archivo');
  });

});
